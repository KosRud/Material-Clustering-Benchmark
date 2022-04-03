using UnityEngine;

namespace ClusteringAlgorithms {

public class DispatcherDummy : ADispatcher {
  public DispatcherDummy(ComputeShader computeShader, int numClusters) : base(
      kernelSize: 4,
      computeShader: computeShader,
      numIterations: 1,
      doRandomizeEmptyClusters: false,
      numClusters: numClusters) {

  }

  public override string descriptionString => "Null";

  public override void RunClustering(
    Texture inputTex,
    int textureSize,
    ClusteringRTsAndBuffers clusteringRTsAndBuffers
  ) {
    // do nothing
  }
}
}