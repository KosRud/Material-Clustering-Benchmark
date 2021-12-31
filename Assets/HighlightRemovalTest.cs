using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int textureSize = 16;
	private const int referenceTextureSize = 1024;

	private const float timeStep = 1f;

	private float timeLastIteration = 0;
	private long? previousFrame = null;
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
	public Texture2D texInput;

	private class LaunchParameters {
		public readonly int textureSize;
		public readonly int numClusters;
		public readonly bool doRandomSwap;
		public readonly bool doRandomizeEmptyClusters;
		public readonly bool doKHM;
		public readonly bool doJitter;
		public readonly bool staggeredJitter;
		public readonly int jitterRadius;

		private LaunchParameters() { }

		public LaunchParameters(
			int textureSize,
			int numClusters,
			bool doRandomSwap,
			bool doRandomizeEmptyClusters,
			bool doKHM,
			bool doJitter,
			bool staggeredJitter,
			int jitterRadius
		) {
			this.textureSize = textureSize;
			this.numClusters = numClusters;
			this.doRandomSwap = doRandomSwap;
			this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
			this.doKHM = doKHM;
			this.doJitter = doJitter;
			this.staggeredJitter = staggeredJitter;
			this.jitterRadius = jitterRadius;
		}
	}

	private void UpdateRandomPositions() {
		for (int k = 0; k < this.work.Peek().numClusters; k++) {
			this.randomPositions[k].x = this.random.Next(textureSize);
			this.randomPositions[k].y = this.random.Next(textureSize);
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
				textureSize & (textureSize - 1)
			) == 0 && textureSize > 0
		); // positive power of 2
		Debug.Assert(textureSize <= referenceTextureSize);

		this.csHighlightRemoval.SetInt("mip_level", this.MipLevel(textureSize));
		this.csHighlightRemoval.SetInt("ref_mip_level", this.MipLevel(referenceTextureSize));

		this.csHighlightRemoval.SetInt("texture_size", textureSize);
	}

	private void InitRTs() {
		var rtDesc = new RenderTextureDescriptor(
			textureSize,
			textureSize,
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
			textureSize,
			textureSize,
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

		this.rtInput = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat) {
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
		// texture size 1024 to 16
		for (int textureSize = 128; textureSize >= 16; textureSize /= 2) {
			// jitter 1 to 16
			for (
				int jitterRadius = 1;
				jitterRadius <= 16 && jitterRadius * textureSize <= 1024;
				jitterRadius *= 2
			) {
				// staggered vs sequential jitter
				foreach (bool staggeredJitter in new bool[] { true, false }) {
					this.work.Push(
						new LaunchParameters(
							textureSize: 1024,
							numClusters: 6,
							doRandomSwap: false,
							doRandomizeEmptyClusters: false,
							doKHM: false,
							doJitter: jitterOffset > 1,
							staggeredJitter: staggeredJitter,
							jitterRadius: jitterOffset
						)
					);
				}
			}
		}
	}

	private void IntJitterOffsets() {
		switch (this.work.Peek().jitterRadius) {
			case 16:
				this.offsets = new int[][] { new int[] { 0, 0 }, new int[] { 8, 0 }, new int[] { 0, 8 }, new int[] { 8, 8 }, new int[] { 4, 0 }, new int[] { 12, 0 }, new int[] { 4, 8 }, new int[] { 12, 8 }, new int[] { 0, 4 }, new int[] { 8, 4 }, new int[] { 0, 12 }, new int[] { 8, 12 }, new int[] { 4, 4 }, new int[] { 12, 4 }, new int[] { 4, 12 }, new int[] { 12, 12 }, new int[] { 2, 0 }, new int[] { 10, 0 }, new int[] { 2, 8 }, new int[] { 10, 8 }, new int[] { 6, 0 }, new int[] { 14, 0 }, new int[] { 6, 8 }, new int[] { 14, 8 }, new int[] { 2, 4 }, new int[] { 10, 4 }, new int[] { 2, 12 }, new int[] { 10, 12 }, new int[] { 6, 4 }, new int[] { 14, 4 }, new int[] { 6, 12 }, new int[] { 14, 12 }, new int[] { 0, 2 }, new int[] { 8, 2 }, new int[] { 0, 10 }, new int[] { 8, 10 }, new int[] { 4, 2 }, new int[] { 12, 2 }, new int[] { 4, 10 }, new int[] { 12, 10 }, new int[] { 0, 6 }, new int[] { 8, 6 }, new int[] { 0, 14 }, new int[] { 8, 14 }, new int[] { 4, 6 }, new int[] { 12, 6 }, new int[] { 4, 14 }, new int[] { 12, 14 }, new int[] { 2, 2 }, new int[] { 10, 2 }, new int[] { 2, 10 }, new int[] { 10, 10 }, new int[] { 6, 2 }, new int[] { 14, 2 }, new int[] { 6, 10 }, new int[] { 14, 10 }, new int[] { 2, 6 }, new int[] { 10, 6 }, new int[] { 2, 14 }, new int[] { 10, 14 }, new int[] { 6, 6 }, new int[] { 14, 6 }, new int[] { 6, 14 }, new int[] { 14, 14 }, new int[] { 1, 0 }, new int[] { 9, 0 }, new int[] { 1, 8 }, new int[] { 9, 8 }, new int[] { 5, 0 }, new int[] { 13, 0 }, new int[] { 5, 8 }, new int[] { 13, 8 }, new int[] { 1, 4 }, new int[] { 9, 4 }, new int[] { 1, 12 }, new int[] { 9, 12 }, new int[] { 5, 4 }, new int[] { 13, 4 }, new int[] { 5, 12 }, new int[] { 13, 12 }, new int[] { 3, 0 }, new int[] { 11, 0 }, new int[] { 3, 8 }, new int[] { 11, 8 }, new int[] { 7, 0 }, new int[] { 15, 0 }, new int[] { 7, 8 }, new int[] { 15, 8 }, new int[] { 3, 4 }, new int[] { 11, 4 }, new int[] { 3, 12 }, new int[] { 11, 12 }, new int[] { 7, 4 }, new int[] { 15, 4 }, new int[] { 7, 12 }, new int[] { 15, 12 }, new int[] { 1, 2 }, new int[] { 9, 2 }, new int[] { 1, 10 }, new int[] { 9, 10 }, new int[] { 5, 2 }, new int[] { 13, 2 }, new int[] { 5, 10 }, new int[] { 13, 10 }, new int[] { 1, 6 }, new int[] { 9, 6 }, new int[] { 1, 14 }, new int[] { 9, 14 }, new int[] { 5, 6 }, new int[] { 13, 6 }, new int[] { 5, 14 }, new int[] { 13, 14 }, new int[] { 3, 2 }, new int[] { 11, 2 }, new int[] { 3, 10 }, new int[] { 11, 10 }, new int[] { 7, 2 }, new int[] { 15, 2 }, new int[] { 7, 10 }, new int[] { 15, 10 }, new int[] { 3, 6 }, new int[] { 11, 6 }, new int[] { 3, 14 }, new int[] { 11, 14 }, new int[] { 7, 6 }, new int[] { 15, 6 }, new int[] { 7, 14 }, new int[] { 15, 14 }, new int[] { 1, 1 }, new int[] { 9, 1 }, new int[] { 1, 9 }, new int[] { 9, 9 }, new int[] { 5, 1 }, new int[] { 13, 1 }, new int[] { 5, 9 }, new int[] { 13, 9 }, new int[] { 1, 5 }, new int[] { 9, 5 }, new int[] { 1, 13 }, new int[] { 9, 13 }, new int[] { 5, 5 }, new int[] { 13, 5 }, new int[] { 5, 13 }, new int[] { 13, 13 }, new int[] { 3, 1 }, new int[] { 11, 1 }, new int[] { 3, 9 }, new int[] { 11, 9 }, new int[] { 7, 1 }, new int[] { 15, 1 }, new int[] { 7, 9 }, new int[] { 15, 9 }, new int[] { 3, 5 }, new int[] { 11, 5 }, new int[] { 3, 13 }, new int[] { 11, 13 }, new int[] { 7, 5 }, new int[] { 15, 5 }, new int[] { 7, 13 }, new int[] { 15, 13 }, new int[] { 1, 3 }, new int[] { 9, 3 }, new int[] { 1, 11 }, new int[] { 9, 11 }, new int[] { 5, 3 }, new int[] { 13, 3 }, new int[] { 5, 11 }, new int[] { 13, 11 }, new int[] { 1, 7 }, new int[] { 9, 7 }, new int[] { 1, 15 }, new int[] { 9, 15 }, new int[] { 5, 7 }, new int[] { 13, 7 }, new int[] { 5, 15 }, new int[] { 13, 15 }, new int[] { 3, 3 }, new int[] { 11, 3 }, new int[] { 3, 11 }, new int[] { 11, 11 }, new int[] { 7, 3 }, new int[] { 15, 3 }, new int[] { 7, 11 }, new int[] { 15, 11 }, new int[] { 3, 7 }, new int[] { 11, 7 }, new int[] { 3, 15 }, new int[] { 11, 15 }, new int[] { 7, 7 }, new int[] { 15, 7 }, new int[] { 7, 15 }, new int[] { 15, 15 }, new int[] { 0, 1 }, new int[] { 8, 1 }, new int[] { 0, 9 }, new int[] { 8, 9 }, new int[] { 4, 1 }, new int[] { 12, 1 }, new int[] { 4, 9 }, new int[] { 12, 9 }, new int[] { 0, 5 }, new int[] { 8, 5 }, new int[] { 0, 13 }, new int[] { 8, 13 }, new int[] { 4, 5 }, new int[] { 12, 5 }, new int[] { 4, 13 }, new int[] { 12, 13 }, new int[] { 2, 1 }, new int[] { 10, 1 }, new int[] { 2, 9 }, new int[] { 10, 9 }, new int[] { 6, 1 }, new int[] { 14, 1 }, new int[] { 6, 9 }, new int[] { 14, 9 }, new int[] { 2, 5 }, new int[] { 10, 5 }, new int[] { 2, 13 }, new int[] { 10, 13 }, new int[] { 6, 5 }, new int[] { 14, 5 }, new int[] { 6, 13 }, new int[] { 14, 13 }, new int[] { 0, 3 }, new int[] { 8, 3 }, new int[] { 0, 11 }, new int[] { 8, 11 }, new int[] { 4, 3 }, new int[] { 12, 3 }, new int[] { 4, 11 }, new int[] { 12, 11 }, new int[] { 0, 7 }, new int[] { 8, 7 }, new int[] { 0, 15 }, new int[] { 8, 15 }, new int[] { 4, 7 }, new int[] { 12, 7 }, new int[] { 4, 15 }, new int[] { 12, 15 }, new int[] { 2, 3 }, new int[] { 10, 3 }, new int[] { 2, 11 }, new int[] { 10, 11 }, new int[] { 6, 3 }, new int[] { 14, 3 }, new int[] { 6, 11 }, new int[] { 14, 11 }, new int[] { 2, 7 }, new int[] { 10, 7 }, new int[] { 2, 15 }, new int[] { 10, 15 }, new int[] { 6, 7 }, new int[] { 14, 7 }, new int[] { 6, 15 }, new int[] { 14, 15 }, };
				break;
			case 8:
				this.offsets = new int[][] { new int[] { 0, 0 }, new int[] { 4, 0 }, new int[] { 0, 4 }, new int[] { 4, 4 }, new int[] { 2, 0 }, new int[] { 6, 0 }, new int[] { 2, 4 }, new int[] { 6, 4 }, new int[] { 0, 2 }, new int[] { 4, 2 }, new int[] { 0, 6 }, new int[] { 4, 6 }, new int[] { 2, 2 }, new int[] { 6, 2 }, new int[] { 2, 6 }, new int[] { 6, 6 }, new int[] { 1, 0 }, new int[] { 5, 0 }, new int[] { 1, 4 }, new int[] { 5, 4 }, new int[] { 3, 0 }, new int[] { 7, 0 }, new int[] { 3, 4 }, new int[] { 7, 4 }, new int[] { 1, 2 }, new int[] { 5, 2 }, new int[] { 1, 6 }, new int[] { 5, 6 }, new int[] { 3, 2 }, new int[] { 7, 2 }, new int[] { 3, 6 }, new int[] { 7, 6 }, new int[] { 1, 1 }, new int[] { 5, 1 }, new int[] { 1, 5 }, new int[] { 5, 5 }, new int[] { 3, 1 }, new int[] { 7, 1 }, new int[] { 3, 5 }, new int[] { 7, 5 }, new int[] { 1, 3 }, new int[] { 5, 3 }, new int[] { 1, 7 }, new int[] { 5, 7 }, new int[] { 3, 3 }, new int[] { 7, 3 }, new int[] { 3, 7 }, new int[] { 7, 7 }, new int[] { 0, 1 }, new int[] { 4, 1 }, new int[] { 0, 5 }, new int[] { 4, 5 }, new int[] { 2, 1 }, new int[] { 6, 1 }, new int[] { 2, 5 }, new int[] { 6, 5 }, new int[] { 0, 3 }, new int[] { 4, 3 }, new int[] { 0, 7 }, new int[] { 4, 7 }, new int[] { 2, 3 }, new int[] { 6, 3 }, new int[] { 2, 7 }, new int[] { 6, 7 }, };
				break;
			case 4:
				this.offsets = new int[][] { new int[] { 0, 0 }, new int[] { 2, 0 }, new int[] { 0, 2 }, new int[] { 2, 2 }, new int[] { 1, 0 }, new int[] { 3, 0 }, new int[] { 1, 2 }, new int[] { 3, 2 }, new int[] { 1, 1 }, new int[] { 3, 1 }, new int[] { 1, 3 }, new int[] { 3, 3 }, new int[] { 0, 1 }, new int[] { 2, 1 }, new int[] { 0, 3 }, new int[] { 2, 3 }, };
				break;
			case 2:
				this.offsets = new int[][] { new int[] { 0, 0 }, new int[] { 1, 0 }, new int[] { 1, 1 }, new int[] { 0, 1 }, };
				break;
			default:
				throw new System.Exception($"invalid jitter radius: {this.work.Peek().jitterRadius}");

		}
	}

	private void OnEnable() {
		this.randomPositions = new Position[this.work.Peek().numClusters];
		this.clusterCenters = new Vector4[this.work.Peek().numClusters * 2];

		this.IntJitterOffsets();
		this.FindKernels();
		this.SetTextureSize();
		this.InitRTs();
		this.InitCbufs();

		this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
		this.videoPlayer.playbackSpeed = 0;

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
		return float.IsNaN(MSE) == false ? MSE : this.videoPlayer.frame < 5 ? 0 : throw new System.Exception("no MSE!");
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
		if (!this.videoPlayer.isPrepared) {
			Graphics.Blit(src, dest);
			return;
		}

		if (this.videoPlayer.frame == (long)this.videoPlayer.frameCount - 1) {
			Graphics.Blit(src, dest);
			Debug.Log(this.frameLogMSE.Count);

			/*
				num. iterations
				texture size
				num clusters
				random swap (y/n)
				randomize empty clusters (y/n)
				KHM (y/n)
				jitter radius
				staggered jitter
				video (1/2)
			*/
			int numIterations = 3;
			int textureSize = this.work.Peek().textureSize;
			int numClusters = this.work.Peek().numClusters;
			bool doRandomSwap = this.work.Peek().doRandomSwap;
			bool doRandomizeEmptyClusters = this.work.Peek().doRandomizeEmptyClusters;
			bool doKHM = this.work.Peek().doKHM;
			int jitterRadius = this.work.Peek().jitterRadius;
			bool staggeredJitter = this.work.Peek().staggeredJitter;
			string video = "2";

			string fileName = $"MSE logs/{numIterations}|{textureSize}|{numClusters}|{doRandomSwap}|{doRandomizeEmptyClusters}|{doKHM}|{jitterRadius}|{staggeredJitter}|{video}.csv";

			if (System.IO.File.Exists(fileName)) {
				System.IO.File.Delete(fileName);
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
				Debug.Log($"entries: {this.frameLogMSE.Count}");
			}
			System.Threading.Thread.Sleep(3000);

			this.work.Pop();
			this.OnDisable();
			this.OnEnable();

			if (this.work.Count == 0) {
				UnityEditor.EditorApplication.isPlaying = false;
			}
			return;
		}

		Graphics.Blit(this.videoPlayer.texture, this.rtReference);

		this.csHighlightRemoval.SetInt("sub_sample_multiplier", referenceTextureSize / textureSize);
		this.csHighlightRemoval.SetInts(
			"sub_sample_offset",
			this.work.Peek().doJitter ?
				(
					this.work.Peek().staggeredJitter ?
						this.offsets[
							Time.frameCount % this.offsets.Length
						] :
						new int[] {
							Time.frameCount % this.offsets.Length,
							(Time.frameCount / this.offsets.Length) % this.offsets.Length
						}
				) :
				new int[] { 0, 0 }
		);
		this.csHighlightRemoval.SetTexture(this.kernelsubsample, "tex_input", this.rtReference);
		this.csHighlightRemoval.SetTexture(this.kernelsubsample, "tex_output", this.rtInput);
		this.csHighlightRemoval.Dispatch(this.kernelsubsample, textureSize / 16, textureSize / 16, 1);

		this.videoPlayer.StepForward();
		if (this.previousFrame == null) {
			if (this.videoPlayer.frame > 0) {
				this.previousFrame = this.videoPlayer.frame;
			}
		} else if (this.videoPlayer.frame != this.previousFrame + 1) {
			throw new System.Exception($"current frame: {this.videoPlayer.frame}\nprevious frame: {this.previousFrame}");
		} else {
			this.previousFrame++;
		}

		this.ClusteringIteration();

		if (Time.time - this.timeLastIteration > timeStep) {
			this.timeLastIteration = Time.time;
			this.showReference = !this.showReference;
			this.LogMSE();
		}

		this.frameLogMSE.Add(this.GetMSE());

		this.RenderResult();
		Graphics.Blit(this.rtResult, dest);
		this.rtResult.DiscardContents();
	}

	private void OnDisable() {
		this.rtArr.Release();
		this.rtResult.Release();
		this.rtInput.Release();
		this.rtMSE.Release();
		this.rtReference.Release();

		this.cbufClusterCenters.Release();
		this.cbufRandomPositions.Release();
	}

	private void RenderResult() {
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_output", this.rtResult);
		this.csHighlightRemoval.SetBuffer(this.kernelShowResult, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBool("show_reference", this.showReference);
		this.csHighlightRemoval.SetTexture(this.kernelShowResult, "tex_input", this.rtInput);
		this.csHighlightRemoval.Dispatch(this.kernelShowResult, textureSize / 16, textureSize / 16, 1);
	}
}
