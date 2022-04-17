using UnityEngine;
using UnityEngine.Pool;

namespace ClusteringAlgorithms
{
    public class DispatcherKnecht : DispatcherKM
    {
        private const int randomInitEveryNiterations = 5;
        private const int maxKMiterations = 20;
        private int frameCounter = 0;

        public DispatcherKnecht(
            ComputeShader computeShader,
            bool doRandomizeEmptyClusters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        ) : base(computeShader, 1, doRandomizeEmptyClusters, clusteringRTsAndBuffers)
        {
            this.frameCounter = 0;
        }

        public override string name => "Knecht";

        protected override void _RunClustering(ClusteringTextures clusteringTextures)
        {
            this.frameCounter++;

            using (KMuntilConvergesResult result = this.KMuntilConverges(clusteringTextures))
            {
                if (result.converged == false || this.frameCounter == randomInitEveryNiterations)
                {
                    this.DoExploration(clusteringTextures, result);
                }

                if (this.frameCounter == randomInitEveryNiterations)
                {
                    this.frameCounter = 0;
                }
            }
        }

        private void DoExploration(
            ClusteringTextures textures,
            KMuntilConvergesResult currentResult
        )
        {
            // alters (currentResult.clusterCenters) - same array is filled with new data and re-used
            this.clusteringRTsAndBuffers.RandomizeClusterCenters();

            using (KMuntilConvergesResult newResult = this.KMuntilConverges(textures))
            {
                if (currentResult.clusterCenters.variance < newResult.clusterCenters.variance)
                {
                    this.clusteringRTsAndBuffers.SetClusterCenters(
                        currentResult.clusterCenters.centers
                    );
                }
            }
        }

        private class KMuntilConvergesResult : System.IDisposable
        {
            public ClusterCenters clusterCenters;
            public bool converged;

            public static readonly IObjectPool<KMuntilConvergesResult> pool =
                new ObjectPoolMaxAssert<KMuntilConvergesResult>(
                    createFunc: () => new KMuntilConvergesResult(),
                    maxActive: 2
                );

            private KMuntilConvergesResult() { }

            public void Dispose()
            {
                this.clusterCenters.Dispose();
                pool.Release(this);
            }

            public static KMuntilConvergesResult Get(ClusterCenters clusterCenters, bool converged)
            {
                KMuntilConvergesResult obj = pool.Get();
                obj.converged = converged;
                obj.clusterCenters = clusterCenters;

                return obj;
            }
        }

        private KMuntilConvergesResult KMuntilConverges(ClusteringTextures textures)
        {
            this.KMiteration(textures, rejectOld: false);

            ClusterCenters clusterCenters = null;
            ClusterCenters newClusterCenters = this.clusteringRTsAndBuffers.GetClusterCenters();

            for (int i = 1; i < maxKMiterations; i++)
            {
                clusterCenters?.Dispose();
                clusterCenters = newClusterCenters;

                this.KMiteration(textures, rejectOld: false);

                newClusterCenters = this.clusteringRTsAndBuffers.GetClusterCenters();

                if (
                    clusterCenters.variance - newClusterCenters.variance
                    < StopCondition.varianceChangeThreshold
                )
                {
                    clusterCenters.Dispose();
                    return KMuntilConvergesResult.Get(
                        converged: true,
                        clusterCenters: newClusterCenters
                    );
                }
            }

            clusterCenters.Dispose();
            return KMuntilConvergesResult.Get(converged: false, clusterCenters: newClusterCenters);
        }
    }
}
