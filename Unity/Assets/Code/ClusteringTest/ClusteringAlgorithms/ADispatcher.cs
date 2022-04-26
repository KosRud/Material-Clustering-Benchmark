using UnityEngine;

namespace ClusteringAlgorithms
{
    public abstract class ADispatcher : IDispatcher
    {
        // reported in the file
        public bool doRandomizeEmptyClusters { get; private set; }
        public int numIterations { get; private set; }
        public ClusteringRTsAndBuffers clusteringRTsAndBuffers { get; private set; }

        public abstract string name { get; }
        public virtual DispatcherParameters parameters => this._parameters;

        private readonly DispatcherParameters _parameters;

        public abstract bool usesStopCondition { get; }

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
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
        {
            this.computeShader = computeShader;
            this.kernelHandleAttributeClusters = this.computeShader.FindKernel("AttributeClusters");
            this.kernelUpdateClusterCenters = computeShader.FindKernel("UpdateClusterCenters");
            this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
            this.numIterations = numIterations;
            this.clusteringRTsAndBuffers = clusteringRTsAndBuffers;
            this._parameters = new DispatcherParameters();
        }

        public abstract void RunClustering(ClusteringTextures clusteringTextures);

        public void UpdateClusterCenters(ClusteringTextures clusteringTextures, bool rejectOld)
        {
            this.clusteringRTsAndBuffers.UpdateRandomPositions();

            this.computeShader.SetBool("reject_old", rejectOld);
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

        public void AttributeClusters(ClusteringTextures clusteringTextures, bool final, bool khm)
        {
            this.computeShader.SetBool("KHM", khm);
            this.computeShader.SetBool("final", final);

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
                clusteringTextures.size / ClusteringTest.kernelSize,
                clusteringTextures.size / ClusteringTest.kernelSize,
                1
            );

            clusteringTextures.rtArr.GenerateMips();
        }

        /// <summary>
        /// Computes variance on full-resolution input texture, without thresholding of dark pixels.
        /// </summary>
        public float GetVariance()
        {
            using (ClusterCenters backupCenters = this.clusteringRTsAndBuffers.GetClusterCenters())
            {
                /*
                  one final attribution
                  (we finished by getting cluster centers)
        
                  also ensure final=true (no threshold)
                */
                this.AttributeClusters(
                    clusteringTextures: this.clusteringRTsAndBuffers.texturesFullRes,
                    final: true,
                    khm: false
                );

                /*
                  the variance computation is delayed by 1 iteration
        
                  after updating cluster centers for the 1st time
                  we get the variance of 0 iterations
        
                  so in order to get current variance,
                  we need one more cluster center update
        
                  additionally, we want to get tha variance
                  from attribution with "final: true"
                  which disables thresholding of dark pixels
                */
                this.UpdateClusterCenters(
                    clusteringTextures: this.clusteringRTsAndBuffers.texturesFullRes,
                    rejectOld: false
                );

                using (ClusterCenters centers = this.clusteringRTsAndBuffers.GetClusterCenters())
                {
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
