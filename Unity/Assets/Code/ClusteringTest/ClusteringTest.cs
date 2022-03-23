using UnityEngine;

public class ClusteringTest : MonoBehaviour {
    // configuration
    private enum LogType {
        FrameTime,
        Variance
    }

    public bool skip = false;

    private const int referenceTextureSize = 512;
    private const int kernelSize = 16;
    private const LogType logType = LogType.FrameTime;

    private const string varianceLogPath = "Variance logs";

    private readonly long? overrideStartFrame = null;
    private readonly long? overrideEndFrame = null;

    // textures and buffers
    private RenderTexture rtInput;
    private RenderTexture rtReference;
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
    private float? timeStart;
    private float? timePreviousFrame;
    private float durationLongestFrame;
    private long framesProcessed;

    private enum Algorithm {
        KM,
        KHM,
        RS_1KM,
        RS_2KM,
        RS_2KM_readback,
        Alternating,
        OneKM
    }

    private readonly System.Collections.Generic.Stack<LaunchParameters> work =
        new System.Collections.Generic.Stack<LaunchParameters>();

    private bool awaitingRestart = false;
    private int[][] offsets;
    private readonly int[] scanlinePixelOffset = new int[2];
    private readonly System.Collections.Generic.List<float> frameLogVariance = new System.Collections.Generic.List<float>();
    private LaunchParameters currentWorkParameters;

    private UnityEngine.Video.VideoPlayer videoPlayer;

    // public
    public ComputeShader csHighlightRemoval;
    public UnityEngine.Video.VideoClip[] videos;

    public class LaunchParameters {
        public string GetFileName() {
            string videoName = this.video.name;
            int numIterations = this.clusteringAlgorithmDispatcher.numIterations;
            int textureSize = this.textureSize;
            int numClusters = this.clusteringAlgorithmDispatcher.numClusters;
            int jitterSize = this.jitterSize;
            bool staggeredJitter = this.staggeredJitter;
            bool doDownscale = this.doDownscale;
            string algorithm = this.clusteringAlgorithmDispatcher.descriptionString;
            bool doRandomizeEmptyClusters = this.clusteringAlgorithmDispatcher.doRandomizeEmptyClusters;

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

        public readonly int textureSize;
        public readonly bool staggeredJitter;
        public readonly int jitterSize;
        public readonly UnityEngine.Video.VideoClip video;
        public readonly bool doDownscale;
        public readonly AClusteringAlgorithmDispatcher clusteringAlgorithmDispatcher;

        private LaunchParameters() { }

        public LaunchParameters(
            int textureSize,
            bool staggeredJitter,
            int jitterSize,
            UnityEngine.Video.VideoClip video,
            bool doDownscale,
            AClusteringAlgorithmDispatcher clusteringAlgorithmDispatcher
        ) {
            this.textureSize = textureSize;
            this.staggeredJitter = staggeredJitter;
            this.jitterSize = jitterSize;
            this.video = video;
            this.doDownscale = doDownscale;
            this.clusteringAlgorithmDispatcher = clusteringAlgorithmDispatcher;
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
                this.currentWorkParameters.textureSize & (this.currentWorkParameters.textureSize - 1)
            ) == 0 && this.currentWorkParameters.textureSize > 0
        ); // positive power of 2
        Debug.Assert(this.currentWorkParameters.textureSize <= referenceTextureSize);

        this.csHighlightRemoval.SetInt("mip_level", this.MipLevel(this.currentWorkParameters.textureSize));
        this.csHighlightRemoval.SetInt("ref_mip_level", this.MipLevel(referenceTextureSize));
        this.csHighlightRemoval.SetInt("texture_size", this.currentWorkParameters.textureSize);
    }

    private void InitRTs() {
        this.rtReference = new RenderTexture(
            referenceTextureSize,
            referenceTextureSize,
            0,
            RenderTextureFormat.ARGBFloat
        );

        this.rtResult = new RenderTexture(
            this.currentWorkParameters.textureSize,
            this.currentWorkParameters.textureSize,
            0,
            RenderTextureFormat.ARGBFloat
        ) {
            enableRandomWrite = true
        };

        this.rtInput = new RenderTexture(
            this.currentWorkParameters.textureSize,
            this.currentWorkParameters.textureSize,
            0,
            RenderTextureFormat.ARGBFloat
        ) {
            enableRandomWrite = true
        };
    }

    private void InitCbufs() {
        this.cbufRandomPositions = new ComputeBuffer(
            this.currentWorkParameters.clusteringAlgorithmDispatcher.numClusters,
            sizeof(int) * 4
        );
    }

    private void FindKernels() {
        this.kernelShowResult = this.csHighlightRemoval.FindKernel("ShowResult");
        this.kernelSubsample = this.csHighlightRemoval.FindKernel("SubSample");
    }

