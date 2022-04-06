using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

public class ClusteringTest : MonoBehaviour {
  public const int maxNumClusters = 16; // ! do not change
  public const int kernelSize = 16; // ! must match shader
  public const int fullTextureSize = 512;

  // configuration
  private enum LogType {
    FrameTime,
    Variance
  }

  public bool skip = false;

  private const LogType logType =
    LogType.Variance; // ToDo put it inside "work" instance

  private const string varianceLogPath = "Variance logs";

  private readonly long? overrideStartFrame = null;
  private readonly long? overrideEndFrame = null;

  // textures and buffers
  private RenderTexture rtInput;
  private RenderTexture rtInputFullSize;
  private RenderTexture rtResult;
  private ClusteringRTsAndBuffers clusteringRTsAndBuffers;

  private struct Position {
    public int x;
    public int y;
  }
  private ComputeBuffer cbufRandomPositions;

  // shader kernels
  //private int kernelAttributeClusters;
  private int kernelShowResult;
  private int kernelSubsample;

  // inner workings
  private float peakFrameTime;
  private float totalTime;
  private long framesMeasured;
  private bool firstFrameProcessed;
  private System.Diagnostics.Stopwatch stopwatch;

  private DispatcherKM kmDispatcherFullSize;

  private enum Algorithm {
    KM,
    KHM,
    RS_1KM,
    RS_2KM,
    RS_2KM_readback,
    Alternating,
    OneKM
  }

  private readonly Stack<LaunchParameters> work =
    new Stack<LaunchParameters>();

  private bool awaitingRestart = false;
  private int[][] offsets;
  private readonly int[] scanlinePixelOffset = new int[2];
  private readonly List<float> frameLogVariance = new List<float>();
  private LaunchParameters currentWorkParameters;

  private UnityEngine.Video.VideoPlayer videoPlayer;

  // public
  public ComputeShader csHighlightRemoval;
  public UnityEngine.Video.VideoClip[] videos;

  public class LaunchParameters {
    public string GetFileName() {
      string videoName = this.video.name;
      int numIterations = this.dispatcher.numIterations;
      int textureSize = this.workingTextureSize;
      int numClusters = this.dispatcher.numClusters;
      int jitterSize = this.jitterSize;
      bool staggeredJitter = this.staggeredJitter;
      bool doDownscale = this.doDownscale;
      string algorithm = this.dispatcher.descriptionString;
      bool doRandomizeEmptyClusters = this.dispatcher.doRandomizeEmptyClusters;

      return $"video file:{videoName}|number of iterations:{numIterations}|texture size:{textureSize}|number of clusters:{numClusters}|randomize empty clusters:{doRandomizeEmptyClusters}|jitter size:{jitterSize}|staggered jitter:{staggeredJitter}|downscale:{doDownscale}|algorithm:{algorithm}.csv";
    }

    public LaunchParameters ThrowIfExists() {
      string fileName = $"{varianceLogPath}/{this.GetFileName()}";

      if (System.IO.File.Exists(fileName)) {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        throw new System.Exception($"File exists: {fileName}");
      }

      return this;
    }

    public readonly int workingTextureSize;
    public readonly bool staggeredJitter;
    public readonly int jitterSize;
    public readonly UnityEngine.Video.VideoClip video;
    public readonly bool doDownscale;
    public readonly ADispatcher dispatcher;

    private LaunchParameters() { }

    public LaunchParameters(
      int workingTextureSize,
      bool staggeredJitter,
      int jitterSize,
      UnityEngine.Video.VideoClip video,
      bool doDownscale,
      ADispatcher dispatcher
    ) {
      this.workingTextureSize = workingTextureSize;
      this.staggeredJitter = staggeredJitter;
      this.jitterSize = jitterSize;
      this.video = video;
      this.doDownscale = doDownscale;
      this.dispatcher = dispatcher;
    }
  }

  private int MipLevel(int textureSize) {
    int mipLevel = 0;
    int targetSize = 1;
    while (targetSize != textureSize) {
      mipLevel++;
      targetSize *= 2;
    }
    return mipLevel;
  }

  private void SetTextureSize() {
    Debug.Assert(
      (
        this.currentWorkParameters.workingTextureSize &
        (this.currentWorkParameters.workingTextureSize
          - 1)
      ) == 0 && this.currentWorkParameters.workingTextureSize > 0
    ); // positive power of 2
    Debug.Assert(this.currentWorkParameters.workingTextureSize <= fullTextureSize);

    this.csHighlightRemoval.SetInt("mip_level",
      this.MipLevel(this.currentWorkParameters.workingTextureSize));
    this.csHighlightRemoval.SetInt("ref_mip_level",
      this.MipLevel(fullTextureSize));
    this.csHighlightRemoval.SetInt("texture_size",
      this.currentWorkParameters.workingTextureSize);
  }

