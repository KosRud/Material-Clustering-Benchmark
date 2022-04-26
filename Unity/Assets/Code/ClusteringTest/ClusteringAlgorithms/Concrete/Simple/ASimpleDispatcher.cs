using UnityEngine;

namespace ClusteringAlgorithms
{
    public abstract class ASimpleDispatcer : ADispatcher
    {
        public ASimpleDispatcer(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        ) : base(computeShader, numIterations, doRandomizeEmptyClusters, clusteringRTsAndBuffers)
        { }

        public abstract void SingleIteration(ClusteringTextures textures);
    }
}
