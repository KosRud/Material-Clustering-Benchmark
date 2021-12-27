using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private const int numClusters = 6;
	private const bool doRandomSwap = true;
	private const bool doRandomInitialAttribution = false;
	private const float timeStep = 0.5f;

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

			this.UpdateClusterCenters();
		}

		this.KMeans();
		this.KMeans();
		this.ValidateCandidates(); // guaranteed to succeed - MSE initialized with infinity
	}

	private void AttributeClusters(bool final = false) {
		int kh_AttributeClusters = this.csHighlightRemoval.FindKernel("AttributeClusters");
		this.csHighlightRemoval.SetBool("final", final);  // replace with define
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_arr_clusters_rw", this.rtArr);
		this.csHighlightRemoval.SetBuffer(kh_AttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(kh_AttributeClusters, 1024 / 32, 1024 / 32, 1);
	}

	private void UpdateClusterCenters() {
		this.UpdateRandomPositions();

		int kh_UpdateClusterCenters = this.csHighlightRemoval.FindKernel("UpdateClusterCenters");
		this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_arr_clusters_r", this.rtArr);
		this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_random_positions", this.cbufRandomPositions);
		this.csHighlightRemoval.Dispatch(kh_UpdateClusterCenters, 1, 1, 1);
	}

	private void KMeans() {
		this.AttributeClusters();
		this.rtArr.GenerateMips();

		this.UpdateClusterCenters();
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
			this.UpdateRandomPositions();

			if (doRandomSwap) {
				this.KMeans();  // update MSE in case if input changed

				int kh_RandomSwap = this.csHighlightRemoval.FindKernel("RandomSwap");
				this.csHighlightRemoval.SetBuffer(kh_RandomSwap, "cbuf_cluster_centers", this.cbufClusterCenters);
				this.csHighlightRemoval.SetBuffer(kh_RandomSwap, "cbuf_random_positions", this.cbufClusterCenters);
				this.csHighlightRemoval.SetTexture(kh_RandomSwap, "tex_input", this.rtInput);
				this.csHighlightRemoval.SetInt("randomClusterCenter", this.random.Next(numClusters));
				this.csHighlightRemoval.Dispatch(kh_RandomSwap, 1, 1, 1);

				this.KMeans();
				this.KMeans();

				this.ValidateCandidates();
			} else {
				this.KMeans();
				this.KMeans();
				this.KMeans();
			}
		}

		Graphics.Blit(this.texInput, this.rtInput);

		if (Time.time - this.timeLastIteration > timeStep) {
			this.timeLastIteration = Time.time;
			ClusteringIteration();
			this.AttributeClusters(true);
			this.LogMSE();
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