  private void InitRTs() {
    this.rtInputFullSize = new RenderTexture(
      fullTextureSize,
      fullTextureSize,
      0,
      RenderTextureFormat.ARGBFloat
    );

    this.rtResult = new RenderTexture(
      this.currentWorkParameters.workingTextureSize,
      this.currentWorkParameters.workingTextureSize,
      0,
      RenderTextureFormat.ARGBFloat
    ) {
      enableRandomWrite = true
    };

    this.rtInput = new RenderTexture(
      this.currentWorkParameters.workingTextureSize,
      this.currentWorkParameters.workingTextureSize,
      0,
      RenderTextureFormat.ARGBFloat
    ) {
      enableRandomWrite = true
    };
  }

  private void InitCbufs() {
    this.cbufRandomPositions = new ComputeBuffer(
      this.currentWorkParameters.dispatcher.numClusters,
      sizeof(int) * 4
    );
  }

  private void FindKernels() {
    this.kernelShowResult = this.csHighlightRemoval.FindKernel("ShowResult");
    this.kernelSubsample = this.csHighlightRemoval.FindKernel("SubSample");
  }

  private void Awake() {
    Debug.Assert(this.videos.Length != 0);

    new WorkGenerator.AlgorithmsConvergence(
      kernelSize: kernelSize,
      videos: this.videos,
      csHighlightRemoval: this.csHighlightRemoval
    ).GenerateWork(
      this.work
    );

    // check timer precision
    if (System.Diagnostics.Stopwatch.IsHighResolution == false) {
      throw new System.NotSupportedException("High resolution timer not available!");
    } else {
      Debug.Log("High resolution timer support check: OK");
    }

    this.stopwatch = new System.Diagnostics.Stopwatch();
    this.FindKernels();
  }

  private void InitJitterOffsets() {
    this.offsets = JitterPattern.Get(this.currentWorkParameters.jitterSize);
  }

  private long GetStartFrame() {
    return this.overrideStartFrame ?? 0;
  }

  private long GetEndFrame() {
    return this.overrideEndFrame ?? (long)this.videoPlayer.frameCount - 1;
  }

  private void OnEnable() {
    if (this.enabled == false) {
      return;
    }

    this.currentWorkParameters = this.work.Pop();

    Debug.Log($"work left: {this.work.Count}");
    Debug.Log($"processing: {this.currentWorkParameters.GetFileName()}");

    this.frameLogVariance.Clear();
    this.framesMeasured = 0;
    this.totalTime = 0;
    this.peakFrameTime = 0;
    this.firstFrameProcessed = false;

    this.InitJitterOffsets();
    this.SetTextureSize();
    this.InitRTs();
    this.InitCbufs();
    this.clusteringRTsAndBuffers = new ClusteringRTsAndBuffers(
      this.currentWorkParameters.dispatcher.numClusters,
      this.currentWorkParameters.workingTextureSize,
      this.csHighlightRemoval
    );

    this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
    this.videoPlayer.playbackSpeed = 0;
    this.videoPlayer.clip = this.currentWorkParameters.video;
    this.videoPlayer.Play();
    this.videoPlayer.frame = this.GetStartFrame();

    this.kmDispatcherFullSize = new DispatcherKM(
      kernelSize: kernelSize,
      computeShader: this.csHighlightRemoval,
      numIterations: 1,
      doRandomizeEmptyClusters: false,
      numClusters: this.currentWorkParameters.dispatcher.numClusters
    );
  }

  // Update is called once per frame
  private void Update() {

  }

  private bool ValidateRandomSwapParams(int iterationsKM, int iterations) {
    if (iterations <= 1) {
      return false;
    }
    return iterationsKM == 1 || iterations % iterationsKM == 1;
  }

  private void WriteVarianceLog() {
    string fileName =
      $"{varianceLogPath}/{this.currentWorkParameters.GetFileName()}";

    if (System.IO.File.Exists(fileName)) {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#endif
      throw new System.Exception($"File exists: {fileName}");
    }

    using (
      System.IO.FileStream fs = System.IO.File.Open(
          fileName, System.IO.FileMode.OpenOrCreate
        )
    ) {
      using var sw = new System.IO.StreamWriter(fs);
      sw.WriteLine("Frame,Variance");
      for (int i = 0; i < this.frameLogVariance.Count; i++) {
        float Variance = this.frameLogVariance[i];
        if (Variance == -1) {
          sw.WriteLine(
            $"{i}"
          );
        } else {
          sw.WriteLine(
            $"{i},{Variance}"
          );
        }
      }
    }
    Debug.Log($"file written: {fileName}");
  }

  private void WriteFrameTimeLog(float avgFrameTime, float peakFrameTime) {
    string fileName = "Frame time log.txt";

    using System.IO.FileStream fs = System.IO.File.Open(
        fileName, System.IO.FileMode.Append
      );
    using var sw = new System.IO.StreamWriter(fs);
    sw.WriteLine(this.currentWorkParameters.GetFileName());
    sw.WriteLine($"Average frame time: {avgFrameTime:0.000} ms");
    sw.WriteLine($"   Peak frame time: {peakFrameTime:0.000} ms");
    sw.WriteLine();
  }

