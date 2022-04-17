using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public class DispatcherKHM : ADispatcher
    {
        [Serializable]
        public class Parameters : DispatcherParameters
        {
            public readonly int p;

            public Parameters(int p)
            {
                this.p = p;
            }
        }

        private readonly Parameters _parameters;
        public override DispatcherParameters parameters => this._parameters;

        public DispatcherKHM(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        ) : base(computeShader, numIterations, doRandomizeEmptyClusters, clusteringRTsAndBuffers)
        {
            this._parameters = new Parameters(3); // hard-coded in shader
        }

        public override string name => "KHM";

        protected override void _RunClustering(ClusteringTextures clusteringTextures)
        {
            this.computeShader.SetBool(
                "do_random_sample_empty_clusters",
                this.doRandomizeEmptyClusters
            );
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            for (int i = 0; i < this.numIterations; i++)
            {
                this.KHMiteration(clusteringTextures);
            }
        }

        protected void KHMiteration(ClusteringTextures textures)
        {
            this.AttributeClusters(textures, final: false, khm: true);
            this.UpdateClusterCenters(textures, rejectOld: false);
        }
    }
}
