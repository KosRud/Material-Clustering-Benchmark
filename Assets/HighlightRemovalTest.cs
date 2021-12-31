using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int textureSize = 1024;
	private const int referenceTextureSize = 1024;

	private const int numClusters = 6;
	private const bool doRandomSwap = false;
	private const bool doRandomizeEmptyClusters = false;
	private const bool doKHM = false;
	private const bool doJitter = false;

	private const float timeStep = 1f;

	private float timeLastIteration = 0;
	private long? previousFrame = null;
	private bool showReference = false;
	private readonly int[][] offsets = new int[][] { new int[] { 0, 0 }, new int[] { 4, 0 }, new int[] { 0, 4 }, new int[] { 4, 4 }, new int[] { 2, 0 }, new int[] { 6, 0 }, new int[] { 2, 4 }, new int[] { 6, 4 }, new int[] { 0, 2 }, new int[] { 4, 2 }, new int[] { 0, 6 }, new int[] { 4, 6 }, new int[] { 2, 2 }, new int[] { 6, 2 }, new int[] { 2, 6 }, new int[] { 6, 6 }, new int[] { 1, 0 }, new int[] { 5, 0 }, new int[] { 1, 4 }, new int[] { 5, 4 }, new int[] { 3, 0 }, new int[] { 7, 0 }, new int[] { 3, 4 }, new int[] { 7, 4 }, new int[] { 1, 2 }, new int[] { 5, 2 }, new int[] { 1, 6 }, new int[] { 5, 6 }, new int[] { 3, 2 }, new int[] { 7, 2 }, new int[] { 3, 6 }, new int[] { 7, 6 }, new int[] { 1, 1 }, new int[] { 5, 1 }, new int[] { 1, 5 }, new int[] { 5, 5 }, new int[] { 3, 1 }, new int[] { 7, 1 }, new int[] { 3, 5 }, new int[] { 7, 5 }, new int[] { 1, 3 }, new int[] { 5, 3 }, new int[] { 1, 7 }, new int[] { 5, 7 }, new int[] { 3, 3 }, new int[] { 7, 3 }, new int[] { 3, 7 }, new int[] { 7, 7 }, new int[] { 0, 1 }, new int[] { 4, 1 }, new int[] { 0, 5 }, new int[] { 4, 5 }, new int[] { 2, 1 }, new int[] { 6, 1 }, new int[] { 2, 5 }, new int[] { 6, 5 }, new int[] { 0, 3 }, new int[] { 4, 3 }, new int[] { 0, 7 }, new int[] { 4, 7 }, new int[] { 2, 3 }, new int[] { 6, 3 }, new int[] { 2, 7 }, new int[] { 6, 7 }, };

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

	private readonly System.Random random = new System.Random(1);
	private readonly Position[] randomPositions = new Position[numClusters];
	private readonly Vector4[] clusterCenters = new Vector4[numClusters * 2];

	private UnityEngine.Video.VideoPlayer videoPlayer;
	private readonly System.Collections.Generic.List<float> frameLogMSE = new System.Collections.Generic.List<float>();

	public ComputeShader csHighlightRemoval;
	public Texture2D texInput;

	private void UpdateRandomPositions() {
		for (int k = 0; k < numClusters; k++) {
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
		this.cbufClusterCenters = new ComputeBuffer(numClusters * 2, sizeof(float) * 4);
		this.cbufRandomPositions = new ComputeBuffer(numClusters, sizeof(int) * 4);

		for (int i = 0; i < this.clusterCenters.Length; i++) {
			// "old" cluster centers with infinite MSE
			// to make sure new ones will overwrite them when validated
			var c = Color.HSVToRGB(
				i / (float)(numClusters),
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

	// Start is called before the first frame update
	private void Start() {
		this.FindKernels();
		this.SetTextureSize();
		this.InitRTs();
		this.InitCbufs();

		this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
		this.videoPlayer.playbackSpeed = 0;

		this.csHighlightRemoval.SetBool("do_random_sample_empty_clusters", doRandomizeEmptyClusters);
		this.csHighlightRemoval.SetBool("KHM", doKHM);
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
		this.csHighlightRemoval.SetInt("randomClusterCenter", this.random.Next(numClusters));
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
		if (float.IsNaN(MSE) == false) {
			return MSE;
		}

		return this.videoPlayer.frame < 5 ? (float)0 : throw new System.Exception("no MSE!");
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
		if (doRandomSwap) {
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
			using (
				System.IO.FileStream fs = System.IO.File.Open(
					"frameLogMSE.csv", System.IO.FileMode.OpenOrCreate
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
			UnityEditor.EditorApplication.isPlaying = false;
			return;
		}

		Graphics.Blit(this.videoPlayer.texture, this.rtReference);

		this.csHighlightRemoval.SetInt("sub_sample_multiplier", referenceTextureSize / textureSize);
		this.csHighlightRemoval.SetInts(
			"sub_sample_offset",
			doJitter ?
				this.offsets[
					Time.frameCount % this.offsets.Length
				] :
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
