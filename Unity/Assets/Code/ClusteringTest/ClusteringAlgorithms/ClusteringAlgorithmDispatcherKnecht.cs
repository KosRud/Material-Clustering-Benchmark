using UnityEngine;

public class ClusteringAlgorithmDispatcherKnecht : ClusteringAlgorithmDispatcherKM {
    private const int randomInitEveryNiterations = 5;
    private const int maxKMiterations = 20;
    private const float varianceChangeThreshold = 1e-4f;
    private int frameCounter = 0;

    public ClusteringAlgorithmDispatcherKnecht(
        int kernelSize, ComputeShader computeShader,
        bool doRandomizeEmptyClusters, int numClusters
    ) : base(kernelSize, computeShader, 1, doRandomizeEmptyClusters, numClusters) {
        this.frameCounter = 0;
    }

    public override string descriptionString => $"Knecht";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.frameCounter++;

        using (
            KMuntilConvergesResult result = this.KMuntilConverges(inputTex, textureSize, clusteringRTsAndBuffers)
        ) {
            if (
                result.converged == false ||
                this.frameCounter == randomInitEveryNiterations
            ) {
                this.DoExploration(inputTex, textureSize, clusteringRTsAndBuffers, result);
            }

            if (this.frameCounter == randomInitEveryNiterations) {
                this.frameCounter = 0;
            }
        }
    }

    private void DoExploration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        KMuntilConvergesResult currentResult
    ) {
        // alters (currentResult.clusterCenters) - same array is filled with new data and re-used
        clusteringRTsAndBuffers.RandomizeClusterCenters();

        using (
            KMuntilConvergesResult newResult = this.KMuntilConverges(
                inputTex, textureSize, clusteringRTsAndBuffers
            )
        ) {
            if (currentResult.clusterCenters.variance < newResult.clusterCenters.variance) {
                clusteringRTsAndBuffers.SetClusterCenters(currentResult.clusterCenters.centers);
            }
        }
    }

    private class KMuntilConvergesResult : System.IDisposable {
        public ClusterCenters clusterCenters;
        public bool converged;

        public static readonly UnityEngine.Pool.IObjectPool<KMuntilConvergesResult> pool =
        new ObjectPoolMaxAssert<KMuntilConvergesResult>(
            createFunc: () => new KMuntilConvergesResult(),
            maxActive: 2
        );

        private KMuntilConvergesResult() {

        }

        void System.IDisposable.Dispose() {
            ((System.IDisposable)this.clusterCenters).Dispose();
            pool.Release(this);
        }

        public static KMuntilConvergesResult Get(
            ClusterCenters clusterCenters,
            bool converged
        ) {
            KMuntilConvergesResult obj = pool.Get();
            obj.converged = converged;
            obj.clusterCenters = clusterCenters;

            return obj;
        }
    }

    private KMuntilConvergesResult KMuntilConverges(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.KMiteration(
            inputTex, textureSize, clusteringRTsAndBuffers,
            rejectOld: false
        );

        ClusterCenters clusterCenters = null;
        ClusterCenters newClusterCenters = clusteringRTsAndBuffers.GetClusterCenters();

        for (int i = 1; i < maxKMiterations; i++) {
            ((System.IDisposable)clusterCenters)?.Dispose();
            clusterCenters = newClusterCenters;

            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );

            newClusterCenters = clusteringRTsAndBuffers.GetClusterCenters();

            if (clusterCenters.variance - newClusterCenters.variance < varianceChangeThreshold) {
                ((System.IDisposable)clusterCenters).Dispose();
                return KMuntilConvergesResult.Get(
                    converged: true,
                    clusterCenters: newClusterCenters
                );
            }
        }

        ((System.IDisposable)clusterCenters).Dispose();
        return KMuntilConvergesResult.Get(
            converged: false,
            clusterCenters: newClusterCenters
        );
    }
}