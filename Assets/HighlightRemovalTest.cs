using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int referenceTextureSize = 1024;

	private const float timeStep = 1f;

	private float timeLastIteration = 0;
	private bool showReference = false;
	private int[][] offsets;

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

	private RenderTexture rtArr;
	private RenderTexture rtResult;
	private RenderTexture rtMSE;
	private RenderTexture rtReference;
	private RenderTexture rtInput;

	private ComputeBuffer cbufClusterCenters;
	private ComputeBuffer cbufRandomPositions;

	private int kernelShowResult;
	private int kernelAttributeClusters;
	private int kernelUpdateClusterCenters;
	private int kernelRandomSwap;
	private int kernelValidateCandidates;
	private int kernelsubsample;
	private int kernelGenerateMSE;
	private int kernelGatherMSE;

	private readonly System.Collections.Generic.Stack<LaunchParameters> work =
		new System.Collections.Generic.Stack<LaunchParameters>();

	private readonly System.Random random = new System.Random(1);
	private Position[] randomPositions;
	private Vector4[] clusterCenters;

	private UnityEngine.Video.VideoPlayer videoPlayer;
	private readonly System.Collections.Generic.List<float> frameLogMSE = new System.Collections.Generic.List<float>();

	public ComputeShader csHighlightRemoval;
	//public Texture2D texInput;
	public UnityEngine.Video.VideoClip[] videos;

	private class LaunchParameters {
		public readonly int textureSize;
		public readonly int numClusters;
		public readonly bool doRandomSwap;
		public readonly bool doRandomizeEmptyClusters;
		public readonly bool doKHM;
		public readonly bool staggeredJitter;
		public readonly int jitterSize;
		public readonly UnityEngine.Video.VideoClip video;

		private LaunchParameters() { }

		public LaunchParameters(
			int textureSize,
			int numClusters,
			bool doRandomSwap,
			bool doRandomizeEmptyClusters,
			bool doKHM,
			bool staggeredJitter,
			int jitterSize,
			UnityEngine.Video.VideoClip video
		) {
			this.textureSize = textureSize;
			this.numClusters = numClusters;
			this.doRandomSwap = doRandomSwap;
			this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
			this.doKHM = doKHM;
			this.staggeredJitter = staggeredJitter;
			this.jitterSize = jitterSize;
			this.video = video;
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

		this.rtMSE = new RenderTexture(
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
			// "old" cluster centers with infinite MSE
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
		this.kernelGenerateMSE = this.csHighlightRemoval.FindKernel("GenerateMSE");
		this.kernelGatherMSE = this.csHighlightRemoval.FindKernel("GatherMSE");
	}

	private void Awake() {
		Debug.Assert(this.videos.Length != 0);

		foreach (var video in this.videos) {
			// texture size 1024 to 16
			for (int textureSize = 1024; textureSize >= 64; textureSize /= 2) {
				// jitter 1 to 16
				for (
					int jitterSize = 1;
					jitterSize <= 16 && jitterSize * textureSize <= 1024;
					jitterSize *= 2
				) {
					// staggered vs sequential jitter
					foreach (bool staggeredJitter in new bool[] { true, false }) {
						this.work.Push(
							new LaunchParameters(
								textureSize: textureSize,
								numClusters: 6,
								doRandomSwap: false,
								doRandomizeEmptyClusters: false,
								doKHM: false,
								staggeredJitter: staggeredJitter,
								jitterSize: jitterSize,
								video: video
							)
						);
					}
				}
			}
		}
	}

	private void InitJitterOffsets() {
		this.offsets = JitterPattern.Get(this.work.Peek().jitterSize);
	}

	private void OnEnable() {
		Debug.Log("enable");
		if (this.enabled == false) {
			return;
		}

		Debug.Log($"processing: {this.GetFileName()}");

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
		this.videoPlayer.frame = (long)this.videoPlayer.frameCount - 200;

		this.csHighlightRemoval.SetBool("do_random_sample_empty_clusters", this.work.Peek().doRandomizeEmptyClusters);
		this.csHighlightRemoval.SetBool("KHM", this.work.Peek().doKHM);
	}

	private void AttributeClusters(Texture inputTex = null, bool final = false) {
		inputTex ??= this.rtInput;

		this.csHighlightRemoval.SetBool("final", final);  // replace with define
		this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_input", inputTex);
		this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_mse", this.rtMSE);
		this.csHighlightRemoval.SetTexture(this.kernelAttributeClusters, "tex_arr_clusters_rw", this.rtArr);
		this.csHighlightRemoval.SetBuffer(this.kernelAttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(this.kernelAttributeClusters, inputTex.width / 16, inputTex.height / 16, 1);
	}

	private void UpdateClusterCenters(bool rejectOld) {
		this.UpdateRandomPositions();

		this.csHighlightRemoval.SetBool("reject_old", rejectOld);
		this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetTexture(this.kernelUpdateClusterCenters, "tex_mse_maskernelr", this.rtMSE);
		this.csHighlightRemoval.SetBuffer(this.kernelUpdateClusterCenters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBuffer(this.kernelUpdateClusterCenters, "cbuf_random_positions", this.cbufRandomPositions);
		this.csHighlightRemoval.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
	}

	private void KMeans(Texture texInput = null, bool rejectOld = false) {
		this.AttributeClusters(texInput);
		this.rtArr.GenerateMips();
		this.rtMSE.GenerateMips();
		this.UpdateClusterCenters(rejectOld);

		//this.LogMSE();
	}

	private void RandomSwap() {
		this.UpdateRandomPositions();

		this.csHighlightRemoval.SetBuffer(this.kernelRandomSwap, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBuffer(this.kernelRandomSwap, "cbuf_random_positions", this.cbufClusterCenters);
		this.csHighlightRemoval.SetTexture(this.kernelRandomSwap, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetInt("randomClusterCenter", this.random.Next(this.work.Peek().numClusters));
		this.csHighlightRemoval.Dispatch(this.kernelRandomSwap, 1, 1, 1);
	}

	private string GetFileName() {
		string videoName = this.work.Peek().video.name;
		int numIterations = 3;
		int textureSize = this.work.Peek().textureSize;
		int numClusters = this.work.Peek().numClusters;
		bool doRandomSwap = this.work.Peek().doRandomSwap;
		bool doRandomizeEmptyClusters = this.work.Peek().doRandomizeEmptyClusters;
		bool doKHM = this.work.Peek().doKHM;
		int jitterSize = this.work.Peek().jitterSize;
		bool staggeredJitter = this.work.Peek().staggeredJitter;

		return $"{videoName}|{numIterations}|{textureSize}|{numClusters}|{doRandomSwap}|{doRandomizeEmptyClusters}|{doKHM}|{jitterSize}|{staggeredJitter}.csv";
	}

	private float GetMSE() {
		this.csHighlightRemoval.SetTexture(this.kernelGenerateMSE, "tex_input", this.rtReference);
		this.csHighlightRemoval.SetTexture(this.kernelGenerateMSE, "tex_mse_rw", this.rtMSE);
		this.csHighlightRemoval.SetBuffer(this.kernelGenerateMSE, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(
			this.kernelGenerateMSE,
			referenceTextureSize / 16,
			referenceTextureSize / 16,
		1);

		this.csHighlightRemoval.SetTexture(this.kernelGatherMSE, "tex_mse_r", this.rtMSE);
		this.csHighlightRemoval.SetBuffer(this.kernelGatherMSE, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(this.kernelGatherMSE, 1, 1, 1);

		this.cbufClusterCenters.GetData(this.clusterCenters);

		float MSE = this.clusterCenters[0].w;
		return MSE;
		//return float.IsNaN(MSE) == false ? MSE : this.videoPlayer.frame < 5 ? 0 : throw new System.Exception($"no MSE! (frame {this.videoPlayer.frame})");
	}

	private void LogMSE() {
		long progress = this.videoPlayer.frame * 100 / (long)this.videoPlayer.frameCount;
		float MSE = this.GetMSE();
		Debug.Log($"         {progress:00}%         MSE: {MSE:0.000000}");
	}

	private void ValidateCandidates() {
		this.csHighlightRemoval.SetBuffer(this.kernelValidateCandidates, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(this.kernelValidateCandidates, 1, 1, 1);
	}

	// Update is called once per frame
	private void Update() {

	}

	private void ClusteringIteration() {
		if (this.work.Peek().doRandomSwap) {
			this.KMeans(this.rtInput, true);  // discard old saved clusters, update MSE

			this.RandomSwap();
			this.KMeans();
			this.KMeans();
			this.ValidateCandidates();
		} else {
			this.KMeans();
			this.KMeans();
			this.KMeans();
			// no need to discard old saved clusters
			// we never validate / restore
		}
		this.AttributeClusters(this.rtInput, true);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
		if (
			this.videoPlayer.frame == -1 ||
			this.videoPlayer.frame == (long)this.videoPlayer.frameCount - 1
		) {
			Graphics.Blit(src, dest);
			return;
		}

		Debug.Log(this.videoPlayer.frame);

		if (this.videoPlayer.frame == (long)this.videoPlayer.frameCount - 2) {
			this.videoPlayer.StepForward();
			Graphics.Blit(src, dest);

			/*
				video file
				num. iterations
				texture size
				num clusters
				random swap (y/n)
				randomize empty clusters (y/n)
				KHM (y/n)
				jitter Size
				staggered jitter
			*/
			string fileName = $"MSE logs/{this.GetFileName()}";

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
				sw.WriteLine("Frame,MSE");
				for (int i = 0; i < this.frameLogMSE.Count; i++) {
					float MSE = this.frameLogMSE[i];
					if (MSE == -1) {
						sw.WriteLine(
							$"{i}"
						);
					} else {
						sw.WriteLine(
							$"{i},{MSE}"
						);
					}
				}
				Debug.Log($"file written: {fileName}");
			}

			this.work.Pop();
			Debug.Log($"work left: {this.work.Count}");

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
		this.csHighlightRemoval.Dispatch(
			this.kernelsubsample,
			this.work.Peek().textureSize / 16,
			this.work.Peek().textureSize / 16,
			1
		);

		this.videoPlayer.StepForward();

		this.ClusteringIteration();

		if (Time.time - this.timeLastIteration > timeStep) {
			this.timeLastIteration = Time.time;
			this.showReference = !this.showReference;
			this.showReference = false;
			//this.LogMSE();
		}

		this.frameLogMSE.Add(this.GetMSE());

		this.RenderResult();
		Graphics.Blit(this.rtResult, dest);
		this.rtResult.DiscardContents();
	}

	private void OnDisable() {
		this?.rtArr.Release();
		this?.rtResult.Release();
		this?.rtInput.Release();
		this?.rtMSE.Release();
		this?.rtReference.Release();

		this?.cbufClusterCenters.Release();
		this?.cbufRandomPositions.Release();
	}

	private void RenderResult() {
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_output", this.rtResult);
		this.csHighlightRemoval.SetBuffer(this.kernelShowResult, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBool("show_reference", this.showReference);
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input", this.rtInput);
		this.csHighlightRemoval.Dispatch(
			this.kernelShowResult,
			this.work.Peek().textureSize / 16,
			this.work.Peek().textureSize / 16,
			1
		);
	}
}
