using UnityEngine;

namespace ClusteringAlgorithms {

  public class DispatcherRSstopCondition : ADispatcherRS {
    private readonly int maxFailedSwaps;

    public DispatcherRSstopCondition(
      int kernelSize, ComputeShader computeShader,
      bool doRandomizeEmptyClusters, int numClusters,
      int numIterationsKM, int maxFailedSwaps
    ) : base(
        kernelSize: kernelSize,
        computeShader: computeShader,
        numIterations: 1,
        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
        numClusters: numClusters,
        numIterationsKM: numIterationsKM
      ) {
      this.maxFailedSwaps = maxFailedSwaps;
    }

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

          if (failedSwaps == this.maxFailedSwaps) {
            return;
          }
        } else if (-varianceChange < DispatcherKnecht.varianceChangeThreshold) {
          return;
        } else {
          failedSwaps = 0;
        }
      }
    }
  }
}