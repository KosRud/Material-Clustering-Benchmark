using UnityEngine;
using WorkGeneration;
using System.Collections.Generic;

public class ClusteringTestGui : MonoBehaviour
{
    private List<WorkOption> workOptions;
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

    private void Awake()
    {
        Debug.Assert(this.videos.Length != 0);

        this.workOptions = new List<WorkOption>();

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
                new WorkGeneration.FrameTime(
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
                foreach (WorkOption option in this.workOptions)
                {
                    if (option.enabled)
                    {
                        this.clusteringTest.workLists.Push(option.workList);
                    }
                }
                this.clusteringTest.enabled = true;
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
        GUILayout.EndVertical();
    }
}
