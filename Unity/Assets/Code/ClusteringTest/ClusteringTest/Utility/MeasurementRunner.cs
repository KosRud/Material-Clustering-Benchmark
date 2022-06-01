using UnityEngine;
using ClusteringAlgorithms;
using System;
using System.Collections.Generic;

public class MeasurementRunner : IDisposable
{
    private const int sectionLength = 1000;
    private const int totalSections = 20; // counting repeats as unique sections

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

    private void FillSectionList(long numFrames)
    {
        this.sections = new List<VideoSection>();

        long numSections = numFrames / sectionLength;
        Debug.Assert(numSections > 0);
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

    private readonly bool noGcAvailable;

    private readonly ComputeShader csHighlightRemoval;
    private readonly int kernelShowResult;

    private readonly WorkGeneration.LaunchParameters launchParameters;

    private readonly System.Diagnostics.Stopwatch stopwatch;

    private UnityEngine.Video.VideoPlayer videoPlayer;
    private readonly long frameStart;
    private readonly long? frameEndOrNull;

    private readonly BenchmarkMeasurementVariance benchmarkMeasurementVariance;
    private readonly BenchmarkMeasurementFrameTime benchmarkMeasurementFrameTime;

    private readonly ClusteringTest.LogType logType;

    public string paramsJSON => JsonUtility.ToJson(this.launchParameters.GetSerializable());

    private long? lastProcessedFrame;

    public bool finished;

    /// <summary>
    /// Takes ownership of launchParameters
    /// </summary>
    public MeasurementRunner(
        WorkGeneration.LaunchParameters launchParameters,
        UnityEngine.Video.VideoPlayer videoPlayer,
        long? frameStart,
        long? frameEnd,
        ClusteringTest.LogType logType,
        ComputeShader csHighlightRemoval,
        bool noGcAvailable
    )
    {
        this.csHighlightRemoval = csHighlightRemoval;
        this.kernelShowResult = csHighlightRemoval.FindKernel("ShowResult");
        this.logType = logType;
        this.launchParameters = launchParameters;
        this.stopwatch = new System.Diagnostics.Stopwatch();
        this.lastProcessedFrame = null;
        this.benchmarkMeasurementVariance = new BenchmarkMeasurementVariance();
        this.benchmarkMeasurementFrameTime = new BenchmarkMeasurementFrameTime();
        this.frameEndOrNull = frameEnd;
        this.frameStart = frameStart ?? 0;
        this.noGcAvailable = noGcAvailable;
        this.finished = false;

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
        this.videoPlayer.frame = frameStart ?? 0;
    }

    private void SetTextureSize()
    {
        int workingTextureSize = this.launchParameters
            .dispatcher
            .clusteringRTsAndBuffers
            .workingSize;

        int fullTextureSize = this.launchParameters.dispatcher.clusteringRTsAndBuffers.fullSize;

        Debug.Assert(
            // positive power of 2
            (workingTextureSize & (workingTextureSize - 1)) == 0
                && workingTextureSize > 0
        );
        Debug.Assert(workingTextureSize <= fullTextureSize);

        this.csHighlightRemoval.SetInt("texture_size", workingTextureSize);
    }

    public BenchmarkReport GetReport()
    {
        return new BenchmarkReport(
            measurement: this.logType switch
            {
                ClusteringTest.LogType.Variance => this.benchmarkMeasurementVariance,
                ClusteringTest.LogType.FrameTime => this.benchmarkMeasurementFrameTime,
                _ => throw new System.NotImplementedException()
            },
            serializableLaunchParameters: this.launchParameters.GetSerializable(),
            logType: this.logType
        );
    }

    public void ProcessNextFrame(RenderTexture src, RenderTexture dst)
    {
        if (
            // not yet loaded video file
            this.videoPlayer.frame == -1
            // not yet loaded next frame
            || this.lastProcessedFrame == this.videoPlayer.frame
        )
        {
            Graphics.Blit(src, dst);
            return;
        }

        /*
            ProcessNextFrame() should not be called
            after measurements were finished
        */
        Debug.Assert(this.finished == false);

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
                if (this.videoPlayer.frame != this.sections[0].start)
                {
                    throw new System.Exception(
                        $"Incorrect frame processing order. Current frame: {this.videoPlayer.frame}. Previous frame: {this.lastProcessedFrame}"
                    );
                }
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

        if (
            this.logType == ClusteringTest.LogType.Variance
            /*
                first frame does not have valid variance
                so trying to extract cluster centers
                will result in a validation error
            */
            || this.videoPlayer.frame == this.frameStart
        )
        {
            this.RunDispatcher();

            if (this.logType == ClusteringTest.LogType.Variance)
            {
                this.MakeVarianceLogEntry();
            }
        }
        else
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

                if (this.noGcAvailable)
                {
                    Debug.Assert(System.GC.TryStartNoGCRegion(0));
                }
                // no GC section
                {
                    this.stopwatch.Reset();
                    this.stopwatch.Start();
                    // measured section
                    {
                        for (int i = 0; i < numRepetitions; i++)
                        {
                            this.launchParameters.dispatcher.clusteringRTsAndBuffers.SetClusterCenters(
                                clusterCenters.centers
                            );
                            this.RunDispatcher();
                        }

                        using (
                            ClusterCenters temp =
                                this.launchParameters.dispatcher.clusteringRTsAndBuffers.GetClusterCenters()
                        )
                        {
                            temp.centers[0] = temp.centers[1];
                            // force current thread to wait until GPU finishes computations
                        }
                    }
                    this.stopwatch.Stop();
                }
                if (this.noGcAvailable)
                {
                    GC.EndNoGCRegion();
                }

                float measuredtimeMS = (float)this.stopwatch.Elapsed.TotalMilliseconds;
                float avgTime = measuredtimeMS / numRepetitions;

                this.benchmarkMeasurementFrameTime.frameTimeRecords.Add(
                    new BenchmarkMeasurementFrameTime.FrameTimeRecord(
                        frameIndex: this.videoPlayer.frame,
                        time: avgTime
                    )
                );
            }
        }

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
            }
        }
        // if the frame we just processed is not the last in current section
        else
        {
            this.videoPlayer.frame++;
        }

        this.RenderResult(dst);
    }

    public void AdvanceFrame()
    {
        this.videoPlayer.StepForward();
    }

    private void MakeVarianceLogEntry()
    {
        this.benchmarkMeasurementVariance.frameVarianceRecords.Add(
            new BenchmarkMeasurementVariance.FrameVarianceRecord(
                frameIndex: this.videoPlayer.frame,
                variance: this.launchParameters.dispatcher.GetVariance()
            )
        );
    }

    private void RunDispatcher()
    {
        this.launchParameters.dispatcher.RunClustering(
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes
        );

        /*
            one final attribution is required,
            because RunClustering finishes with updating cluster centers
        */
        this.launchParameters.dispatcher.AttributeClusters(
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.texturesWorkRes,
            final: true,
            khm: false
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
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult.width
                / ClusteringTest.kernelSize,
            this.launchParameters.dispatcher.clusteringRTsAndBuffers.rtResult.height
                / ClusteringTest.kernelSize,
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
