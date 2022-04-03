using UnityEngine;

namespace ClusteringAlgorithms {

public abstract class ADispatcher {

  // reported in the file
  public readonly int numClusters;
  public readonly bool doRandomizeEmptyClusters;
  public readonly int numIterations;

  public abstract string descriptionString {
    get;
  }

  // internal
  protected readonly int kernelSize;
  protected readonly ComputeShader computeShader;
  private readonly int kernelHandleAttributeClusters;
  private readonly int kernelUpdateClusterCenters;
  private readonly int kernelGenerateVariance;
  private readonly int kernelGatherVariance;

  protected ADispatcher(int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters) {
    this.kernelSize = kernelSize;
    this.computeShader = computeShader;
    this.kernelHandleAttributeClusters = this.computeShader.FindKernel("AttributeClusters");
    this.kernelUpdateClusterCenters = computeShader.FindKernel("UpdateClusterCenters");
    this.kernelGenerateVariance = this.computeShader.FindKernel("GenerateVariance");
    this.kernelGatherVariance = this.computeShader.FindKernel("GatherVariance");
    this.numClusters = numClusters;
    this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
    this.numIterations = numIterations;
  }

  public abstract void RunClustering(
    Texture inputTex,
    int textureSize,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers
  );

  protected void UpdateClusterCenters(
    Texture inputTex,
    int textureSize,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers,
    bool rejectOld
  ) {
    clusteringRTsAndBuffers.UpdateRandomPositions(textureSize);

    this.computeShader.SetBool("reject_old", rejectOld);
    this.computeShader.SetTexture(
      this.kernelUpdateClusterCenters,
      "tex_arr_clusters_r",
      clusteringRTsAndBuffers.rtArr
    );
    this.computeShader.SetTexture(this.kernelUpdateClusterCenters, "tex_input", inputTex);
    this.computeShader.SetBuffer(
      this.kernelUpdateClusterCenters,
      "cbuf_cluster_centers",
      clusteringRTsAndBuffers.cbufClusterCenters
    );
    this.computeShader.SetBuffer(
      this.kernelUpdateClusterCenters,
      "cbuf_random_positions",
      clusteringRTsAndBuffers.cbufRandomPositions
    );
    this.computeShader.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
  }

  public void AttributeClusters(
    Texture inputTex,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers,
    bool final,
    bool khm
  ) {
    this.computeShader.SetBool("KHM", khm);
    this.computeShader.SetBool("final", final);

    this.computeShader.SetTexture(
      this.kernelHandleAttributeClusters,
      "tex_input",
      inputTex
    );
    this.computeShader.SetTexture(
      this.kernelHandleAttributeClusters,
      "tex_arr_clusters_rw",
      clusteringRTsAndBuffers.rtArr
    );
    this.computeShader.SetBuffer(
      this.kernelHandleAttributeClusters,
      "cbuf_cluster_centers",
      clusteringRTsAndBuffers.cbufClusterCenters
    );
    this.computeShader.Dispatch(
      this.kernelHandleAttributeClusters,
      inputTex.width / this.kernelSize,
      inputTex.height / this.kernelSize,
      1
    );

    clusteringRTsAndBuffers.rtArr.GenerateMips();
  }

  public float GetVariance(ClusteringRTsAndBuffers clusteringRTsAndBuffers) {
    this.computeShader.SetTexture(
      this.kernelGenerateVariance,
      "tex_input",
      clusteringRTsAndBuffers.texReference
    );
    this.computeShader.SetTexture(
      this.kernelGenerateVariance,
      "tex_variance_rw",
      clusteringRTsAndBuffers.rtVariance
    );
    this.computeShader.SetBuffer(
      this.kernelGenerateVariance,
      "cbuf_cluster_centers",
      clusteringRTsAndBuffers.cbufClusterCenters
    );
    this.computeShader.Dispatch(
      this.kernelGenerateVariance,
      clusteringRTsAndBuffers.texReference.width / this.kernelSize,
      clusteringRTsAndBuffers.texReference.height / this.kernelSize,
      1);

    clusteringRTsAndBuffers.rtVariance.GenerateMips();

    this.computeShader.SetTexture(
      this.kernelGatherVariance,
      "tex_variance_r",
      clusteringRTsAndBuffers.rtVariance
    );
    this.computeShader.SetBuffer(
      this.kernelGatherVariance,
      "cbuf_cluster_centers",
      clusteringRTsAndBuffers.cbufClusterCenters
    );
    this.computeShader.Dispatch(this.kernelGatherVariance, 1, 1, 1);

    using (
      ClusterCenters clusterCenters = clusteringRTsAndBuffers.GetClusterCenters()
    ) {
      return clusterCenters.variance;
    }
  }
}
}