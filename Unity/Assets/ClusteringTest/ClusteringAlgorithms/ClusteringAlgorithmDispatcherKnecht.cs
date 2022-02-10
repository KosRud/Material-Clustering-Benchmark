using UnityEngine;

public class ClusteringAlgorithmDispatcherKnecht : ClusteringAlgorithmDispatcherKM {
    private readonly KMuntilConvergesResult kMuntilConvergesResult = new KMuntilConvergesResult();

    private int frameCounter;

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

        if (this.frameCounter % 5 == 0) {
            this.frameCounter = 0;
            KMuntilConvergesResult result1 = this.KMuntilConverges(
                inputTex, textureSize, clusteringRTsAndBuffers
            );

            if (result1.converged) {
                return;
            }

            Debug.Log("Knecht's KM did not converge in 20 iterations!");

            clusteringRTsAndBuffers.RandomizeClusterCenters();
            KMuntilConvergesResult result2 = this.KMuntilConverges(
                inputTex, textureSize, clusteringRTsAndBuffers
            );

            if (result1.variance < result2.variance) {
                clusteringRTsAndBuffers.clusterCenters = result1.clusterCenters;
                Debug.Log("Knech's KM: failed exploration");
            } else {
                Debug.Log("Knecht's KM: successful exploration");
            }
        }

        Vector4[] oldClusterCenters = clusteringRTsAndBuffers.clusterCenters;
        float oldVariance = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
        ).variance;

        clusteringRTsAndBuffers.RandomizeClusterCenters();

        float newVariance = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
        ).variance;

        if (oldVariance < newVariance) {
            clusteringRTsAndBuffers.clusterCenters = oldClusterCenters;
        }

    }

    private class KMuntilConvergesResult {
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

        public KMuntilConvergesResult() {

        }

        public KMuntilConvergesResult(
            float variance,
            bool converged,
            Vector4[] clusterCenters
        ) {
            this.variance = variance;
            this.converged = converged;
            this.clusterCenters = clusterCenters;
        }
    }

    private KMuntilConvergesResult KMuntilConverges(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        float newVariance = 0;
        Vector4[] newClusterCenters = null;

        for (int i = 0; i < 20; i++) {
            Vector4[] clusterCenters = clusteringRTsAndBuffers.clusterCenters;
            float variance = clusterCenters[0].w;

            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );

            newClusterCenters = clusteringRTsAndBuffers.clusterCenters;
            newVariance = clusteringRTsAndBuffers.clusterCenters[0].w;

            if (variance - newVariance < 1e-4) {
                this.kMuntilConvergesResult.variance = newVariance;
                this.kMuntilConvergesResult.converged = true;
                this.kMuntilConvergesResult.clusterCenters = newClusterCenters;
                return this.kMuntilConvergesResult;
            }
        }

        Debug.Assert(newClusterCenters != null);

        this.kMuntilConvergesResult.variance = newVariance;
        this.kMuntilConvergesResult.converged = false;
        this.kMuntilConvergesResult.clusterCenters = newClusterCenters;
        return this.kMuntilConvergesResult;
    }
}