using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public abstract class ADispatcher : IDispatcher
    {
        // reported in the file
        public bool doRandomizeEmptyClusters { get; private set; }
        public int numIterations { get; private set; }
        public ClusteringRTsAndBuffers clusteringRTsAndBuffers { get; private set; }

        public abstract string name { get; }
        private DispatcherParameters parameters;
        public virtual DispatcherParameters abstractParameters => this.parameters;

        public abstract bool usesStopCondition { get; }
        public abstract bool doesReadback { get; }

        private int _warningCounter;
        public int warningCounter
        {
            get => _warningCounter;
            private set { _warningCounter = value; }
        }

        // internal
        public readonly ComputeShader computeShader;
        private readonly int kernelHandleAttributeClusters;
        private readonly int kernelUpdateClusterCenters;

        /// <summary>
        /// Takes ownership of the clusteringRTsAndBuffers
        /// </summary>
        protected ADispatcher(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
        {
            this.computeShader = computeShader;
            this.kernelHandleAttributeClusters = this.computeShader.FindKernel("AttributeClusters");
            this.kernelUpdateClusterCenters = computeShader.FindKernel("UpdateClusterCenters");
            this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
            this.numIterations = numIterations;
            this.clusteringRTsAndBuffers = clusteringRTsAndBuffers;
            this.useFullResTexRef = useFullResTexRef;
            this.warningCounter = 0;
        }

        public abstract void RunClustering(ClusteringTextures clusteringTextures);

        public void UpdateClusterCenters(ClusteringTextures clusteringTextures, bool rejectOld)
        {
            this.clusteringRTsAndBuffers.UpdateRandomPositions();

            this.computeShader.SetBool("reject_old", rejectOld);
            this.computeShader.SetInt("mip_level", clusteringTextures.mipLevel);
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.computeShader.SetTexture(
                this.kernelUpdateClusterCenters,
                "tex_arr_clusters_r",
                clusteringTextures.rtArr
            );
            this.computeShader.SetTexture(
                this.kernelUpdateClusterCenters,
                "tex_input",
                clusteringTextures.rtInput
            );
            this.computeShader.SetBuffer(
                this.kernelUpdateClusterCenters,
                "cbuf_cluster_centers",
                this.clusteringRTsAndBuffers.cbufClusterCenters
            );
            this.computeShader.SetBuffer(
                this.kernelUpdateClusterCenters,
                "cbuf_random_positions",
                this.clusteringRTsAndBuffers.cbufRandomPositions
            );

            this.computeShader.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
        }

        public void AttributeClustersKM(ClusteringTextures clusteringTextures)
        {
            this.computeShader.SetBool("KHM", false);
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.computeShader.SetTexture(
                this.kernelHandleAttributeClusters,
                "tex_input",
                clusteringTextures.rtInput
            );
            this.computeShader.SetTexture(
                this.kernelHandleAttributeClusters,
                "tex_arr_clusters_rw",
                clusteringTextures.rtArr
            );
            this.computeShader.SetBuffer(
                this.kernelHandleAttributeClusters,
                "cbuf_cluster_centers",
                this.clusteringRTsAndBuffers.cbufClusterCenters
            );

            this.computeShader.Dispatch(
                this.kernelHandleAttributeClusters,
                Math.Max(clusteringTextures.size / ClusteringTest.kernelSize, 1),
                Math.Max(clusteringTextures.size / ClusteringTest.kernelSize, 1),
                1
            );

            clusteringTextures.rtArr.GenerateMips();
        }

        public void AttributeClustersKHM(ClusteringTextures clusteringTextures, float p)
        {
            this.computeShader.SetBool("KHM", true);
            this.computeShader.SetFloat("p", p);
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.computeShader.SetTexture(
                this.kernelHandleAttributeClusters,
                "tex_input",
                clusteringTextures.rtInput
            );
            this.computeShader.SetTexture(
                this.kernelHandleAttributeClusters,
                "tex_arr_clusters_rw",
                clusteringTextures.rtArr
            );
            this.computeShader.SetBuffer(
                this.kernelHandleAttributeClusters,
                "cbuf_cluster_centers",
                this.clusteringRTsAndBuffers.cbufClusterCenters
            );

            this.computeShader.Dispatch(
                this.kernelHandleAttributeClusters,
                Math.Max(clusteringTextures.size / ClusteringTest.kernelSize, 1),
                Math.Max(clusteringTextures.size / ClusteringTest.kernelSize, 1),
                1
            );

            clusteringTextures.rtArr.GenerateMips();
        }

        public bool useFullResTexRef { get; private set; }

        public float? GetVariance()
        {
            using (ClusterCenters backupCenters = this.clusteringRTsAndBuffers.GetClusterCenters())
            {
                if (backupCenters.warning)
                {
                    this.warningCounter++;
                }

                // We need to perform cluster centers update to get variance. However, we don't want to actually update cluster centers as it would interfere with the benchmarks. So we backup the current cluster centers and restore them afterwards.
                ClusteringTextures refTextures = this.useFullResTexRef switch
                {
                    true => this.clusteringRTsAndBuffers.texturesFullRes,
                    false => this.clusteringRTsAndBuffers.texturesWorkRes
                };

                // Attribution to ensure cluster memberships match current cluster centers. It is techinically unnecessary if attribution was ran after the latest update of cluster centers, the algorithm used was KM, and the variance is computed on working resolution texture. For simplicity, we do not make a special case and always perform attribution.
                this.AttributeClustersKM(clusteringTextures: refTextures);

                this.UpdateClusterCenters(clusteringTextures: refTextures, rejectOld: false);

                using (ClusterCenters centers = this.clusteringRTsAndBuffers.GetClusterCenters())
                {
                    // Restore original cluster centers as to not interfere with the benchmark.
                    this.clusteringRTsAndBuffers.SetClusterCenters(backupCenters.centers);

                    return centers.variance;
                }
            }
        }

        public virtual void Dispose()
        {
            this.clusteringRTsAndBuffers.Dispose();
        }
    }
}
