using UnityEngine;

public class ClusteringTest : MonoBehaviour {
    // configuration
    private enum LogType {
        FrameTime,
        Variance
    }

    private const int referenceTextureSize = 512;
    private const int kernelSize = 16;
    private const float timeStep = 1f;
    private const LogType logType = LogType.Variance;

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

        /*
			NVidia says structures not aligned to 128 bits are slow
			https://developer.nvidia.com/content/understanding-structured-buffer-performance
		*/
        private readonly int padding_1;
        private readonly int padding_2;
    }
    private ComputeBuffer cbufRandomPositions;

    // shader kernels
    //private int kernelAttributeClusters;
    private int kernelShowResult;
    private int kernelSubsample;

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

    private bool awaitingRestart = false;
    private bool showReference = false;
    private int[][] offsets;
    private readonly int[] scanlinePixelOffset = new int[2];
    private readonly System.Collections.Generic.List<float> frameLogVariance = new System.Collections.Generic.List<float>();
    private float timeLastIteration = 0;
    private LaunchParameters currentWorkParameters;

    private UnityEngine.Video.VideoPlayer videoPlayer;

    // public
    public ComputeShader csHighlightRemoval;
    public UnityEngine.Video.VideoClip[] videos;

    private class LaunchParameters {
        public readonly int textureSize;
        public readonly bool staggeredJitter;
        public readonly int jitterSize;
        public readonly UnityEngine.Video.VideoClip video;
        public readonly bool doDownscale;
        public readonly AClusteringAlgorithmDispatcher clusteringAlgorithmDispatcher;

        private LaunchParameters() { }

        public LaunchParameters(
            int textureSize,
            int numClusters,
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

    private float PortionUsed(int textureSize) {
        return textureSize / (float)(referenceTextureSize);
    }

    private void SetTextureSize() {
        Debug.Assert(
            (
                this.currentWorkParameters.textureSize & (this.currentWorkParameters.textureSize - 1)
            ) == 0 && this.currentWorkParameters.textureSize > 0
        ); // positive power of 2
        Debug.Assert(this.currentWorkParameters.textureSize <= referenceTextureSize);

        this.csHighlightRemoval.SetInt("mip_level", this.MipLevel(this.currentWorkParameters.textureSize));
        this.csHighlightRemoval.SetFloat("portion_used", this.PortionUsed(this.currentWorkParameters.textureSize));
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

    private void PopIfExists() {
        string fileName = $"Variance logs/{this.GetFileName(this.work.Peek())}";
        if (System.IO.File.Exists(fileName)) {
            this.work.Pop();
        }
    }

    private void ThrowIfExists() {
        string fileName = $"Variance logs/{this.GetFileName(this.work.Peek())}";

        if (System.IO.File.Exists(fileName)) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            throw new System.Exception($"File exists: {fileName}");
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

						string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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

						string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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

						string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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

						string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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

						string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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
            foreach (UnityEngine.Video.VideoClip video in this.videos) {
                for (int numIterations = 1; numIterations < 31; numIterations += 1) {
                    // KM
                    this.work.Push(
                        new LaunchParameters(
                            textureSize: 64,
                            numClusters: 6,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKM(
                                kernelSize: kernelSize,
                                computeShader: this.csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6
                            )
                        )
                    );
                    this.ThrowIfExists();

                    // KHM
                    this.work.Push(
                        new LaunchParameters(
                            textureSize: 64,
                            numClusters: 6,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKHM(
                                kernelSize: kernelSize,
                                computeShader: this.csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6
                            )
                        )
                    );
                    this.ThrowIfExists();

                    // RS(1KM)
                    if (
                        ClusteringAlgorithmDispatcherRS.IsNumIterationsValid(
                            iterations: numIterations,
                            iterationsKM: 1
                        )
                    ) {
                        this.work.Push(
                            new LaunchParameters(
                                textureSize: 64,
                                numClusters: 6,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherRS(
                                    kernelSize: kernelSize,
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6,
                                    numIterationsKM: 1,
                                    doReadback: false
                                )
                            )
                        );
                        this.ThrowIfExists();
                    }

                    //RS(2KM)
                    if (
                        ClusteringAlgorithmDispatcherRS.IsNumIterationsValid(
                            iterations: numIterations,
                            iterationsKM: 2
                        )
                    ) {
                        this.work.Push(
                            new LaunchParameters(
                                textureSize: 64,
                                numClusters: 6,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherRS(
                                    kernelSize: kernelSize,
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6,
                                    numIterationsKM: 2,
                                    doReadback: false
                                )
                            )
                        );
                        this.ThrowIfExists();
                    }
                }
                // Knecht
                this.work.Push(
                    new LaunchParameters(
                        textureSize: 64,
                        numClusters: 6,
                        staggeredJitter: false,
                        jitterSize: 1,
                        video: video,
                        doDownscale: false,
                        clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKnecht(
                            kernelSize: kernelSize,
                            computeShader: this.csHighlightRemoval,
                            doRandomizeEmptyClusters: false,
                            numClusters: 6
                        )
                    )
                );
                this.ThrowIfExists();
            }
        }
        */

        /*
        {       // frame time measurements
            for (int i = 0; i < 3; i++) {
                foreach (UnityEngine.Video.VideoClip video in this.videos) {
                    foreach (int textureSize in new int[] { 512, 64 }) {
                        // 3 iterations
                        {
                            const int numIterations = 3;

                            // KM
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numClusters: 6,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKM(
                                        kernelSize: kernelSize,
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
                                        numClusters: 6
                                    )
                                )
                            );
                            this.ThrowIfExists();

                            // KHM
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numClusters: 6,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKHM(
                                        kernelSize: kernelSize,
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
                                        numClusters: 6
                                    )
                                )
                            );
                            this.ThrowIfExists();

                            foreach (bool doReadback in new bool[] { true, false }) {
                                // RS(2KM)
                                this.work.Push(
                                     new LaunchParameters(
                                         textureSize: textureSize,
                                         numClusters: 6,
                                         staggeredJitter: false,
                                         jitterSize: 1,
                                         video: video,
                                         doDownscale: false,
                                         clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherRS(
                                             kernelSize: kernelSize,
                                             computeShader: this.csHighlightRemoval,
                                             numIterations: numIterations,
                                             doRandomizeEmptyClusters: false,
                                             numClusters: 6,
                                             numIterationsKM: 2,
                                             doReadback: doReadback
                                         )
                                     )
                                 );
                                this.ThrowIfExists();
                            }
                        }

                        // 1 iteration
                        {
                            const int numIterations = 1;

                            // KM
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numClusters: 6,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKM(
                                        kernelSize: kernelSize,
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
                                        numClusters: 6
                                    )
                                )
                            );
                            this.ThrowIfExists();

                            // KHM
                            this.work.Push(
                                new LaunchParameters(
                                    textureSize: textureSize,
                                    numClusters: 6,
                                    staggeredJitter: false,
                                    jitterSize: 1,
                                    video: video,
                                    doDownscale: false,
                                    clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKHM(
                                        kernelSize: kernelSize,
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
                                        numClusters: 6
                                    )
                                )
                            );
                            this.ThrowIfExists();
                        }
                        // Knecht
                        this.work.Push(
                            new LaunchParameters(
                                textureSize: textureSize,
                                numClusters: 6,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKnecht(
                                    kernelSize: kernelSize,
                                    computeShader: this.csHighlightRemoval,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6
                                )
                            )
                        );
                        this.ThrowIfExists();
                    }
                }
            }
        }
        */

        this.work.Push(
            new LaunchParameters(
                textureSize: 64,
                numClusters: 6,
                staggeredJitter: false,
                jitterSize: 1,
                video: this.videos[1],
                doDownscale: false,
                clusteringAlgorithmDispatcher: new ClusteringAlgorithmDispatcherKnecht(
                    kernelSize: kernelSize,
                    computeShader: this.csHighlightRemoval,
                    doRandomizeEmptyClusters: false,
                    numClusters: 6
                )
            )
        );
        this.ThrowIfExists();
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
        Debug.Log($"processing: {this.GetFileName(this.currentWorkParameters)}");

        this.frameLogVariance.Clear();
        this.framesProcessed = 0;
        this.timeStart = null;

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

    private string GetFileName(LaunchParameters launchParams) {
        string videoName = launchParams.video.name;
        int numIterations = launchParams.clusteringAlgorithmDispatcher.numIterations;
        int textureSize = launchParams.textureSize;
        int numClusters = launchParams.clusteringAlgorithmDispatcher.numClusters;
        int jitterSize = launchParams.jitterSize;
        bool staggeredJitter = launchParams.staggeredJitter;
        bool doDownscale = launchParams.doDownscale;
        string algorithm = launchParams.clusteringAlgorithmDispatcher.descriptionString;
        bool doRandomizeEmptyClusters = launchParams.clusteringAlgorithmDispatcher.doRandomizeEmptyClusters;

        return $"video file:{videoName}|number of iterations:{numIterations}|texture size:{textureSize}|number of clusters:{numClusters}|randomize empty clusters:{doRandomizeEmptyClusters}|jitter size:{jitterSize}|staggered jitter:{staggeredJitter}|downscale:{doDownscale}|algorithm:{algorithm}.csv";
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

    private void WriteVarianceLog() {
        string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

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

    private void WriteFrameTimeLog(float frameTime) {
        string fileName = "Frame time log.txt";

        using System.IO.FileStream fs = System.IO.File.Open(
                fileName, System.IO.FileMode.Append
            );
        using var sw = new System.IO.StreamWriter(fs);
        sw.WriteLine(this.GetFileName(this.currentWorkParameters));
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

        this.videoPlayer.StepForward();

        this.currentWorkParameters.clusteringAlgorithmDispatcher.RunClustering(
            this.rtInput,
            this.currentWorkParameters.textureSize,
            this.clusteringRTsAndBuffers
        );

        this.currentWorkParameters.clusteringAlgorithmDispatcher.AttributeClusters(
            this.rtInput,
            this.clusteringRTsAndBuffers,
            final: true,
            khm: false
        );

        if (Time.time - this.timeLastIteration > timeStep) {
            this.timeLastIteration = Time.time;
            this.showReference = !this.showReference;
            this.showReference = false;
        }

        if (logType == LogType.Variance) {
            this.frameLogVariance.Add(
                this.currentWorkParameters.clusteringAlgorithmDispatcher.GetVariance(
                    this.clusteringRTsAndBuffers
                )
            );
        }

        this.RenderResult();
        Graphics.Blit(this.rtResult, dest);
        this.rtResult.DiscardContents();
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
        this.csHighlightRemoval.SetBool("show_reference", this.showReference);
        this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input", this.rtInput);
        this.csHighlightRemoval.Dispatch(
            this.kernelShowResult,
            this.currentWorkParameters.textureSize / kernelSize,
            this.currentWorkParameters.textureSize / kernelSize,
            1
        );
    }
}
