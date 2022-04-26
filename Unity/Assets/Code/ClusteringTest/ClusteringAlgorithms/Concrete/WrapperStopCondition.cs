using UnityEngine;

namespace ClusteringAlgorithms
{
    public class WrapperStopCondition : ADispatcher
    {
        private readonly ASimpleDispatcer wrappedDispatcher;

        public WrapperStopCondition(ASimpleDispatcer wrappedDispatcher)
            : base(
                computeShader: wrappedDispatcher.computeShader,
                numIterations: wrappedDispatcher.numIterations,
                doRandomizeEmptyClusters: wrappedDispatcher.doRandomizeEmptyClusters,
                clusteringRTsAndBuffers: wrappedDispatcher.clusteringRTsAndBuffers
            )
        {
            this.wrappedDispatcher = wrappedDispatcher;
        }

        public override string name => this.wrappedDispatcher.name;

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            // ToDo actual implementation
            this.wrappedDispatcher.RunClustering(clusteringTextures);
            throw new System.NotImplementedException();
        }
    }
}
