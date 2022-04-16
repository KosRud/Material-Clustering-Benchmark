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
        public string name;
        public bool enabled;
        public WorkList workList;

        public WorkOption(string name, WorkList workList)
        {
            this.name = name;
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
                "Algorithms convergence",
                new AlgorithmsConvergence(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Empty cluster randomization",
                new WorkGeneration.EmptyClusterRandomization(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Frame time",
                new WorkGeneration.FrameTime(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Random swap (1KM) vs Random swap (2km)",
                new WorkGeneration.Rs1VsRs2(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Random swap with stop condition",
                new WorkGeneration.RsStopCondition(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Scaling vs subsampling",
                new WorkGeneration.ScalingVsSubsampling(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Scanline jitter",
                new WorkGeneration.ScanlineJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Staggered jitter",
                new WorkGeneration.StaggeredJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                "Subsampling",
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
                option.enabled = GUILayout.Toggle(option.enabled, option.name);
            }
        }
        GUILayout.EndVertical();
    }
}
