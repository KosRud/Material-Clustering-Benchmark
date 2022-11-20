using UnityEngine;
using ClusteringAlgorithms;
using System;
using System.Collections.Generic;
using static Diagnostics;

/// <summary>
/// Call <see cref="Dispose" /> after using.
/// </summary>
public class MeasurementRunner : IDisposable
{
    private const int sectionLength = 1000;
    private const int totalSections = 10; // counting repeats as unique sections

    private class VideoSection
    {
        public readonly long start;
        public readonly long end;

        public VideoSection(long start, long end)
        {
            this.start = start;
            this.end = end;
        }
    }

    private List<VideoSection> sections;

    private bool loadingVideo;

    private void FillSectionList(long numFrames)
    {
        this.sections = new List<VideoSection>();

        long numSections = numFrames / sectionLength;
        Assert(numSections > 0, "Video file has no sections.");
        for (
            long sectionStart = 0;
            sectionStart + sectionLength <= numFrames && sections.Count < totalSections;
            sectionStart += sectionLength
        )
        {
            sections.Add(new VideoSection(start: sectionStart, end: sectionStart + sectionLength));
        }
        for (int i = 0; sections.Count < totalSections; i++)
        {
            sections.Add(sections[i]);
        }
    }

    private readonly ComputeShader csHighlightRemoval;
    private readonly int kernelShowResult;

    private readonly WorkGeneration.LaunchParameters launchParameters;

    private UnityEngine.Video.VideoPlayer videoPlayer;
    private readonly long frameStart;
    private readonly long? frameEndOrNull;

    private readonly BenchmarkMeasurementVariance benchmarkMeasurementVariance;
    private readonly BenchmarkMeasurementFrameTime benchmarkMeasurementFrameTime;

    private readonly ClusteringTest.LogType logType;

    private long? lastProcessedFrame;

    public string paramsJSON => JsonUtility.ToJson(this.launchParameters.GetSerializable());
    public bool finished;
    public int warningCounter => this.launchParameters.dispatcher.warningCounter;

    /// <summary>
    /// Takes ownership of launchParameters
    /// </summary>
    public MeasurementRunner(
        WorkGeneration.LaunchParameters launchParameters,
        UnityEngine.Video.VideoPlayer videoPlayer,
        long? frameStart,
        long? frameEnd,
        ClusteringTest.LogType logType,
        ComputeShader csHighlightRemoval
    )
    {
        this.csHighlightRemoval = csHighlightRemoval;
        this.kernelShowResult = csHighlightRemoval.FindKernel("ShowResult");
        this.logType = logType;
        this.launchParameters = launchParameters;
        this.lastProcessedFrame = null;
        this.benchmarkMeasurementVariance = new BenchmarkMeasurementVariance();
        this.benchmarkMeasurementFrameTime = new BenchmarkMeasurementFrameTime();
        this.frameEndOrNull = frameEnd;
        this.frameStart = frameStart ?? 0;
        this.finished = false;
        this.loadingVideo = true;

        if (this.launchParameters.dispatcher.clusteringRTsAndBuffers.isAllocated == false)
        {
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.Allocate();
        }

        this.SetTextureSize();
        this.InitVideoPlayer(videoPlayer, frameStart);

        FillSectionList((long)this.videoPlayer.frameCount);
    }

    private void InitVideoPlayer(UnityEngine.Video.VideoPlayer videoPlayer, long? frameStart)
    {
        this.videoPlayer = videoPlayer;
        this.videoPlayer.playbackSpeed = 0;
        this.videoPlayer.clip = this.launchParameters.video;
        this.videoPlayer.Play();
        /*
            if the frame was 0
            we won't be able to figure out when the video loads

            TODO robust check
        */
        Assert(
            this.videoPlayer.frame != 0,
            "Video player set to frame 0. Normally this does not happen."
        );
        this.videoPlayer.frame = frameStart ?? 0;
    }

    private void SetTextureSize()
    {
        int workingTextureSize = this.launchParameters
            .dispatcher
            .clusteringRTsAndBuffers
            .workingSize;

        int fullTextureSize = this.launchParameters.dispatcher.clusteringRTsAndBuffers.fullSize;

        Assert(
            // positive power of 2
            (workingTextureSize & (workingTextureSize - 1)) == 0
                && workingTextureSize > 0,
            $"Invalid texture size provided to {System.Reflection.MethodBase.GetCurrentMethod().ReflectedType.Name + System.Reflection.MethodBase.GetCurrentMethod().Name}"
        );
        Assert(
            workingTextureSize <= fullTextureSize,
            "Full texture size can not be smaller than working texture size."
        );

        this.csHighlightRemoval.SetInt("texture_size", workingTextureSize);
    }