    private void Awake() {
        Debug.Assert(this.videos.Length != 0);
        WorkGenerator.GenerateWorkFrameTime(this.work, kernelSize, this.videos, this.csHighlightRemoval);
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
        this.framesProcessed = 0;
        this.timeStart = null;
        this.timePreviousFrame = null;
        this.durationLongestFrame = 0;

        this.InitJitterOffsets();
        this.FindKernels();
        this.SetTextureSize();
        this.InitRTs();
        this.InitCbufs();
        this.clusteringRTsAndBuffers = new ClusteringRTsAndBuffers(
            this.currentWorkParameters.clusteringAlgorithmDispatcher.numClusters,
            this.currentWorkParameters.textureSize,
            referenceTextureSize,
            this.rtReference
        );

        this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
        this.videoPlayer.playbackSpeed = 0;
        this.videoPlayer.clip = this.currentWorkParameters.video;
        this.videoPlayer.Play();
        this.videoPlayer.frame = this.GetStartFrame();
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
        string fileName = $"{varianceLogPath}/{this.currentWorkParameters.GetFileName()}";

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

        this.timePreviousFrame ??= Time.time;
        this.durationLongestFrame = Mathf.Max(
            this.durationLongestFrame,
            Time.time - (float)this.timePreviousFrame
        );
        this.timePreviousFrame = Time.time;

        this.timeStart ??= Time.time;
        this.framesProcessed++;

        if (this.videoPlayer.frame == this.GetEndFrame() || this.skip) {
            this.skip = false;

            this.awaitingRestart = true;
            Graphics.Blit(src, dest);

            switch (logType) {
                case LogType.Variance:
                case LogType.FrameTime:
                    this.WriteFrameTimeLog(
                        avgFrameTime: (Time.time - (float)this.timeStart) / this.framesProcessed * 1000,
                        peakFrameTime: this.durationLongestFrame * 1000
                    );
                    break;
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

        Graphics.Blit(this.videoPlayer.texture, this.rtReference);

        this.csHighlightRemoval.SetInt("sub_sample_multiplier", referenceTextureSize / this.currentWorkParameters.textureSize);
        if (this.currentWorkParameters.staggeredJitter) {
            this.csHighlightRemoval.SetInts(
                "sub_sample_offset",
                this.offsets[
                    Time.frameCount % this.offsets.Length
                ]
            );
        } else {
            this.scanlinePixelOffset[0] = Time.frameCount % this.currentWorkParameters.jitterSize;
            this.scanlinePixelOffset[1] = (Time.frameCount / this.offsets.Length) % this.currentWorkParameters.jitterSize;
            this.csHighlightRemoval.SetInts(
                "sub_sample_offset",
                this.scanlinePixelOffset
            );
        }
        this.csHighlightRemoval.SetTexture(this.kernelSubsample, "tex_input", this.rtReference);
        this.csHighlightRemoval.SetTexture(this.kernelSubsample, "tex_output", this.rtInput);
        this.csHighlightRemoval.SetBool("downscale", this.currentWorkParameters.doDownscale);
        this.csHighlightRemoval.Dispatch(
            this.kernelSubsample,
            this.currentWorkParameters.textureSize / kernelSize,
            this.currentWorkParameters.textureSize / kernelSize,
            1
        );

        /*
            in order to re-start the clustering "from scratch"
            all we need to do is to reset the cluster centers

            clustering only edits:
                * attribution texture array
                * cluster centers ComputeBuffer

            clustering starts with attribution
            so the texture array will be messed by the reset of cluster centers
        */

        this.currentWorkParameters.clusteringAlgorithmDispatcher.RunClustering(
            this.rtInput,
            this.currentWorkParameters.textureSize,
            this.clusteringRTsAndBuffers
        );

        /*
            one final attribution is required,
            because RunClustering finishes with updating cluster centers
        */
        this.currentWorkParameters.clusteringAlgorithmDispatcher.AttributeClusters(
            this.rtInput,
            this.clusteringRTsAndBuffers,
            final: true,
            khm: false
        );

        if (logType == LogType.Variance) {
        }

        this.RenderResult();
        if (this.currentWorkParameters.clusteringAlgorithmDispatcher is ClusteringAlgorithmDispatcherDummy) {
            Graphics.Blit(this.videoPlayer.texture, dest);
        } else {
            Graphics.Blit(this.rtResult, dest);
        }
        this.rtResult.DiscardContents();

        this.videoPlayer.StepForward();
    }

    private void OnDisable() {
        this.rtResult.Release();
        this.rtInput.Release();
        this.rtReference.Release();
        this.cbufRandomPositions.Release();

        this.clusteringRTsAndBuffers.Release();
    }

    private void RenderResult() {
        this.csHighlightRemoval.SetTexture(
            this.kernelShowResult, "tex_arr_clusters_r",
            this.clusteringRTsAndBuffers.rtArr
        );
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_output", this.rtResult);
        this.csHighlightRemoval.SetBuffer(this.kernelShowResult, "cbuf_cluster_centers", this.clusteringRTsAndBuffers.cbufClusterCenters);
        this.csHighlightRemoval.SetBool("show_reference", false);
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input", this.rtInput);
        this.csHighlightRemoval.Dispatch(
            this.kernelShowResult,
            this.currentWorkParameters.textureSize / kernelSize,
            this.currentWorkParameters.textureSize / kernelSize,
            1
        );
    }
}
