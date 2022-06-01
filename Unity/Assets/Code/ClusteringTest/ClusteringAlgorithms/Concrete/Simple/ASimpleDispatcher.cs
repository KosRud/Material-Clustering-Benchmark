using UnityEngine;

namespace ClusteringAlgorithms
{
    public abstract class ASimpleDispatcer : ADispatcher
    {
        public ASimpleDispatcer(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: numIterations,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                useFullResTexRef: useFullResTexRef,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            ) { }

        public abstract void SingleIteration(ClusteringTextures textures);

        public override bool usesStopCondition => false;
    }
}
