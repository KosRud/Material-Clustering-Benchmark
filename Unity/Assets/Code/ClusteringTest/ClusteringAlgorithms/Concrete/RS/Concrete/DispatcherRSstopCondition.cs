using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public class DispatcherRSstopCondition : ADispatcherRS
    {
        public DispatcherRSstopCondition(
            ComputeShader computeShader,
            bool doRandomizeEmptyClusters,
            int numIterationsKM,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: 1,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                numIterationsKM: numIterationsKM,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            this._parameters = new ADispatcherRS.Parameters(
                numIterationsKM: numIterationsKM,
                stopCondition: true
            );
        }

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.KMiteration(clusteringTextures, rejectOld: true);

            int failedSwaps = 0;

            for (int i = 1; ; i += this._parameters.numIterationsKM)
            {
                this.RandomSwap(clusteringTextures);

                for (int k = 0; k < this._parameters.numIterationsKM; k++)
                {
                    this.KMiteration(clusteringTextures, rejectOld: false);
                }

                float varianceChange = this.ValidateCandidatesReadback();

                if (varianceChange > 0)
                {
                    failedSwaps++;

                    if (failedSwaps == StopCondition.maxConsecutiveFailedSwaps)
                    {
                        return;
                    }
                }
                else if (-varianceChange < StopCondition.varianceChangeThreshold)
                {
                    return;
                }
                else
                {
                    failedSwaps = 0;
                }
            }
        }
    }
}
