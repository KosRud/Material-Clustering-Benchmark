using UnityEngine;
using WorkGeneration;
using System.Collections.Generic;
using static Diagnostics;

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

    private GUIStyle guiStyle;

    private void InitGuiStyle()
    {
        const int size = 16;
        Texture2D guiBackgroundTex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        for (int i = 0; i < size * size; i++)
        {
            colors[i] = Camera.main.backgroundColor;
        }
        guiBackgroundTex.SetPixels(colors);
        guiBackgroundTex.Apply();

        this.guiStyle = new GUIStyle();
        guiStyle.normal.background = guiBackgroundTex;
    }

    private void Awake()
    {
        Assert(this.videos.Length != 0, "No video files provided.");
        Assert(
            System.Diagnostics.Stopwatch.IsHighResolution,
            "High resolution timer not available."
        );

        InitGuiStyle();

        this.workOptions = new List<WorkOption>();

        this.frameTimeWorkOption = new WorkOption(
            new FrameTime(
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
                new EmptyClusterRandomization(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new RSnumKM(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new KHMp(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new ScalingVsSubsampling(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new ScanlineJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new StaggeredJitter(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );

        this.workOptions.Add(
            new WorkOption(
                new Subsampling(
                    kernelSize: ClusteringTest.kernelSize,
                    videos: this.videos,
                    csHighlightRemoval: this.csHighlightRemoval
                ).GenerateWork()
            )
        );
    }

    private static void GuiLine()
    {
        GUILayout.Label("-----------------------------------------");
    }

    /// <summary>
    /// Warning: even empty <see cref="OnGUI" /> function creates GC allocations! For frame time measurements to be accurate there should be no GC allocations.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginVertical(guiStyle);
        {
            if (this.clusteringTest.enabled)
            {
                foreach (WorkOption option in this.workOptions)
                {
                    if (option.enabled)
                    {
                        GUILayout.Label("- " + option.workList.name);
                    }
                }

                GuiLine();

                GUILayout.Label(
                    $"Total: {this.clusteringTest.numTotalFinishedRuns} / {this.clusteringTest.numTotalRuns}"
                );
                GUILayout.Label(
                    $"{this.clusteringTest.currentWorkList.name}: {this.clusteringTest.numCurWorkListFinishedRuns} / {this.clusteringTest.numCurWorkListRuns}"
                );
                GUILayout.Label($"warnings: {this.clusteringTest.warningCounter}");
                ;
            }
            else
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
                }
            }
        }
        GUILayout.EndVertical();
    }
}
