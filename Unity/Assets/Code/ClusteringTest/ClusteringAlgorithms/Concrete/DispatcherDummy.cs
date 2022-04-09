using UnityEngine;

namespace ClusteringAlgorithms {

  public class DispatcherDummy : ADispatcher {
    public DispatcherDummy(
      ComputeShader computeShader,
      ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) : base(
        computeShader: computeShader,
        numIterations: 1,
        doRandomizeEmptyClusters: false,
        clusteringRTsAndBuffers: clusteringRTsAndBuffers
      ) {

    }

    public override string descriptionString => "Null";

    public override void RunClustering(ClusteringTextures clusteringTextures) {
      // do nothing
    }
  }
}