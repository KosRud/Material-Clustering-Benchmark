using UnityEngine;

namespace ClusteringAlgorithms {

  public class DispatcherKM : ADispatcher {
    public DispatcherKM(
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

    public override string descriptionString => "KM";

    public override void RunClustering(ClusteringTextures clusteringTextures) {
      for (int i = 0; i < this.numIterations; i++) {
        this.KMiteration(clusteringTextures, rejectOld: false);
      }
    }

    /// <summary>
    /// First attributes clusters to
    /// In order to use the result, one final cluster attribution is required!
    /// </summary>
    protected void KMiteration(
      ClusteringTextures textures,
      bool rejectOld
    ) {
      this.computeShader.SetBool("do_random_sample_empty_clusters",
        this.doRandomizeEmptyClusters);
      this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

      this.AttributeClusters(
        textures,
        final: false,
        khm: false
      );
      this.UpdateClusterCenters(
        textures,
        rejectOld
      );
    }
  }
}