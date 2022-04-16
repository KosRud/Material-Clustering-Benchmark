using UnityEngine;

public class ClusteringTest : MonoBehaviour
{
    public const int maxNumClusters = 16; // ! do not change
    public const int kernelSize = 8; // ! must match shader
    public const int fullTextureSize = 512;

    private bool noGcAvailable;

    public const string varianceLogPath = "Variance logs";

    public enum LogType
    {
        FrameTime,
        Variance
    }

    private MeasurementRunner measurementRunner;

    // configuration
    public long? frameStart = null;
    public long? frameEnd = null;

    // ToDo: is it needed?
    private enum Algorithm
    {
        KM,
        KHM,
        RS_1KM,
        RS_2KM,
        RS_2KM_readback,
        Alternating,
        OneKM
    }

    private WorkGeneration.WorkList workList;
    private BenchmarkReportCollection reportCollection;

    // public
    public ComputeShader csHighlightRemoval;
    public UnityEngine.Video.VideoClip[] videos;

    private void Awake()
    {
        Debug.Assert(this.videos.Length != 0);

        this.workList = new WorkGeneration.Subsampling(
            kernelSize: kernelSize,
            videos: this.videos,
            csHighlightRemoval: this.csHighlightRemoval
        ).GenerateWork();

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
        this.LoadNextMeasurementRunner();
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

    private void LoadNextMeasurementRunner()
    {
        this.measurementRunner?.Dispose();

        this.measurementRunner = new MeasurementRunner(
            launchParameters: this.workList.runs.Pop(),
            videoPlayer: this.GetComponent<UnityEngine.Video.VideoPlayer>(),
            frameStart: this.frameStart,
            frameEnd: this.frameEnd,
            logType: this.workList.logType,
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
            this.reportCollection.reports.Add(this.measurementRunner.GetReport());

            if (this.workList.runs.Count == 0)
            {
                System.IO.File.WriteAllText(
                    "report.json",
                    JsonUtility.ToJson(this.reportCollection)
                );
                this.enabled = false;
            }
            else
            {
                this.LoadNextMeasurementRunner();
            }
        }
    }
}
