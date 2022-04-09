using UnityEngine;

public class ClusteringTextures : System.IDisposable {
  public readonly RenderTexture rtInput;
  public readonly RenderTexture rtArr;

  private RenderTexture MakeRtArr(int textureSize) {
    var rtDesc = new RenderTextureDescriptor(
      textureSize,
      textureSize,
      RenderTextureFormat.ARGBFloat,
      0
    ) {
      dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
      volumeDepth = ClusteringTest.maxNumClusters,
      useMipMap = true,
      autoGenerateMips = false
    };

    return new RenderTexture(rtDesc) {
      enableRandomWrite = true
    };
  }

  public ClusteringTextures(int size) {
    this.rtInput = new RenderTexture(
      size, size,
      0,
      RenderTextureFormat.ARGBFloat
    ) {
      enableRandomWrite = true
    };

    this.rtArr = this.MakeRtArr(size);
  }

  public int size => this.rtInput.width;

  public void Dispose() {
    this.rtInput.Release();
    this.rtArr.Release();
  }
}