    public BenchmarkReport GetReport()
    {
        ABenchmarkMeasurement measurement = null;
        switch (this.logType)
        {
            case ClusteringTest.LogType.Variance:
                measurement = this.benchmarkMeasurementVariance;
                break;
            case ClusteringTest.LogType.FrameTime:
                measurement = this.benchmarkMeasurementFrameTime;
                break;
            default:
                Throw(
                    new System.NotImplementedException($"Log type not implemented: {this.logType}")
                );
                break;
        }

        return new BenchmarkReport(
            measurement: measurement,
            serializableLaunchParameters: this.launchParameters.GetSerializable(),
            logType: this.logType
        );
    }

    private void ProcessFrame_VarianceMode()
    {
        this.RunDispatcher();

        if (this.videoPlayer.frame != this.sections[0].start)
        {
            this.MakeVarianceLogEntry();
        }
    }

    private void ProcessFrame_FrameTimeMode()
    {
        /*
                    in order to re-start the clustering "from scratch"
                    all we need to do is to reset the cluster centers
          
                    clustering only edits:
                        * attribution texture array
                        * cluster centers ComputeBuffer
          
                    clustering starts with attribution
                    so the texture array will be messed by the reset of cluster centers
                */

        using (
            ClusterCenters clusterCenters =
                this.launchParameters.dispatcher.clusteringRTsAndBuffers.GetClusterCenters()
        )
        {
            const int numRepetitions = 10;

            float avgTime =
                BenchmarkHelper.MeasureTime(
                    () =>
                    {
                        for (int i = 0; i < numRepetitions; i++)
                        {
                            this.launchParameters.dispatcher.clusteringRTsAndBuffers.SetClusterCenters(
                                clusterCenters.centers
                            );
                            this.RunDispatcher();
                        }

                        using (
                            // force current thread to wait until GPU finishes computations
                            ClusterCenters temp =
                                this.launchParameters.dispatcher.clusteringRTsAndBuffers.GetClusterCenters()
                        )
                        {
                            // useless line to prevent compiler optimization
                            temp.centers[0] = temp.centers[1];
                        }
                    }
                ) / numRepetitions;

            this.benchmarkMeasurementFrameTime.frameTimeRecords.Add(
                new BenchmarkMeasurementFrameTime.FrameTimeRecord(
                    frameIndex: this.videoPlayer.frame,
                    time: avgTime
                )
            );
        }
    }

    private bool IsFrameReady()
    {
        if (
            // not yet loaded video file
            this.videoPlayer.frame == -1
            /*
                ! will be a bug if previous run ended on frame 0
                (which should not normally happen)

                TODO robust check
            */
            || this.videoPlayer.frame != 0 && this.loadingVideo
            // not yet loaded next frame
            || this.lastProcessedFrame == this.videoPlayer.frame
        )
        {
            return false;
        }

        this.loadingVideo = false;

        return true;
    }

    public void ProcessNextFrame(RenderTexture src, RenderTexture dst)
    {
        if (!IsFrameReady())
        {
            Graphics.Blit(src, dst);
            return;
        }

        /*
            ProcessNextFrame() should not be called
            after measurements were finished
        */
        Assert(
            this.finished == false,
            $"{System.Reflection.MethodBase.GetCurrentMethod().ReflectedType.Name + System.Reflection.MethodBase.GetCurrentMethod().Name} should not be called after measurements were finished"
        );

        /*
            check, that frames are being processed one by one
            without skips or repeats
        */
        if (this.lastProcessedFrame != null)
        {
            if (this.lastProcessedFrame + 1 != this.videoPlayer.frame)
            {
                /*
                    the order will not be x => x+1
                    when jumping from section to section

                    setting lastProcessedFrame to null doesn't work
                        lastProcessedFrame = videoPlayer.frame
                        means the frame is still loading

                        setting lastProcessedFrame to null
                        will loose this information
                        and we won't know how many frames to skip
                */
                Assert(
                    this.videoPlayer.frame == this.sections[0].start,
                    $"Incorrect frame processing order. Current frame: {this.videoPlayer.frame}. Previous frame: {this.lastProcessedFrame}"
                );
            }
        }
        this.lastProcessedFrame = this.videoPlayer.frame;

        Graphics.Blit(
            this.videoPlayer.texture,
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesFullRes.rtInput
        );

        this.launchParameters.dispatcher.clusteringRTsAndBuffers.Downsample(
            this.csHighlightRemoval,
            staggeredJitter: this.launchParameters.staggeredJitter,
            doDownscale: this.launchParameters.doDownscale
        );

        switch (this.logType)
        {
            case ClusteringTest.LogType.Variance:
                this.ProcessFrame_VarianceMode();
                break;
            case ClusteringTest.LogType.FrameTime:
                ProcessFrame_FrameTimeMode();
                break;
            default:
                new System.NotImplementedException($"Log type not implemented: {this.logType}");
                break;
        }

        this.AdvanceFrame();

        this.RenderResult(dst);
    }

