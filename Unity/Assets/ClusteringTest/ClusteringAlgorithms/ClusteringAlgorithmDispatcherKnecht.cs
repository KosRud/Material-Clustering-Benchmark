using UnityEngine;

public class ClusteringAlgorithmDispatcherKnecht : ClusteringAlgorithmDispatcherKM {
    private const int randomInitEveryNiterations = 5;
    private const int maxKMiterations = 20;
    private const float varianceChangeThreshold = 1e-4f;
    private readonly Vector4[] oldClusterCenters;
    private int frameCounter = 0;

    public ClusteringAlgorithmDispatcherKnecht(
        int kernelSize, ComputeShader computeShader,
        bool doRandomizeEmptyClusters, int numClusters
    ) : base(kernelSize, computeShader, 1, doRandomizeEmptyClusters, numClusters) {
        this.frameCounter = 0;
        this.oldClusterCenters = new Vector4[numClusters * 2];
    }

    public override string descriptionString => $"Knecht";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        KMuntilConvergesResult.AssertEmpty();

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
        this.CopyClusterCenters(currentResult.clusterCenters, this.oldClusterCenters);

        // alters (currentResult.clusterCenters) - same array is filled with new data and re-used
        clusteringRTsAndBuffers.RandomizeClusterCenters();

        using (
            KMuntilConvergesResult newResult = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
            )
        ) {
            if (currentResult.variance < newResult.variance) {
                clusteringRTsAndBuffers.clusterCenters = this.oldClusterCenters;
            }
        }
    }

    private class KMuntilConvergesResult : System.IDisposable {
        private const int maxActive = 2;

        public float variance;
        public bool converged;
        public Vector4[] clusterCenters {
            get {
                if (this._clusterCenters == null) {
                    throw new System.NullReferenceException("requesting cluster centers before assigning");
                }
                return this._clusterCenters;
            }
            set => this._clusterCenters = value;
        }
        private Vector4[] _clusterCenters;

        private readonly static UnityEngine.Pool.ObjectPool<KMuntilConvergesResult> pool = new UnityEngine.Pool.ObjectPool<KMuntilConvergesResult>(
            () => new KMuntilConvergesResult()
        );

        private KMuntilConvergesResult() {

        }

        void System.IDisposable.Dispose() {
            pool.Release(this);
        }

        private static void AssertMaxActive() {
            Debug.Assert(pool.CountActive <= maxActive);
        }

        public static KMuntilConvergesResult Get() {
            AssertMaxActive();
            return pool.Get();
        }

        public static KMuntilConvergesResult Get(
            float variance,
            bool converged,
            Vector4[] clusterCenters
        ) {
            AssertMaxActive();

            KMuntilConvergesResult obj = pool.Get();
            obj.variance = variance;
            obj.converged = converged;
            obj.clusterCenters = clusterCenters;
            return obj;
        }

        public static void AssertEmpty() {
            Debug.Assert(pool.CountActive == 0);
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

        Vector4[] clusterCenters = clusteringRTsAndBuffers.clusterCenters;
        float newVariance = clusterCenters[0].z;

        for (int i = 1; i < maxKMiterations; i++) {
            float oldVariance = newVariance;

            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );

            clusterCenters = clusteringRTsAndBuffers.clusterCenters;
            newVariance = clusterCenters[0].z;

            if (oldVariance - newVariance < varianceChangeThreshold) {
                return KMuntilConvergesResult.Get(
                    variance: newVariance,
                    converged: true,
                    clusterCenters: clusterCenters
                );
            }
        }

        return KMuntilConvergesResult.Get(
            variance: newVariance,
            converged: false,
            clusterCenters: clusterCenters
        );
    }

    Vector4[] CopyClusterCenters(Vector4[] from, Vector4[] to) {
        Debug.Assert(from.Length == to.Length);
        for (int i = 0; i < from.Length; i++) {
            to[i] = from[i];
        }
        return to;
    }
}