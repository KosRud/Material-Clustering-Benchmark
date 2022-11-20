using UnityEngine;
using static Diagnostics;

namespace ClusteringAlgorithms
{
    public class DispatcherRSfixed : ADispatcherRS
    {
        public override bool usesStopCondition => false;

        private readonly bool doReadback;

        public DispatcherRSfixed(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            bool doReadback,
            ADispatcherRS.Parameters parameters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: numIterations,
                parameters: parameters,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                useFullResTexRef: useFullResTexRef,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            Assert(
                IsNumIterationsValid(
                    iterationsKM: parameters.numIterationsKm,
                    iterations: numIterations
                ),
                "Invalid number of iterations supplied to random swap dispatcher."
            );

            this.doReadback = doReadback;
        }

        public override bool doesReadback => this.doReadback;

        public override string name => this.doesReadback ? $"{base.name} (readback)" : base.name;

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.KMiteration(clusteringTextures, rejectOld: true);

            for (int i = 1; i < this.numIterations; i += this.parameters.numIterationsKm)
            {
                this.RandomSwap(clusteringTextures);

                for (int k = 0; k < this.parameters.numIterationsKm; k++)
                {
                    this.KMiteration(clusteringTextures, rejectOld: false);
                }

                if (this.doReadback)
                {
                    this.ValidateCandidatesReadback();
                }
                else
                {
                    this.ValidateCandidatesGPU();
                }
            }
        }

        public static bool IsNumIterationsValid(int iterationsKM, int iterations)
        {
            if (iterations <= 1)
            {
                return false;
            }
            if (iterationsKM == 1)
            {
                return true;
            }

            bool result = iterations % iterationsKM == 1;

            return result;
        }
    }
}
