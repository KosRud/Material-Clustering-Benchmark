using UnityEngine;
using System.Collections.Generic;
using WorkGeneration;

public class ClusteringTest : MonoBehaviour
{
    /// <summary>
    /// Must match the shader.
    /// </summary>
    public const int maxNumClusters = 32;

    /// <summary>
    /// Must match the shader.
    /// </summary>
    public const int kernelSize = 16;
    public const int fullTextureSize = 512;

    public Stack<WorkList> workLists;
    public WorkList currentWorkList;
    public ComputeShader csHighlightRemoval;
    public const string varianceLogPath = "Variance logs";

    public int numTotalRuns;
    public int numTotalFinishedRuns;
    public int numCurWorkListFinishedRuns;
    public int numCurWorkListRuns;
    public int warningCounter => this.measurementRunner.warningCounter;

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

        this.currentWorkList = this.workLists.Pop();
        this.numCurWorkListFinishedRuns = 0;
        this.numCurWorkListRuns = this.currentWorkList.runs.Count;

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentWorkList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentWorkList.logType,
            csHighlightRemoval: this.csHighlightRemoval
        );

        Debug.Log(this.measurementRunner.paramsJSON);
    }

    private void OnFinishedRunner()
    {
        this.measurementRunner.Dispose();

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

            this.reportCollection = new BenchmarkReportCollection();
        }

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentWorkList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentWorkList.logType,
            csHighlightRemoval: this.csHighlightRemoval
        );

        Debug.Log(this.measurementRunner.paramsJSON);
    }

    // Update is called once per frame
    private void Update() { }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //try
        //{
        this.measurementRunner.ProcessNextFrame(src, dest);
        /*}
        catch (System.Exception e)
        {
            string msg = $"Exception during processing a frame ({e.GetType().Name}): {e.Message}";

            System.IO.File.WriteAllText($"Reports/Error.txt", msg);
            Debug.Log(msg);

            this.enabled = false;
            Application.Quit();
            throw e;
        }*/

        if (this.measurementRunner.finished == true)
        {
            this.reportCollection.reports.Add(this.measurementRunner.GetReport());
            this.OnFinishedRunner();
            this.numCurWorkListFinishedRuns++;
            this.numTotalFinishedRuns++;
        }
    }
}
