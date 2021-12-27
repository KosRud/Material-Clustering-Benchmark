using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int numClusters = 6;
	private const bool doRandomSwap = false;
	private const bool doRandomInitialAttribution = false;
	private const float timeStep = 2f;

	private float timeLastIteration = 0;

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

	public ComputeShader csHighlightRemoval;
	public Texture2D texInput;

	private void UpdateRandomPositions() {
		for (int k = 0; k < numClusters; k++) {
			this.randomPositions[k].x = this.random.Next(1024);
			this.randomPositions[k].y = this.random.Next(1024);
		}
		this.cbufRandomPositions.SetData(this.randomPositions);
	}

	// Start is called before the first frame update
	private void Start() {
		var rtDesc = new RenderTextureDescriptor(1024, 1024, RenderTextureFormat.ARGBFloat, 0) {
			dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
			volumeDepth = 16,
			useMipMap = true,
			autoGenerateMips = false
		};

		this.rtArr = new RenderTexture(rtDesc) {
			enableRandomWrite = true
		};
		this.rtResult = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat) {
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

		this.rtInput = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
		Graphics.Blit(this.texInput, this.rtInput);

		if (doRandomInitialAttribution) {
			int kh_AttributeInitialClusters = this.csHighlightRemoval.FindKernel("AttributeInitialClusters");
			this.csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_input", this.rtInput);
			this.csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_arr_clusters_rw", this.rtArr);
			this.csHighlightRemoval.SetBuffer(kh_AttributeInitialClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
			this.csHighlightRemoval.Dispatch(kh_AttributeInitialClusters, 1024 / 32, 1024 / 32, 1);

			this.rtArr.GenerateMips();

			this.UpdateClusterCenters(true);
		}

		this.KMeans();
		this.KMeans(true);
	}

	private void AttributeClusters(bool final = false) {
		int kh_AttributeClusters = this.csHighlightRemoval.FindKernel("AttributeClusters");
		this.csHighlightRemoval.SetBool("final", final);  // replace with define
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_arr_clusters_rw", this.rtArr);
		this.csHighlightRemoval.SetBuffer(kh_AttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(kh_AttributeClusters, 1024 / 32, 1024 / 32, 1);
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

	private void LogMSE() {
		this.cbufClusterCenters.GetData(this.clusterCenters);

		for (int i = 0; i < numClusters; i++) {
			float MSE = this.clusterCenters[i].z;
			if (MSE != Mathf.Infinity) {
				Debug.Log($"MSE: {(int)(MSE * 1000000),8}");
				break;
			} else {
				if (i == numClusters) {
					Debug.Log("something went horribly wrong...");
				}
			}
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

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
		void ClusteringIteration() {
			if (doRandomSwap) {
				this.KMeans();
				this.KMeans();
				this.KMeans(true);  // discard old saved clusters, update MSE

				// normally the data does not change between clustering iterations
				// in our case it does
				// so we need to account for it by running a few K-Means iterations
				// before opting for a random swap attempt

				cbufClusterCenters.GetData(this.clusterCenters);
				for (int i = 0; i < numClusters; i++) {
					float MSE = this.clusterCenters[i].z;
					if (MSE != Mathf.Infinity) {
						Debug.Log($"before swap | MSE: {(int)(MSE * 1000000),8}");
						break;
					} else {
						if (i == numClusters) {
							Debug.Log("something went horribly wrong...");
						}
					}
				}

				this.RandomSwap();

				this.KMeans();
				this.KMeans();

				cbufClusterCenters.GetData(this.clusterCenters);
				for (int i = 0; i < numClusters; i++) {
					float MSE = this.clusterCenters[i].z;
					if (MSE != Mathf.Infinity) {
						Debug.Log($" after swap | MSE: {(int)(MSE * 1000000),8}");
						break;
					} else {
						if (i == numClusters) {
							Debug.Log("something went horribly wrong...");
						}
					}
				}

				this.ValidateCandidates();
			} else {
				this.KMeans();
				this.KMeans();
				this.KMeans();

				cbufClusterCenters.GetData(this.clusterCenters);
				for (int i = 0; i < numClusters; i++) {
					float MSE = this.clusterCenters[i].z;
					if (MSE != Mathf.Infinity) {
						Debug.Log($" after 3 | MSE: {(int)(MSE * 1000000),8}");
						break;
					} else {
						if (i == numClusters) {
							Debug.Log("something went horribly wrong...");
						}
					}
				}

				this.KMeans();
				this.KMeans();

				cbufClusterCenters.GetData(this.clusterCenters);
				for (int i = 0; i < numClusters; i++) {
					float MSE = this.clusterCenters[i].z;
					if (MSE != Mathf.Infinity) {
						Debug.Log($" after 5 | MSE: {(int)(MSE * 1000000),8}");
						break;
					} else {
						if (i == numClusters) {
							Debug.Log("something went horribly wrong...");
						}
					}
				}
				// no need to discard old saved clusters
				// we never validate / restore
			}
		}

		Graphics.Blit(this.texInput, this.rtInput);

		if (Time.time - this.timeLastIteration > timeStep) {
			this.timeLastIteration = Time.time;
			ClusteringIteration();
			this.AttributeClusters(true);
			//this.LogMSE();
		}

		int kh_ShowResult = this.csHighlightRemoval.FindKernel("ShowResult");
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_output", this.rtResult);
		this.csHighlightRemoval.SetBuffer(kh_ShowResult, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetTexture(kh_ShowResult, "tex_input", this.rtInput);
		this.csHighlightRemoval.Dispatch(kh_ShowResult, 1024 / 32, 1024 / 32, 1);
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
}
