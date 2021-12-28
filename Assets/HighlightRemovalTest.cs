using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int numClusters = 6;
	private const bool doRandomSwap = false;
	private const bool doRandomInitialAttribution = false;
	private const float timeStep = 1f;

	private float timeLastIteration = 0;
	private long? previousFrame = null;

	private struct Position {
		public int x;
		public int y;
	}

	private RenderTexture rtArr;
	private RenderTexture rtResult;
	private ComputeBuffer cbufClusterCenters;
	private ComputeBuffer cbufRandomPositions;
	private RenderTexture rtInput;
	private readonly System.Random random = new System.Random(1);
	private readonly Position[] randomPositions = new Position[numClusters];
	private readonly Vector3[] clusterCenters = new Vector3[numClusters * 2];

	private UnityEngine.Video.VideoPlayer videoPlayer;
	private readonly System.Collections.Generic.List<float> frameLogMSE = new System.Collections.Generic.List<float>();

	public ComputeShader csHighlightRemoval;
	public Texture2D texInput;

	private void UpdateRandomPositions() {
		for (int k = 0; k < numClusters; k++) {
			this.randomPositions[k].x = this.random.Next(512);
			this.randomPositions[k].y = this.random.Next(512);
		}
		this.cbufRandomPositions.SetData(this.randomPositions);
	}

	// Start is called before the first frame update
	private void Start() {
		this.videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
		this.videoPlayer.playbackSpeed = 0;

		var rtDesc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0) {
			dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
			volumeDepth = 16,
			useMipMap = true,
			autoGenerateMips = false
		};

		this.rtArr = new RenderTexture(rtDesc) {
			enableRandomWrite = true
		};
		this.rtResult = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat) {
			enableRandomWrite = true
		};

		// second half of the buffer contains candidate cluster centers
		// first half contains current cluster centers
		this.cbufClusterCenters = new ComputeBuffer(numClusters * 2, sizeof(float) * 3);
		this.cbufRandomPositions = new ComputeBuffer(numClusters, sizeof(int) * 2);

		for (int i = 0; i < this.clusterCenters.Length; i++) {
			// "old" cluster centers with infinite MSE
			// to make sure new ones will overwrite them when validated
			var c = Color.HSVToRGB(
				//this.random.Next(10000) / 10000.0f,
				i / (float)(numClusters),
				1,
				1
			);
			c *= 1.0f / (c.r + c.g + c.b);
			this.clusterCenters[i] = new Vector3(c.r, c.g, Mathf.Infinity);
		}
		this.cbufClusterCenters.SetData(this.clusterCenters);

		this.rtInput = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat);
	}

	private void AttributeClusters(bool final = false) {
		int kh_AttributeClusters = this.csHighlightRemoval.FindKernel("AttributeClusters");
		this.csHighlightRemoval.SetBool("final", final);  // replace with define
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_arr_clusters_rw", this.rtArr);
		this.csHighlightRemoval.SetBuffer(kh_AttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(kh_AttributeClusters, 512 / 32, 512 / 32, 1);
	}

	private void UpdateClusterCenters(bool rejectOld) {
		this.UpdateRandomPositions();

		int kh_UpdateClusterCenters = this.csHighlightRemoval.FindKernel("UpdateClusterCenters");
		this.csHighlightRemoval.SetBool("rejectOld", rejectOld);
		this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_random_positions", this.cbufRandomPositions);
		this.csHighlightRemoval.Dispatch(kh_UpdateClusterCenters, 1, 1, 1);
	}

	private void KMeans(bool rejectOld = false) {
		this.AttributeClusters();
		this.rtArr.GenerateMips();

		this.UpdateClusterCenters(rejectOld);

		//this.LogMSE();
	}

	private void RandomSwap() {
		this.UpdateRandomPositions();
		int kh_RandomSwap = this.csHighlightRemoval.FindKernel("RandomSwap");
		this.csHighlightRemoval.SetBuffer(kh_RandomSwap, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBuffer(kh_RandomSwap, "cbuf_random_positions", this.cbufClusterCenters);
		this.csHighlightRemoval.SetTexture(kh_RandomSwap, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetInt("randomClusterCenter", this.random.Next(numClusters));
		this.csHighlightRemoval.Dispatch(kh_RandomSwap, 1, 1, 1);
	}

	private float GetMSE() {
		this.cbufClusterCenters.GetData(this.clusterCenters);

		for (int i = 0; i < numClusters; i++) {
			float MSE = this.clusterCenters[i].z;
			if (MSE != Mathf.Infinity) {
				return MSE;
			}
		}

		if (this.videoPlayer.frame > 5) {
			throw new System.Exception("no MSE!");
		}

		return -1;
	}

	private void LogMSE() {
		this.cbufClusterCenters.GetData(this.clusterCenters);

		float MSE = -1;
		float emptyClusters = 0;

		for (int i = 0; i < numClusters; i++) {
			float x = this.clusterCenters[i].z;
			if (x != Mathf.Infinity && MSE == -1) {
				MSE = x;
			}
			if (x > 100) {
				emptyClusters++;
			}
		}

		if (MSE == -1 && this.videoPlayer.frame > 5) {
			throw new System.Exception("no MSE!");
		}

		Debug.Log($"MSE: {(int)(this.GetMSE() * 1000000),8}");
		if (emptyClusters != 0) {
			Debug.Log($"empty clusters: {emptyClusters}");
		}
	}

	private void ValidateCandidates() {
		int kh_ValidateCandidates = this.csHighlightRemoval.FindKernel("ValidateCandidates");
		this.csHighlightRemoval.SetBuffer(kh_ValidateCandidates, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(kh_ValidateCandidates, 1, 1, 1);
	}

	// Update is called once per frame
	private void Update() {

	}

	private void ClusteringIteration() {
		if (doRandomSwap) {
			this.KMeans(true);  // discard old saved clusters, update MSE

			//Debug.Log("doing random swap");
			this.RandomSwap();

			// adjust after swap
			this.KMeans();
			this.KMeans();

			this.ValidateCandidates();
			//Debug.Log("validation");
			//this.LogMSE();
		} else {
			this.KMeans();
			this.KMeans();
			//this.KMeans();
			// no need to discard old saved clusters
			// we never validate / restore
		}
		this.AttributeClusters(true);
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
				var sw = new System.IO.StreamWriter(fs);
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
			}
			System.Threading.Thread.Sleep(3000);
			UnityEditor.EditorApplication.isPlaying = false;
			return;
		}

		Graphics.Blit(this.videoPlayer.texture, this.rtInput);
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
			float val = this.videoPlayer.frame / (float)this.videoPlayer.frameCount * 100;
			Debug.Log($"{val:0.}%");
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
		this.cbufClusterCenters.Release();
		this.cbufRandomPositions.Release();
		this.rtInput.Release();
	}

	private void RenderResult() {
		int kh_ShowResult = this.csHighlightRemoval.FindKernel("ShowResult");
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_output", this.rtResult);
		this.csHighlightRemoval.SetBuffer(kh_ShowResult, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_input", this.rtInput);
		this.csHighlightRemoval.Dispatch(kh_ShowResult, 512 / 32, 512 / 32, 1);
	}
}
