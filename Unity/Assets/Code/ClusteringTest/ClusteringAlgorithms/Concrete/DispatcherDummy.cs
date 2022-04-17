using UnityEngine;

namespace ClusteringAlgorithms
{
    public class DispatcherDummy : ADispatcher
    {
        public DispatcherDummy(
            ComputeShader computeShader,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: 1,
                doRandomizeEmptyClusters: false,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            ) { }

        public override string name => "Null";

        protected override void _RunClustering(ClusteringTextures clusteringTextures)
        {
            // do nothing
        }
    }
}
