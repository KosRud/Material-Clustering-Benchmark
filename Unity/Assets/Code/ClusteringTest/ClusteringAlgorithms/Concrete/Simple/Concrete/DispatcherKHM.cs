using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public class DispatcherKHM : ASimpleDispatcer
    {
        [Serializable]
        public class Parameters : DispatcherParameters
        {
            public int p;

            public Parameters(int p)
            {
                this.p = p;
            }
        }

        public override bool doesReadback => false;

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

        /// <summary>
        /// Each iteration first attributes pixels to clusters,
        /// then updates cluster centers.
        /// In order to use the resulting cluster centers,
        /// one final cluster attribution is required!
        /// </summary>
        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            for (int i = 0; i < this.numIterations; i++)
            {
                this.KHMiteration(clusteringTextures);
            }
        }

        /// <summary>
        /// First attributes pixels to clusters.
        /// Then updates cluster centers.
        /// In order to use the resulting cluster centers,
        /// one final cluster attribution is required!
        /// </summary>
        protected void KHMiteration(ClusteringTextures textures)
        {
            this.AttributeClusters(textures, final: false, khm: true);
            this.UpdateClusterCenters(textures, rejectOld: false);
        }

        public override void SingleIteration(ClusteringTextures textures)
        {
            this.computeShader.SetBool(
                "do_random_sample_empty_clusters",
                this.doRandomizeEmptyClusters
            );

            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.KHMiteration(textures);
        }
    }
}
