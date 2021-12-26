using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour {
	private RenderTexture rtArr;
	private RenderTexture rtResult;
	private ComputeBuffer cbufClusterCenters;
	private ComputeBuffer cbufRandomPositions;
	private RenderTexture rtInput;

	public ComputeShader csHighlightRemoval;
	public Texture2D texInput;

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
		this.cbufClusterCenters = new ComputeBuffer(32, sizeof(float) * 3);
		this.cbufRandomPositions = new ComputeBuffer(16, sizeof(float) * 2);

		var clusterCenters = new Vector3[32];
		for (int i = 0; i < 32; i++) {
			// "old" cluster centers with infinite MSE
			// to make sure new ones will overwrite them when validated
			clusterCenters[i] = new Vector3(0, 0, Mathf.Infinity);
		}

		this.rtInput = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat) {
			useMipMap = true,
			autoGenerateMips = false,
		};
		Graphics.Blit(this.texInput, this.rtInput);
		this.rtInput.GenerateMips();

		// initial clusters

		int kh_AttributeInitialClusters = this.csHighlightRemoval.FindKernel("AttributeInitialClusters");
		this.csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_input", this.rtInput);
		this.csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_arr_clusters_rw", this.rtArr);
		this.csHighlightRemoval.SetBuffer(kh_AttributeInitialClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
		this.csHighlightRemoval.Dispatch(kh_AttributeInitialClusters, 1024 / 32, 1024 / 32, 1);

		void KMeans(bool final = false) {
			this.rtArr.GenerateMips();

			int kh_UpdateClusterCenters = this.csHighlightRemoval.FindKernel("UpdateClusterCenters");
			this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_arr_clusters_r", this.rtArr);
			this.csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_input", this.rtInput);
			this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_cluster_centers", this.cbufClusterCenters);
			this.csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_random_positions", this.cbufRandomPositions);
			this.csHighlightRemoval.Dispatch(kh_UpdateClusterCenters, 1, 1, 1);

			int kh_AttributeClusters = this.csHighlightRemoval.FindKernel("AttributeClusters");
			this.csHighlightRemoval.SetBool("final", final);  // replace with define
			this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_input", this.rtInput);
			this.csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_arr_clusters_rw", this.rtArr);
			this.csHighlightRemoval.SetBuffer(kh_AttributeClusters, "cbuf_cluster_centers", this.cbufClusterCenters);
			this.csHighlightRemoval.Dispatch(kh_AttributeClusters, 1024 / 32, 1024 / 32, 1);
		}

		KMeans();
		KMeans();
	}

	// Update is called once per frame
	private void Update() {

	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
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
