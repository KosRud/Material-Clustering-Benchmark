using UnityEngine;

namespace ClusteringAlgorithms {

  public class DispatcherKHM : ADispatcher {
    public DispatcherKHM(
      ComputeShader computeShader,
      int numIterations,
      bool doRandomizeEmptyClusters,
      ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) : base(
        computeShader,
        numIterations,
        doRandomizeEmptyClusters,
        clusteringRTsAndBuffers
      ) { }

    public override string descriptionString => "KHM(3)";

    public override void RunClustering(
      ClusteringTextures clusteringTextures
    ) {
      this.computeShader.SetBool("do_random_sample_empty_clusters",
        this.doRandomizeEmptyClusters);
      this.computeShader.SetInt("num_clusters",
        this.clusteringRTsAndBuffers.numClusters);

      for (int i = 0; i < this.numIterations; i++) {
        this.KHMiteration(clusteringTextures);
      }
    }

    protected void KHMiteration(ClusteringTextures textures) {
      this.AttributeClusters(
        textures,
        final: false,
        khm: true
      );
      this.UpdateClusterCenters(
        textures,
        rejectOld: false
      );
    }
  }
}