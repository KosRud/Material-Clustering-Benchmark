using UnityEngine;
using WorkGeneration;
using System.Collections.Generic;

public class ClusteringTestGui : MonoBehaviour
{
    private List<WorkOption> workOptions;
    private WorkOption frameTimeWorkOption;

    public UnityEngine.Video.VideoClip[] videos;
    public ComputeShader csHighlightRemoval;

    public ClusteringTest clusteringTest;

    private class WorkOption
    {
        public bool enabled;
        public WorkList workList;

        public WorkOption(WorkList workList)
        {
            this.enabled = false;
            this.workList = workList;
        }
    }

    private bool noGcAvailable;

    private void Awake()
    {
        Debug.Assert(this.videos.Length != 0);

        this.noGcAvailable = false;
        try
        {
            System.GC.TryStartNoGCRegion(0);
            this.noGcAvailable = true;
            System.GC.EndNoGCRegion();
        }
        catch (System.NotImplementedException)
        {
            Debug.Log("No GC region not implemented!");
        }

        this.workOptions = new List<WorkOption>();

        this.frameTimeWorkOption = new WorkOption(
            new WorkGeneration.FrameTime(
                kernelSize: ClusteringTest.kernelSize,
                videos: this.videos,
                csHighlightRemoval: this.csHighlightRemoval
            ).GenerateWork()
        );

        this.workOptions.Add(this.frameTimeWorkOption);

        this.workOptions.Add(
            new WorkOption(
                new AlgorithmsConvergence(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.EmptyClusterRandomization(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.Rs1VsRs2(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.ScalingVsSubsampling(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.ScanlineJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.StaggeredJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new WorkGeneration.Subsampling(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            foreach (WorkOption option in this.workOptions)
            {
                option.enabled = GUILayout.Toggle(option.enabled, option.workList.name);
            }
            if (GUILayout.Button("Start"))
            {
                if (System.IO.Directory.Exists("Reports") == false)
                {
                    System.IO.Directory.CreateDirectory("Reports");
                }

                foreach (WorkOption option in this.workOptions)
                {
                    if (option.enabled)
                    {
                        this.clusteringTest.workLists.Push(option.workList);
                    }
                }
                this.clusteringTest.enabled = true;

                if (this.frameTimeWorkOption.enabled)
                {
                    /*
                        make sure no GC allocations happen,
                        when measuring frame times
        
                        ! even an empty OnGUI function GC allocates
                    */
                    this.enabled = false;
                }
            }
            if (this.clusteringTest.enabled)
            {
                GUILayout.Label(
                    $"Total: {this.clusteringTest.numTotalFinishedRuns} / {this.clusteringTest.numTotalRuns}"
                );
                GUILayout.Label(
                    $"{this.clusteringTest.currentWorkList.name}: {this.clusteringTest.numCurWorkListFinishedRuns} / {this.clusteringTest.numCurWorkListRuns}"
                );
            }
        }
        if (this.noGcAvailable == false)
        {
            GUILayout.Label(
                "No-GC region functionality not available - frame time measurements will be inaccurate!"
            );
        }
        GUILayout.EndVertical();
    }
}
