using UnityEngine;

namespace ClusteringAlgorithms {

  public class DispatcherRSstopCondition : ADispatcherRS {
    public DispatcherRSstopCondition(
      int kernelSize, ComputeShader computeShader,
      bool doRandomizeEmptyClusters, int numClusters,
      int numIterationsKM
    ) : base(
        kernelSize: kernelSize,
        computeShader: computeShader,
        numIterations: 1,
        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
        numClusters: numClusters,
        numIterationsKM: numIterationsKM
      ) { }

    public override string descriptionString => $"RS({this.iterationsKM}KM)_stop";

    public override void RunClustering(
      Texture inputTex,
      int textureSize,
      ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
      this.KMiteration(
        inputTex, textureSize, clusteringRTsAndBuffers,
        rejectOld: true
      );

      int failedSwaps = 0;

      for (int i = 1; ; i += this.iterationsKM) {
        this.RandomSwap(inputTex, textureSize, clusteringRTsAndBuffers);

        for (int k = 0; k < this.iterationsKM; k++) {
          this.KMiteration(
            inputTex, textureSize, clusteringRTsAndBuffers,
            rejectOld: false
          );
        }

        float varianceChange = this.ValidateCandidatesReadback(clusteringRTsAndBuffers);

        if (varianceChange > 0) {
          failedSwaps++;

          if (failedSwaps == StopCondition.maxConsecutiveFailedSwaps) {
            return;
          }
        } else if (-varianceChange < StopCondition.varianceChangeThreshold) {
          return;
        } else {
          failedSwaps = 0;
        }
      }
    }
  }
}