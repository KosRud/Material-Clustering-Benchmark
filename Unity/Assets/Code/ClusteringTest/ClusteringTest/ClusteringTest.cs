using UnityEngine;
using System.Collections.Generic;
using BenchmarkGeneration;

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

    public Stack<BenchmarkDescription> benchmarkStack;
    public BenchmarkDescription currentBenchmark;
    public ComputeShader csHighlightRemoval;
    public const string varianceLogPath = "Variance logs";

    public int numTotalDispatches;
    public int numTotalFinishedDispatches;

    public int numCurBenchmarkDispatches;
    public int numCurBenchmarkFinishedDispatches;

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
        this.benchmarkStack = new Stack<BenchmarkDescription>();
    }

    private void OnEnable()
    {
        this.reportCollection = new BenchmarkReportCollection();

        this.numTotalDispatches = 0;
        foreach (BenchmarkDescription workList in this.benchmarkStack)
        {
            this.numTotalDispatches += workList.dispatches.Count;
        }
        this.numTotalFinishedDispatches = 0;

        this.currentBenchmark = this.benchmarkStack.Pop();
        this.numCurBenchmarkFinishedDispatches = 0;
        this.numCurBenchmarkDispatches = this.currentBenchmark.dispatches.Count;

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentBenchmark.dispatches.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentBenchmark.logType,
            csHighlightRemoval: this.csHighlightRemoval
        );

        Debug.Log(this.measurementRunner.paramsJSON);
    }

    private void OnFinishedRunner()
    {
        this.measurementRunner.Dispose();

        if (this.currentBenchmark.dispatches.Count == 0)
        {
            System.IO.File.WriteAllText(
                $"Reports/{this.currentBenchmark.name}.json",
                JsonUtility.ToJson(this.reportCollection)
            );

            if (this.benchmarkStack.Count == 0)
            {
                this.enabled = false;
                return;
            }

            this.currentBenchmark = this.benchmarkStack.Pop();
            this.numCurBenchmarkFinishedDispatches = 0;
            this.numCurBenchmarkDispatches = this.currentBenchmark.dispatches.Count;

            this.reportCollection = new BenchmarkReportCollection();
        }

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.currentBenchmark.dispatches.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.currentBenchmark.logType,
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
            this.numCurBenchmarkFinishedDispatches++;
            this.numTotalFinishedDispatches++;
        }
    }
}
