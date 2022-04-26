using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public class DispatcherRSfixed : ADispatcherRS
    {
        [Serializable]
        public new class Parameters : ADispatcherRS.Parameters
        {
            public bool doReadback;

            public Parameters(int numIterationsKm, bool stopCondition, bool doReadback)
                : base(numIterationsKm: numIterationsKm, stopCondition: stopCondition)
            {
                this.doReadback = doReadback;
            }
        }

        protected new Parameters _parameters;
        public override DispatcherParameters parameters => this._parameters;

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
                numIterationsKm: numIterationsKM,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            Debug.Assert(
                IsNumIterationsValid(iterationsKM: numIterationsKM, iterations: numIterations)
            );

            this._parameters = new Parameters(
                numIterationsKM,
                stopCondition: true,
                doReadback: doReadback
            );
        }

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.KMiteration(clusteringTextures, rejectOld: true);

            for (int i = 1; i < this.numIterations; i += this._parameters.numIterationsKm)
            {
                this.RandomSwap(clusteringTextures);

                for (int k = 0; k < this._parameters.numIterationsKm; k++)
                {
                    this.KMiteration(clusteringTextures, rejectOld: false);
                }
                if (this._parameters.doReadback)
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
