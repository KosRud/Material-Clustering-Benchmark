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

			/// <summary>
			/// Default value of <see cref="Parameters.p"/> is 2.5, which we experimentally confirmed to be optimal for our dataset (<see cref="WorkGeneration.KHMp"/>). For values above p=3.0 we saw a sharp decline in quality for one of the video files.<para />
			/// Integer powers are optimized by the compiler to use multiplication instead, which is faster. For non-integer powers <c>pow(a,b)</c> will be replaced with <c>exp2(b * log2(a))</c>, because there is no hardware <c>pow()</c>.<para />
			/// According to [1]: p=3.5 is the optimal value for 2-dimensional data in a general case; p=3 produces almost as good clustering quality as p=3.5; any value above p=2 should generally outperform K-means; for higher dimensions values of p snould be higher.<para />
			/// In our application the samples are located on the edges of a triangle in two-dimensional space, which could explain lower value of p being optimal.<para />
			/// [1] Zhang, B., 2000. Generalized k-harmonic means--boosting in unsupervised learning. HP LABORATORIES TECHNICAL REPORT HPL, 137.
			/// </summary>
			/// <returns></returns>
            public static Parameters Default()
            {
                return new Parameters(p: 2.5f);
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