    private void AdvanceFrame()
    {
        // if the frame we just processed is the last in current section
        if (this.videoPlayer.frame == this.sections[0].end - 1)
        {
            // drop processed section
            this.sections.RemoveAt(0);

            // if there are no sections left
            if (this.sections.Count == 0)
            {
                this.finished = true;
            }
            // if there are sections left
            else
            {
                this.videoPlayer.frame = this.sections[0].start;
                /*
                    ! we do not reset this.lastProcessedFrame

                    while the video is loading
                    this.lastProcessedFrame == this.videoPlayer.frame
                    we can use this to wait for the frame to load
                */

                // new init for each new section
#pragma warning disable 162
                if (ClusteringRTsAndBuffers.randomInit)
                {
                    this.launchParameters.dispatcher.clusteringRTsAndBuffers.RandomizeClusterCenters();
                }
                else
                {
                    this.launchParameters.dispatcher.clusteringRTsAndBuffers.SetDeterministicClusterCenters();
                }
#pragma warning restore 162
            }
        }
        // if the frame we just processed is not the last in current section
        else
        {
            this.videoPlayer.frame++;
        }
    }

    /// <summary>
    /// Makes a variance log entry for the current cluster centers.
    /// </summary>
    private void MakeVarianceLogEntry()
    {
        this.benchmarkMeasurementVariance.frameVarianceRecords.Add(
            new BenchmarkMeasurementVariance.FrameVarianceRecord(
                frameIndex: this.videoPlayer.frame,
                variance: this.launchParameters.dispatcher.GetVariance()
            )
        );
    }

    /// <summary>
    /// Runs clustering iterations plus one final attribution. Without the final attribution we would have the latest cluster centers, but we wouldn't know which pixels belong to which cluster. The additional attribution does not create a "bonus" iteration, because the next frame starts with new attribution (this is required as the input has changed).
    /// </summary>
    private void RunDispatcher()
    {
        this.launchParameters.dispatcher.RunClustering(
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes
        );

        /*
            RunClustering finishes with updating cluster centers
            so we have the latest cluster centers
            but we don't have appropriate attribution

            this final attribution does not create a "bonus" iteration
            because the next frame starts with attribution
        */
        this.launchParameters.dispatcher.AttributeClustersKM(
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes
        );
    }

    public void RenderResult(RenderTexture target)
    {
        this.csHighlightRemoval.SetTexture(
            this.kernelShowResult,
            "tex_arr_clusters_r",
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes.rtArr
        );

        this.csHighlightRemoval.SetTexture(
            this.kernelShowResult,
            "tex_output",
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult
        );

        this.csHighlightRemoval.SetBuffer(
            this.kernelShowResult,
            "cbuf_cluster_centers",
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.cbufClusterCenters
        );

        this.csHighlightRemoval.SetTexture(
            this.kernelShowResult,
            "tex_input",
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes.rtInput
        );

        this.csHighlightRemoval.Dispatch(
            this.kernelShowResult,
            Math.Max(
                this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult.width
                    / ClusteringTest.kernelSize,
                1
            ),
            Math.Max(
                this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult.height
                    / ClusteringTest.kernelSize,
                1
            ),
            1
        );

        Graphics.Blit(this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult, target);

        this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult.DiscardContents();
    }

    public void Dispose()
    {
        this.launchParameters.Dispose();
    }
}
