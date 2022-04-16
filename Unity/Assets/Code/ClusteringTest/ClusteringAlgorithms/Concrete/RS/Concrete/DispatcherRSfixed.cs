using UnityEngine;

namespace ClusteringAlgorithms
{
    public class DispatcherRSfixed : ADispatcherRS
    {
        public readonly bool doReadback;

        public DispatcherRSfixed(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            int numIterationsKM,
            bool doReadback,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: numIterations,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                numIterationsKM: numIterationsKM,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            Debug.Assert(
                IsNumIterationsValid(iterationsKM: numIterationsKM, iterations: numIterations)
            );
            this.doReadback = doReadback;
        }

        public override string descriptionString
        {
            get
            {
                string result = $"RS({this.iterationsKM}KM)";
                if (this.doReadback)
                {
                    result += "_readback";
                }
                return result;
            }
        }

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.KMiteration(clusteringTextures, rejectOld: true);

            for (int i = 1; i < this.numIterations; i += this.iterationsKM)
            {
                this.RandomSwap(clusteringTextures);

                for (int k = 0; k < this.iterationsKM; k++)
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
