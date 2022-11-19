using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public class DispatcherKHM : ASimpleDispatcer
    {
        [Serializable]
        public class Parameters : DispatcherParameters
        {
            public float p;

            public Parameters(float p)
            {
                this.p = p;
            }

            public static Parameters Default()
            {
                return new Parameters(p: 3);
            }
        }

        public override bool doesReadback => false;
        public override DispatcherParameters abstractParameters => this.parameters;

        private readonly Parameters parameters;

        public DispatcherKHM(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            Parameters parameters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: numIterations,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                useFullResTexRef: useFullResTexRef,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            this.parameters = parameters; // hard-coded in shader
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
                Debug.Log(this.GetVariance());
            }
            Debug.Log("----------");
        }

        /// <summary>
        /// First attributes pixels to clusters.
        /// Then updates cluster centers.
        /// In order to use the resulting cluster centers,
        /// one final cluster attribution is required!
        /// </summary>
        protected void KHMiteration(ClusteringTextures textures)
        {
            this.AttributeClustersKHM(textures, p: this.parameters.p);
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
