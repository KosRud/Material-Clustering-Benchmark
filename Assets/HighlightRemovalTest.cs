using UnityEngine;

public class HighlightRemovalTest : MonoBehaviour
{
	private RenderTexture rtArr;
	private RenderTexture rtResult;
	private ComputeBuffer cbufClusterCenters;
	private RenderTexture rtInput;

	public ComputeShader csHighlightRemoval;
	public Texture2D texInput;

	// Start is called before the first frame update
	void Start()
	{
		var rtDesc = new RenderTextureDescriptor(1024, 1024, RenderTextureFormat.ARGBFloat, 0)
		{
			dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
			volumeDepth = 16,
			useMipMap = true,
			autoGenerateMips = false
		};

		this.rtArr = new RenderTexture(rtDesc)
		{
			enableRandomWrite = true
		};
		this.rtResult = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat)
		{
			enableRandomWrite = true
		};

		this.cbufClusterCenters = new ComputeBuffer(16, sizeof(float) * 2);

		this.rtInput = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat)
		{
			useMipMap = true,
			autoGenerateMips = false,
		};
		Graphics.Blit(this.texInput, this.rtInput);
		this.rtInput.GenerateMips();

		// initial clusters

		int kh_AttributeInitialClusters = csHighlightRemoval.FindKernel("AttributeInitialClusters");
		csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_input", rtInput);
		csHighlightRemoval.SetTexture(kh_AttributeInitialClusters, "tex_arr_clusters_rw", rtArr);
		csHighlightRemoval.SetBuffer(kh_AttributeInitialClusters, "cbuf_cluster_centers", cbufClusterCenters);
		csHighlightRemoval.Dispatch(kh_AttributeInitialClusters, 1024 / 32, 1024 / 32, 1);

		// end initial clusters

		const int numIter = 16;

		for (int i = 0; i < numIter; i++)
		{
			rtArr.GenerateMips();

			int kh_UpdateClusterCenters = csHighlightRemoval.FindKernel("UpdateClusterCenters");
			csHighlightRemoval.SetTexture(kh_UpdateClusterCenters, "tex_arr_clusters_r", rtArr);
			csHighlightRemoval.SetBuffer(kh_UpdateClusterCenters, "cbuf_cluster_centers", cbufClusterCenters);
			csHighlightRemoval.Dispatch(kh_UpdateClusterCenters, 1, 1, 1);

			int kh_AttributeClusters = csHighlightRemoval.FindKernel("AttributeClusters");
			csHighlightRemoval.SetBool("final", i == numIter - 1);  // replace with define
			csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_input", rtInput);
			csHighlightRemoval.SetTexture(kh_AttributeClusters, "tex_arr_clusters_rw", rtArr);
			csHighlightRemoval.SetBuffer(kh_AttributeClusters, "cbuf_cluster_centers", cbufClusterCenters);
			csHighlightRemoval.Dispatch(kh_AttributeClusters, 1024 / 32, 1024 / 32, 1);
		}
	}

	// Update is called once per frame
	void Update()
	{

	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		int kh_ShowResult = csHighlightRemoval.FindKernel("ShowResult");
		csHighlightRemoval.SetTexture(kh_ShowResult, "tex_arr_clusters_r", rtArr);
		csHighlightRemoval.SetTexture(kh_ShowResult, "tex_output", rtResult);
		csHighlightRemoval.SetBuffer(kh_ShowResult, "cbuf_cluster_centers", cbufClusterCenters);
		csHighlightRemoval.SetTexture(kh_ShowResult, "tex_input", rtInput);
		csHighlightRemoval.Dispatch(kh_ShowResult, 1024 / 32, 1024 / 32, 1);
		Graphics.Blit(rtResult, dest);
		rtResult.DiscardContents();
	}

	void OnDisable()
	{
		this.rtArr.Release();
		this.rtResult.Release();
		this.cbufClusterCenters.Release();
		this.rtInput.Release();
	}
}
