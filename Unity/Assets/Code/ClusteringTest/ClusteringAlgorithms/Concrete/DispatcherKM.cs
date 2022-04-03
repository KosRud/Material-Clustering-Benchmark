using UnityEngine;

namespace ClusteringAlgorithms {

public class DispatcherKM : ADispatcher {
  public DispatcherKM(
    int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters
  ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) { }

  public override string descriptionString => "KM";

  public override void RunClustering(
    Texture inputTex,
    int textureSize,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers
  ) {
    for (int i = 0; i < this.numIterations; i++) {
      this.KMiteration(
        inputTex, textureSize, clusteringRTsAndBuffers,
        rejectOld: false
      );
    }
  }

  /// <summary>
  /// First attributes clusters to
  /// In order to use the result, one final cluster attribution is required!
  /// </summary>
  protected void KMiteration(
    Texture inputTex,
    int textureSize,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers,
    bool rejectOld
  ) {
    this.computeShader.SetBool("do_random_sample_empty_clusters", this.doRandomizeEmptyClusters);
    this.computeShader.SetInt("num_clusters", this.numClusters);

    this.AttributeClusters(inputTex, clusteringRTsAndBuffers, final: false, khm: false);
    this.UpdateClusterCenters(inputTex, textureSize, clusteringRTsAndBuffers, rejectOld);
  }
}
}