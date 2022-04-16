using UnityEngine;
using System.Collections.Generic;
using WorkGeneration;

public class ClusteringTest : MonoBehaviour
{
    public const int maxNumClusters = 16; // ! do not change
    public const int kernelSize = 8; // ! must match shader
    public const int fullTextureSize = 512;

    public Stack<WorkList> workLists;
    public ComputeShader csHighlightRemoval;
    public const string varianceLogPath = "Variance logs";

    private bool noGcAvailable;

    public enum LogType
    {
        FrameTime,
        Variance
    }

    private MeasurementRunner measurementRunner;

    // configuration
    public long? frameStart = null;
    public long? frameEnd = null;

    private BenchmarkReportCollection reportCollection;

    private void Awake()
    {
        this.reportCollection = new BenchmarkReportCollection();

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

        // must be called after checking noGcAvailable
        this.OnFinishedRunner();
    }

    private void CheckTimerPrecision()
    {
        if (System.Diagnostics.Stopwatch.IsHighResolution == false)
        {
            throw new System.NotSupportedException("High resolution timer not available!");
        }
        else
        {
            Debug.Log("High resolution timer support check: OK");
        }
    }

    private void OnFinishedRunner()
    {
        this.measurementRunner?.Dispose();

        WorkList workList = this.workLists.Peek();
        if (workList.runs.Count == 0)
        {
            System.IO.File.WriteAllText(
                $"Reports/{workList.name}.json",
                JsonUtility.ToJson(this.reportCollection)
            );

            this.workLists.Pop();
            if (this.workLists.Count == 0)
            {
                this.enabled = false;
                return;
            }
            workList = this.workLists.Peek();
            Debug.Assert(workList.runs.Count != 0);
        }
        this.measurementRunner = new MeasurementRunner(
            launchParameters: workList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: workList.logType,
            csHighlightRemoval: this.csHighlightRemoval,
            noGcAvailable: this.noGcAvailable
        );
    }

    // Update is called once per frame
    private void Update() { }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        this.measurementRunner.ProcessNextFrame(src, dest);

        if (this.measurementRunner.finished == true)
        {
            WorkList workList = this.workLists.Peek();

            this.reportCollection.reports.Add(this.measurementRunner.GetReport());

            this.OnFinishedRunner();
        }
    }
}
