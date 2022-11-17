using UnityEngine;

namespace ClusteringAlgorithms
{
    public class DispatcherRSstopCondition : ADispatcherRS
    {
        public DispatcherRSstopCondition(
            ComputeShader computeShader,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            int numIterationsKM,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: 1,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                useFullResTexRef: useFullResTexRef,
                numIterationsKm: numIterationsKM,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            ) { }

        // stop condition impossible without readback
        public override bool doesReadback => true;

        public override bool usesStopCondition => true;

        public override string name => $"{base.name} (readback)";

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.KMiteration(clusteringTextures, rejectOld: true);

            int numFailedSwaps = 0;

            for (int i = 1; ; i += this._parameters.numIterationsKm)
            {
                this.RandomSwap(clusteringTextures);

                for (int k = 0; k < this._parameters.numIterationsKm; k++)
                {
                    this.KMiteration(clusteringTextures, rejectOld: false);
                }

                using (
                    ADispatcherRS.RandomSwapResult randomSwapResult =
                        this.ValidateCandidatesReadback()
                )
                {
                    switch (randomSwapResult.stopConditionOverride)
                    {
                        case RandomSwapResult.StopConditionOverride.Stop:
                            return;
                        case RandomSwapResult.StopConditionOverride.KeepRunning:
                            continue;
                        case RandomSwapResult.StopConditionOverride.Default:
                            if (randomSwapResult.swapFailed)
                            {
                                // failed swap

                                numFailedSwaps++;
                                if (numFailedSwaps > StopCondition.maxFailedSwaps)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                // successful swap

                                if (
                                    randomSwapResult.varianceReduction
                                    < StopCondition.varianceChangeThreshold
                                )
                                {
                                    return;
                                }
                            }

                            continue;
                    }
                }
            }
        }
    }
}
