using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
    // configuration
    enum LogType {
        FrameTime,
        Variance
    }

    private const int referenceTextureSize = 512;
    private const int kernelSize = 4;
    private const float timeStep = 1f;
    private const LogType logType = LogType.FrameTime;

    private readonly long? overrideStartFrame = null;
    private readonly long? overrideEndFrame = null;

    // textures and buffers
    private RenderTexture rtArr;
    private RenderTexture rtInput;
    private RenderTexture rtVariance;
    private RenderTexture rtReference;
    private RenderTexture rtResult;

    private ComputeBuffer cbufClusterCenters;
    private struct Position {
        public int x;
        public int y;

        /*
			NVidia says structures not aligned to 128 bits are slow
			https://developer.nvidia.com/content/understanding-structured-buffer-performance
		*/
        private readonly int padding_1;
        private readonly int padding_2;
    }
    private ComputeBuffer cbufRandomPositions;

    // shader kernels
    private int kernelAttributeClusters;
    private int kernelGatherVariance;
    private int kernelGenerateVariance;
    private int kernelRandomSwap;
    private int kernelShowResult;
    private int kernelsubsample;
    private int kernelUpdateClusterCenters;
    private int kernelValidateCandidates;

    // inner workings
    private float? timeStart;
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

    private bool toggleKHM = false;
    private bool awaitingRestart = false;
    private bool showReference = false;
    private int[][] offsets;
    private Position[] randomPositions;
    private readonly System.Random random = new System.Random(1);
    private readonly System.Collections.Generic.List<float> frameLogVariance = new System.Collections.Generic.List<float>();
    private Vector4[] clusterCenters;
    private float timeLastIteration = 0;

    private UnityEngine.Video.VideoPlayer videoPlayer;

    // public
    public ComputeShader csHighlightRemoval;
    public UnityEngine.Video.VideoClip[] videos;

    private class LaunchParameters {
        public readonly int textureSize;
        public readonly int numClusters;
        public readonly bool doRandomizeEmptyClusters;
        public readonly bool staggeredJitter;
        public readonly int jitterSize;
        public readonly UnityEngine.Video.VideoClip video;
        public readonly bool doDownscale;
        public readonly int numIterations;
        public readonly Algorithm algorithm;

        private LaunchParameters() { }

        public LaunchParameters(
            int textureSize,
            int numClusters,
            bool doRandomizeEmptyClusters,
            bool staggeredJitter,
            int jitterSize,
            UnityEngine.Video.VideoClip video,
            bool doDownscale,
            int numIterations,
            Algorithm algorithm
        ) {
            this.textureSize = textureSize;
            this.numClusters = numClusters;
            this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
            this.staggeredJitter = staggeredJitter;
            this.jitterSize = jitterSize;
            this.video = video;
            this.doDownscale = doDownscale;
            this.numIterations = numIterations;
            this.algorithm = algorithm;
        }
    }

    private void UpdateRandomPositions() {
        for (int k = 0; k < this.work.Peek().numClusters; k++) {
            this.randomPositions[k].x = this.random.Next(this.work.Peek().textureSize);
            this.randomPositions[k].y = this.random.Next(this.work.Peek().textureSize);
        }
        this.cbufRandomPositions.SetData(this.randomPositions);
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
                this.work.Peek().textureSize & (this.work.Peek().textureSize - 1)
            ) == 0 && this.work.Peek().textureSize > 0
        ); // positive power of 2
        Debug.Assert(this.work.Peek().textureSize <= referenceTextureSize);

        this.csHighlightRemoval.SetInt("mip_level", this.MipLevel(this.work.Peek().textureSize));
        this.csHighlightRemoval.SetInt("ref_mip_level", this.MipLevel(referenceTextureSize));

        this.csHighlightRemoval.SetInt("texture_size", this.work.Peek().textureSize);
    }

    private void InitRTs() {
        var rtDesc = new RenderTextureDescriptor(
            this.work.Peek().textureSize,
            this.work.Peek().textureSize,
            RenderTextureFormat.ARGBFloat,
            0
        ) {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
            volumeDepth = 16,
            useMipMap = true,
            autoGenerateMips = false
        };

        this.rtArr = new RenderTexture(rtDesc) {
            enableRandomWrite = true
        };

        this.rtReference = new RenderTexture(
            referenceTextureSize,
            referenceTextureSize,
            0,
            RenderTextureFormat.ARGBFloat
        );

        this.rtResult = new RenderTexture(
            this.work.Peek().textureSize,
            this.work.Peek().textureSize,
            0,
            RenderTextureFormat.ARGBFloat
        ) {
            enableRandomWrite = true
        };

        this.rtVariance = new RenderTexture(
            referenceTextureSize,
            referenceTextureSize,
            0,
            RenderTextureFormat.RGFloat
        ) {
            enableRandomWrite = true,
            useMipMap = true,
            autoGenerateMips = false
        };

        this.rtInput = new RenderTexture(
            this.work.Peek().textureSize,
            this.work.Peek().textureSize,
            0,
            RenderTextureFormat.ARGBFloat
        ) {
            enableRandomWrite = true
        };
    }

    private void InitCbufs() {
        /*
			second half of the buffer contains candidate cluster centers
			first half contains current cluster centers

			NVidia says structures not aligned to 128 bits are slow
			https://developer.nvidia.com/content/understanding-structured-buffer-performance
		*/
        this.cbufClusterCenters = new ComputeBuffer(this.work.Peek().numClusters * 2, sizeof(float) * 4);
        this.cbufRandomPositions = new ComputeBuffer(this.work.Peek().numClusters, sizeof(int) * 4);

        for (int i = 0; i < this.clusterCenters.Length; i++) {
            // "old" cluster centers with infinite Variance
            // to make sure new ones will overwrite them when validated
            var c = Color.HSVToRGB(
                i / (float)(this.work.Peek().numClusters),
                1,
                1
            );
            c *= 1.0f / (c.r + c.g + c.b);
            this.clusterCenters[i] = new Vector4(c.r, c.g, Mathf.Infinity, 0);
        }
        this.cbufClusterCenters.SetData(this.clusterCenters);
    }

    private void FindKernels() {
        this.kernelShowResult = this.csHighlightRemoval.FindKernel("ShowResult");
        this.kernelAttributeClusters = this.csHighlightRemoval.FindKernel("AttributeClusters");
        this.kernelUpdateClusterCenters = this.csHighlightRemoval.FindKernel("UpdateClusterCenters");
        this.kernelRandomSwap = this.csHighlightRemoval.FindKernel("RandomSwap");
        this.kernelValidateCandidates = this.csHighlightRemoval.FindKernel("ValidateCandidates");
        this.kernelsubsample = this.csHighlightRemoval.FindKernel("SubSample");
        this.kernelGenerateVariance = this.csHighlightRemoval.FindKernel("GenerateVariance");
        this.kernelGatherVariance = this.csHighlightRemoval.FindKernel("GatherVariance");
    }

    private void PopIfExists() {
        string fileName = $"Variance logs/{this.GetFileName()}";
        if (System.IO.File.Exists(fileName)) {
            this.work.Pop();
        }
    }

    private void Awake() {
        Debug.Assert(this.videos.Length != 0);

        /*
            for (int textureSize = 1024; textureSize >= 8; textureSize /= 2) {
                for (
                    int jitterSize = 1;
                    jitterSize <= 16 && jitterSize * textureSize <= 1024;
                    jitterSize *= 2
                ) {
                    foreach (bool staggeredJitter in new bool[] { true, false }) {
                    if (jitterSize == 1 && staggeredJitter) {
                        continue;
                    }
        */


        /*
		{       // 1. subsampling
			foreach (UnityEngine.Video.VideoClip video in this.videos) {
				for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
					foreach (int numClusters in new int[] { 4, 6, 8, 12, 16 }) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numIterations: 3,
								numClusters: numClusters,
								doRandomSwap: false,
								doRandomizeEmptyClusters: false,
								doKHM: false,
								staggeredJitter: false,
								jitterSize: 1,
								video: video,
								doDownscale: false
							)
						);

						string fileName = $"Variance logs/{this.GetFileName()}";

						if (System.IO.File.Exists(fileName)) {
							UnityEditor.EditorApplication.isPlaying = false;
							throw new System.Exception($"File exists: {fileName}");
						}
					}
				}
			}
		}
        */

        /*
		{       // 2. scaling vs subsampling
			foreach (UnityEngine.Video.VideoClip video in this.videos) {
				for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
					foreach (bool doDownscale in new bool[] { true, false }) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numIterations: 3,
								numClusters: 6,
								doRandomSwap: false,
								doRandomizeEmptyClusters: false,
								doKHM: false,
								staggeredJitter: false,
								jitterSize: 1,
								video: video,
								doDownscale: doDownscale
							)
						);

						string fileName = $"Variance logs/{this.GetFileName()}";

						if (System.IO.File.Exists(fileName)) {
							UnityEditor.EditorApplication.isPlaying = false;
							throw new System.Exception($"File exists: {fileName}");
						}
					}
				}
			}
		}
        */

        /*
		{       // 3. staggered jitter
			foreach (UnityEngine.Video.VideoClip video in this.videos) {
				for (int textureSize = 64; textureSize >= 4; textureSize /= 2) {
					for (
							int jitterSize = 1;
							jitterSize <= 16 && jitterSize * textureSize <= 64;
							jitterSize *= 2
						) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numIterations: 3,
								numClusters: 6,
								doRandomSwap: false,
								doRandomizeEmptyClusters: false,
								doKHM: false,
								staggeredJitter: true,
								jitterSize: jitterSize,
								video: video,
								doDownscale: false
							)
						);

						string fileName = $"Variance logs/{this.GetFileName()}";

						if (System.IO.File.Exists(fileName)) {
							UnityEditor.EditorApplication.isPlaying = false;
							throw new System.Exception($"File exists: {fileName}");
						}
					}
				}
			}
		}
        */

        /*
		{       // 4. scanline jitter
			foreach (UnityEngine.Video.VideoClip video in this.videos) {
				for (int textureSize = 64; textureSize >= 4; textureSize /= 2) {
					for (
							int jitterSize = 1;
							jitterSize <= 16 && jitterSize * textureSize <= 64;
							jitterSize *= 2
						) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numIterations: 3,
								numClusters: 6,
								doRandomSwap: false,
								doRandomizeEmptyClusters: false,
								doKHM: false,
								staggeredJitter: false,
								jitterSize: jitterSize,
								video: video,
								doDownscale: false
							)
						);

						string fileName = $"Variance logs/{this.GetFileName()}";

						if (System.IO.File.Exists(fileName)) {
							UnityEditor.EditorApplication.isPlaying = false;
							throw new System.Exception($"File exists: {fileName}");
						}
					}
				}
			}
		}
        */

        /*
		{       // 5. empty cluster randomization
			foreach (UnityEngine.Video.VideoClip video in this.videos) {
				for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
					foreach (bool doRandomizeEmptyClusters in new bool[] { true, false }) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numIterations: 3,
								numClusters: 6,
								doRandomSwap: false,
								doRandomizeEmptyClusters: doRandomizeEmptyClusters,
								doKHM: false,
								staggeredJitter: false,
								jitterSize: 1,
								video: video,
								doDownscale: false
							)
						);

						string fileName = $"Variance logs/{this.GetFileName()}";

						if (System.IO.File.Exists(fileName)) {
							UnityEditor.EditorApplication.isPlaying = false;
							throw new System.Exception($"File exists: {fileName}");
						}
					}
				}
			}
		}
        */

        /*
        {       // 6. KHM and random swap

            for (int numIterations = 1; numIterations < 31; numIterations++) {

                foreach (UnityEngine.Video.VideoClip video in this.videos) {

                    // normal  K-Means
                    this.work.Push(
                        new LaunchParameters(
                            textureSize: 64,
                            numIterations: numIterations,
                            numClusters: 6,
                            doRandomizeEmptyClusters: false,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            algorithm: Algorithm.KM
                        )
                    );

                    if (
                        this.ValidateRandomSwapParams(1, numIterations)
                    ) {
                        // random swap
                        this.work.Push(
                            new LaunchParameters(
                                textureSize: 64,
                                numIterations: numIterations,
                                numClusters: 6,
                                doRandomizeEmptyClusters: false,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                algorithm: Algorithm.RS_1KM
                            )
                        );
                    }

                    if (
                        this.ValidateRandomSwapParams(2, numIterations)
                    ) {
                        // random swap
                        this.work.Push(
                            new LaunchParameters(
                                textureSize: 64,
                                numIterations: numIterations,
                                numClusters: 6,
                                doRandomizeEmptyClusters: false,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                algorithm: Algorithm.RS_2KM
                            )
                        );
                    }

                    // KHM
                    this.work.Push(
                        new LaunchParameters(
                            textureSize: 64,
                            numIterations: numIterations,
                            numClusters: 6,
                            doRandomizeEmptyClusters: false,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            algorithm: Algorithm.KHM
                        )

                    );

                    string fileName = $"Variance logs/{this.GetFileName()}";

                    if (System.IO.File.Exists(fileName)) {
                        UnityEditor.EditorApplication.isPlaying = false;
                        throw new System.Exception($"File exists: {fileName}");
                    }
                }
            }
        }
        */

        {       // frame time measurement for RS (dispatch vs readback) 
            for (int i = 0; i < 10; i++) {
                foreach (UnityEngine.Video.VideoClip video in this.videos) {
                    foreach (int textureSize in new int[] { 512, 64 }) {
                        foreach (
                            Algorithm algorithm in new Algorithm[] {
                            Algorithm.KM, Algorithm.KHM, Algorithm.RS_2KM, Algorithm.RS_2KM_readback
                            }
                        ) {
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numIterations: 3,
                                    numClusters: 6,
                                    doRandomizeEmptyClusters: false,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    algorithm: algorithm
                                )
                            );
                        }
                        foreach (
                            Algorithm algorithm in new Algorithm[] {
                            Algorithm.KM, Algorithm.KHM,
                            }
                        ) {
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numIterations: 1,
                                    numClusters: 6,
                                    doRandomizeEmptyClusters: false,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    algorithm: Algorithm.KM
                                )
                            );
                        }
                    }
                }
            }
        }


    }

    private void InitJitterOffsets() {
        this.offsets = JitterPattern.Get(this.work.Peek().jitterSize);
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

        Debug.Log($"work left: {this.work.Count}");
        Debug.Log($"processing: {this.GetFileName()}");

        this.frameLogVariance.Clear();
        this.framesProcessed = 0;
        this.timeStart = null;
        this.toggleKHM = false; // important to reset!

        this.randomPositions = new Position[this.work.Peek().numClusters];
        this.clusterCenters = new Vector4[this.work.Peek().numClusters * 2];

        this.InitJitterOffsets();
        this.FindKernels();
        this.SetTextureSize();
        this.InitRTs();
        this.InitCbufs();

        this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
        this.videoPlayer.playbackSpeed = 0;
        this.videoPlayer.clip = this.work.Peek().video;
        this.videoPlayer.Play();
        this.videoPlayer.frame = this.GetStartFrame();

        this.csHighlightRemoval.SetBool("do_random_sample_empty_clusters", this.work.Peek().doRandomizeEmptyClusters);
        this.csHighlightRemoval.SetInt("num_clusters", this.work.Peek().numClusters);
    }

    private void AttributeClusters(Texture inputTex = null, bool final = false) {
        inputTex ??= this.rtInput;

        this.csHighlightRemoval.SetBool("final", final);  // replace with define
        this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_input", inputTex);
        this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_variance", this.rtVariance);
        this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_arr_clusters_rw", this.rtArr);
        this.csHighlightRemoval.SetBuffer(this.kernelAttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.Dispatch(
            this.kernelAttributeClusters,
            inputTex.width / kernelSize,
            inputTex.height / kernelSize,
            1
        );
    }

    private void UpdateClusterCenters(bool rejectOld) {
        this.UpdateRandomPositions();

        this.csHighlightRemoval.SetBool("reject_old", rejectOld);
        this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_arr_clusters_r", this.rtArr);
        this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_input", this.rtInput);
        this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_variance_maskernelr", this.rtVariance);
        this.csHighlightRemoval.SetBuffer(this.kernelUpdateClusterCenters, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.SetBuffer(this.kernelUpdateClusterCenters, "cbuf_random_positions", this.cbufRandomPositions);
        this.csHighlightRemoval.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
    }

    private void KMeans(Texture texInput = null, bool rejectOld = false) {
        this.AttributeClusters(texInput);
        this.rtArr.GenerateMips();
        this.rtVariance.GenerateMips();
        this.UpdateClusterCenters(rejectOld);

        //this.LogVariance();
    }

    private void RandomSwap() {
        this.UpdateRandomPositions();

        this.csHighlightRemoval.SetBuffer(this.kernelRandomSwap, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.SetBuffer(this.kernelRandomSwap, "cbuf_random_positions", this.cbufRandomPositions);
        this.csHighlightRemoval.SetTexture(this.kernelRandomSwap, "tex_input", this.rtInput);
        this.csHighlightRemoval.SetInt("randomClusterCenter", this.random.Next(this.work.Peek().numClusters));
        this.csHighlightRemoval.Dispatch(this.kernelRandomSwap, 1, 1, 1);
    }

    private string GetFileName() {
        string videoName = this.work.Peek().video.name;
        int numIterations = this.work.Peek().numIterations;
        int textureSize = this.work.Peek().textureSize;
        int numClusters = this.work.Peek().numClusters;
        bool doRandomizeEmptyClusters = this.work.Peek().doRandomizeEmptyClusters;
        int jitterSize = this.work.Peek().jitterSize;
        bool staggeredJitter = this.work.Peek().staggeredJitter;
        bool doDownscale = this.work.Peek().doDownscale;
        string algorithm;
        switch (this.work.Peek().algorithm) {
            case Algorithm.KM:
                algorithm = "KM";
                break;
            case Algorithm.KHM:
                algorithm = "KHM(3)";
                break;
            case Algorithm.RS_1KM:
                algorithm = "RS(1KM)";
                break;
            case Algorithm.RS_2KM:
                algorithm = "RS(2KM)";
                break;
            case Algorithm.RS_2KM_readback:
                algorithm = "RS(2KM)_readback";
                break;
            case Algorithm.Alternating:
                algorithm = "KM+KHM(3)";
                break;
            case Algorithm.OneKM:
                algorithm = "1xKM+KHM(3)";
                break;
            default:
                throw new System.NotImplementedException();
        }

        return $"video file:{videoName}|number of iterations:{numIterations}|texture size:{textureSize}|number of clusters:{numClusters}|randomize empty clusters:{doRandomizeEmptyClusters}|jitter size:{jitterSize}|staggered jitter:{staggeredJitter}|downscale:{doDownscale}|algorithm:{algorithm}.csv";
    }

    private float GetVariance() {
        this.csHighlightRemoval.SetTexture(this.kernelGenerateVariance, "tex_input", this.rtReference);
        this.csHighlightRemoval.SetTexture(this.kernelGenerateVariance, "tex_variance_rw", this.rtVariance);
        this.csHighlightRemoval.SetBuffer(this.kernelGenerateVariance, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.Dispatch(
            this.kernelGenerateVariance,
            referenceTextureSize / kernelSize,
            referenceTextureSize / kernelSize,
        1);

        this.csHighlightRemoval.SetTexture(this.kernelGatherVariance, "tex_variance_r", this.rtVariance);
        this.csHighlightRemoval.SetBuffer(this.kernelGatherVariance, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.Dispatch(this.kernelGatherVariance, 1, 1, 1);

        this.cbufClusterCenters.GetData(this.clusterCenters);

        float variance = this.clusterCenters[0].w;
        //return Variance;
        return float.IsNaN(variance) == false ? variance : this.videoPlayer.frame < this.GetStartFrame() + 5 ? 0 : throw new System.Exception($"no Variance! (frame {this.videoPlayer.frame})");
    }

    private void ValidateCandidates() {
        switch (this.work.Peek().algorithm) {
            case Algorithm.RS_1KM:
            case Algorithm.RS_2KM:
                this.csHighlightRemoval.SetBuffer(this.kernelValidateCandidates, "cbuf_cluster_centers", this.cbufClusterCenters);
                this.csHighlightRemoval.Dispatch(this.kernelValidateCandidates, 1, 1, 1);
                break;
            case Algorithm.RS_2KM_readback:
                this.cbufClusterCenters.GetData(this.clusterCenters);
                int numClusters = this.work.Peek().numClusters;
                for (int i = 0; i < numClusters; i++) {
                    if (this.clusterCenters[i].z < this.clusterCenters[i + numClusters].z) {
                        this.clusterCenters[i + numClusters] = this.clusterCenters[i];
                    } else {
                        this.clusterCenters[i] = this.clusterCenters[i + numClusters];
                    }
                }
                this.cbufClusterCenters.SetData(this.clusterCenters);
                break;
            default:
                throw new System.NotImplementedException();
        }
    }

    // Update is called once per frame
    private void Update() {

    }

    private bool ValidateRandomSwapParams(int iterationsKM, int iterations) {
        if (iterations <= 1) {
            return false;
        }
        if (iterationsKM == 1) {
            return true;
        }
        return iterations % iterationsKM == 1;
    }

    private void RandomSwapClustering(int iterationsKM) {
        Debug.Assert(
            this.ValidateRandomSwapParams(iterationsKM, this.work.Peek().numIterations)
        );

        this.csHighlightRemoval.SetBool("KHM", false);
        this.KMeans(this.rtInput, true);

        for (int i = 1; i < this.work.Peek().numIterations; i += iterationsKM) {
            this.RandomSwap();
            for (int k = 0; k < iterationsKM; k++) {
                this.KMeans();
            }
            this.ValidateCandidates();
        }
    }

    private void RunClustering() {
        switch (this.work.Peek().algorithm) {
            case Algorithm.KM:
                this.csHighlightRemoval.SetBool("KHM", false);

                for (int i = 0; i < this.work.Peek().numIterations; i++) {
                    this.KMeans();
                }

                break;
            case Algorithm.KHM:
                this.csHighlightRemoval.SetBool("KHM", true);

                for (int i = 0; i < this.work.Peek().numIterations; i++) {
                    this.KMeans();
                }

                break;
            case Algorithm.RS_1KM:
                this.RandomSwapClustering(1);
                break;
            case Algorithm.RS_2KM:
            case Algorithm.RS_2KM_readback:
                this.RandomSwapClustering(2);
                break;
            case Algorithm.Alternating:
                for (int i = 0; i < this.work.Peek().numIterations; i++) {
                    this.csHighlightRemoval.SetBool("KHM", this.toggleKHM);
                    this.toggleKHM = !this.toggleKHM;
                    this.KMeans();
                }

                break;
            case Algorithm.OneKM:
                for (int i = 0; i < this.work.Peek().numIterations; i++) {
                    this.csHighlightRemoval.SetBool("KHM", i != 0);
                    this.KMeans();
                }

                break;
            default:
                throw new System.NotImplementedException();
        }

        this.AttributeClusters(this.rtInput, true);
    }

    private void WriteVarianceLog() {
        string fileName = $"Variance logs/{this.GetFileName()}";

        if (System.IO.File.Exists(fileName)) {
            UnityEditor.EditorApplication.isPlaying = false;
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

    private void WriteFrameTimeLog(float frameTime) {
        string fileName = "Frame time log.txt";

        using System.IO.FileStream fs = System.IO.File.Open(
                fileName, System.IO.FileMode.Append
            );
        using var sw = new System.IO.StreamWriter(fs);
        sw.WriteLine(this.GetFileName());
        sw.WriteLine($"Frame time: {frameTime:0.000} ms");
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

        this.timeStart ??= Time.time;
        this.framesProcessed++;

        if (this.videoPlayer.frame == this.GetEndFrame()) {
            this.awaitingRestart = true;
            Graphics.Blit(src, dest);

            switch (logType) {
                case LogType.Variance:
                    this.WriteVarianceLog();
                    break;
                case LogType.FrameTime:
                    this.WriteFrameTimeLog(
                        (Time.time - (float)this.timeStart) / this.framesProcessed * 1000
                    );
                    break;
            }

            this.work.Pop();

            if (this.work.Count == 0) {
                UnityEditor.EditorApplication.isPlaying = false;
                Destroy(this);
            }

            this.OnDisable();
            this.OnEnable();

            return;
        }

        Graphics.Blit(this.videoPlayer.texture, this.rtReference);

        this.csHighlightRemoval.SetInt("sub_sample_multiplier", referenceTextureSize / this.work.Peek().textureSize);
        this.csHighlightRemoval.SetInts(
            "sub_sample_offset",
            this.work.Peek().staggeredJitter ?
                this.offsets[
                    Time.frameCount % this.offsets.Length
                ] :
                new int[] {
                    Time.frameCount % this.work.Peek().jitterSize,
                    (Time.frameCount / this.offsets.Length) % this.work.Peek().jitterSize
                }
        );
        this.csHighlightRemoval.SetTexture(this.kernelsubsample, "tex_input", this.rtReference);
        this.csHighlightRemoval.SetTexture(this.kernelsubsample, "tex_output", this.rtInput);
        this.csHighlightRemoval.SetBool("downscale", this.work.Peek().doDownscale);
        this.csHighlightRemoval.Dispatch(
            this.kernelsubsample,
            this.work.Peek().textureSize / kernelSize,
            this.work.Peek().textureSize / kernelSize,
            1
        );

        this.videoPlayer.StepForward();

        this.RunClustering();

        if (Time.time - this.timeLastIteration > timeStep) {
            this.timeLastIteration = Time.time;
            this.showReference = !this.showReference;
            this.showReference = false;
            //this.LogVariance();
        }

        if (logType == LogType.Variance) {
            this.frameLogVariance.Add(this.GetVariance());
        }

        this.RenderResult();
        Graphics.Blit(this.rtResult, dest);
        this.rtResult.DiscardContents();
    }

    private void OnDisable() {
        this.rtArr?.Release();
        this.rtResult?.Release();
        this.rtInput?.Release();
        this.rtVariance?.Release();
        this.rtReference?.Release();

        this.cbufClusterCenters?.Release();
        this.cbufRandomPositions?.Release();
    }

    private void RenderResult() {
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_arr_clusters_r", this.rtArr);
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_output", this.rtResult);
        this.csHighlightRemoval.SetBuffer(this.kernelShowResult, "cbuf_cluster_centers", this.cbufClusterCenters);
        this.csHighlightRemoval.SetBool("show_reference", this.showReference);
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input", this.rtInput);
        this.csHighlightRemoval.Dispatch(
            this.kernelShowResult,
            this.work.Peek().textureSize / kernelSize,
            this.work.Peek().textureSize / kernelSize,
            1
        );
    }
}
