using UnityEngine;

namespace ClusteringAlgorithms
{
    public class DispatcherKM : ASimpleDispatcer
    {
        public DispatcherKM(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        ) : base(computeShader, numIterations, doRandomizeEmptyClusters, clusteringRTsAndBuffers)
        { }

        public override string name => "KM";

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
                this.KMiteration(clusteringTextures, rejectOld: false);
            }
        }

        /// <summary>
        /// First attributes pixels to clusters.
        /// Then updates cluster centers.
        /// In order to use the resulting cluster centers,
        /// one final cluster attribution is required!
        /// </summary>
        protected void KMiteration(ClusteringTextures textures, bool rejectOld)
        {
            this.AttributeClusters(textures, final: false, khm: false);
            this.UpdateClusterCenters(textures, rejectOld);
        }

        public override void SingleIteration(ClusteringTextures textures)
        {
            this.computeShader.SetBool(
                "do_random_sample_empty_clusters",
                this.doRandomizeEmptyClusters
            );
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.KMiteration(textures, rejectOld: false);
        }
    }
}