  private void OnRenderImage(RenderTexture src, RenderTexture dest) {
    if (this.videoPlayer.frame < this.GetEndFrame()) {
      this.awaitingRestart = false;
    }

    if (this.videoPlayer.frame == -1) {
      Graphics.Blit(src, dest);
      return;
    }

    if (this.awaitingRestart) {
      Graphics.Blit(src, dest);
      return;
    }

    if (this.videoPlayer.frame == this.GetEndFrame() || this.skip) {
      this.skip = false;

      this.awaitingRestart = true;
      Graphics.Blit(src, dest);

      switch (logType) {
        case LogType.Variance:
          this.WriteVarianceLog();
          break;
        case LogType.FrameTime:
        default:
          throw new System.NotImplementedException();
      }

      if (this.work.Count == 0) {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Destroy(this);
      }

      this.OnDisable();
      this.OnEnable();

      return;
    }

    Graphics.Blit(this.videoPlayer.texture, this.rtInputFullSize);

    this.csHighlightRemoval.SetInt("sub_sample_multiplier",
      fullTextureSize / this.currentWorkParameters.workingTextureSize);
    if (this.currentWorkParameters.staggeredJitter) {
      this.csHighlightRemoval.SetInts(
        "sub_sample_offset",
        this.offsets[
      Time.frameCount % this.offsets.Length
      ]
      );
    } else {
      this.scanlinePixelOffset[0] = Time.frameCount %
        this.currentWorkParameters.jitterSize;
      this.scanlinePixelOffset[1] = (Time.frameCount / this.offsets.Length) %
        this.currentWorkParameters.jitterSize;
      this.csHighlightRemoval.SetInts(
        "sub_sample_offset",
        this.scanlinePixelOffset
      );
    }
    this.csHighlightRemoval.SetTexture(this.kernelSubsample, "tex_input",
      this.rtInputFullSize);
    this.csHighlightRemoval.SetTexture(this.kernelSubsample, "tex_output",
      this.rtInput);
    this.csHighlightRemoval.SetBool("downscale",
      this.currentWorkParameters.doDownscale);
    this.csHighlightRemoval.Dispatch(
      this.kernelSubsample,
      this.currentWorkParameters.workingTextureSize / kernelSize,
      this.currentWorkParameters.workingTextureSize / kernelSize,
      1
    );

    if (logType == LogType.Variance || this.firstFrameProcessed == false) {
      this.RunDispatcher();

      if (logType == LogType.Variance) {
        this.MakeVarianceLogEntry();
      }

      this.firstFrameProcessed = true;
    } else {
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
        ClusterCenters clusterCenters = this.clusteringRTsAndBuffers.GetClusterCenters()
      ) {
        const int numRepetitions = 10;

        this.stopwatch.Reset();
        this.stopwatch.Start();
        // measured section
        {
          for (int i = 0; i < numRepetitions; i++) {
            this.clusteringRTsAndBuffers.SetClusterCenters(clusterCenters.centers);
            this.RunDispatcher();
          }

          using (
            ClusterCenters temp = this.clusteringRTsAndBuffers.GetClusterCenters()
          ) {
            temp.centers[0] = temp.centers[1];
            // force current thread to wait until GPU finishes computations
          }
        }
        this.stopwatch.Stop();

        float measuredtimeMS = (float)this.stopwatch.Elapsed.TotalMilliseconds;
        float avgTime = measuredtimeMS / numRepetitions;

        this.totalTime += avgTime;
        this.peakFrameTime = Mathf.Max(
            this.peakFrameTime,
            avgTime
          );

        this.framesMeasured++;
      }
    }

    this.RenderResult(dest);

    this.videoPlayer.StepForward();
  }

  private void MakeVarianceLogEntry() {
    this.frameLogVariance.Add(this.clusteringRTsAndBuffers.GetVariance(
        this.rtInputFullSize));
  }

  private void RunDispatcher() {
    this.currentWorkParameters.dispatcher.RunClustering(
      this.rtInput,
      this.currentWorkParameters.workingTextureSize,
      this.clusteringRTsAndBuffers
    );

    /*
        one final attribution is required,
        because RunClustering finishes with updating cluster centers
    */
    this.kmDispatcherFullSize.AttributeClusters(
      this.rtInput,
      this.clusteringRTsAndBuffers,
      final: true,
      khm: false
    );
  }

  private void OnDisable() {
    this.rtResult.Release();
    this.rtInput.Release();
    this.rtInputFullSize.Release();
    this.cbufRandomPositions.Release();

    this.clusteringRTsAndBuffers.Release();
  }

  private void RenderResult(RenderTexture target) {
    this.csHighlightRemoval.SetTexture(
      this.kernelShowResult, "tex_arr_clusters_r",
      this.clusteringRTsAndBuffers.rtArr
    );
    this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_output",
      this.rtResult);
    this.csHighlightRemoval.SetBuffer(this.kernelShowResult, "cbuf_cluster_centers",
      this.clusteringRTsAndBuffers.cbufClusterCenters);
    this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input",
      this.rtInput);
    this.csHighlightRemoval.Dispatch(
      this.kernelShowResult,
      this.rtResult.width / kernelSize,
      this.rtResult.height / kernelSize,
      1
    );

    Graphics.Blit(this.rtResult, target);
    this.rtResult.DiscardContents();
  }
}
