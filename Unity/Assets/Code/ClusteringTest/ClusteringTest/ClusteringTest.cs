using UnityEngine;
using System.Collections.Generic;
using WorkGeneration;

public class ClusteringTest : MonoBehaviour
{
    public const int maxNumClusters = 16; // ! do not change
    public const int kernelSize = 8; // ! must match shader
    public const int fullTextureSize = 512;

    public Stack<WorkList> workLists;
    public WorkList currentWorkList;
    public ComputeShader csHighlightRemoval;
    public const string varianceLogPath = "Variance logs";

    private bool noGcAvailable;

    public int numTotalRuns;
    public int numTotalFinishedRuns;
    public int numCurWorkListFinishedRuns;
    public int numCurWorkListRuns;

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
        this.workLists = new Stack<WorkList>();
    }

    private void OnEnable()
    {
        this.reportCollection = new BenchmarkReportCollection();

        this.numTotalRuns = 0;
        foreach (WorkList workList in this.workLists)
        {
            this.numTotalRuns += workList.runs.Count;
        }
        this.numTotalFinishedRuns = 0;

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

        this.currentWorkList = this.workLists.Pop();
        this.numCurWorkListFinishedRuns = 0;
        this.numCurWorkListRuns = this.currentWorkList.runs.Count;

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentWorkList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentWorkList.logType,
            csHighlightRemoval: this.csHighlightRemoval,
            noGcAvailable: this.noGcAvailable
        );
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

        if (this.currentWorkList.runs.Count == 0)
        {
            System.IO.File.WriteAllText(
                $"Reports/{this.currentWorkList.name}.json",
                JsonUtility.ToJson(this.reportCollection)
            );

            if (this.workLists.Count == 0)
            {
                this.enabled = false;
                return;
            }

            this.currentWorkList = this.workLists.Pop();
            this.numCurWorkListFinishedRuns = 0;
            this.numCurWorkListRuns = this.currentWorkList.runs.Count;
        }

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentWorkList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentWorkList.logType,
            csHighlightRemoval: this.csHighlightRemoval,
            noGcAvailable: this.noGcAvailable
        );
    }

    // Update is called once per frame
    private void Update() { }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        try
        {
            this.measurementRunner.ProcessNextFrame(src, dest);
        }
        catch (System.Exception e)
        {
            System.IO.File.WriteAllText(
                $"Reports/Error.txt",
                $"Exception during processing a frame ({e.GetType().Name}): {e.Message}"
            );
            this.enabled = false;
            Application.Quit();
        }

        if (this.measurementRunner.finished == true)
        {
            this.reportCollection.reports.Add(this.measurementRunner.GetReport());
            this.OnFinishedRunner();
            this.numCurWorkListFinishedRuns++;
            this.numTotalFinishedRuns++;
        }
    }
}
