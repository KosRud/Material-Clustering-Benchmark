using UnityEngine;

public class ClusteringAlgorithmDispatcherKnecht : ClusteringAlgorithmDispatcherKM {
    private const int randomInitEveryNiterations = 5;
    private const int maxKMiterations = 20;
    private const float varianceChangeThreshold = 1e-4f;

    private readonly KMuntilConvergesResult kMuntilConvergesResult = new KMuntilConvergesResult();
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

        if (this.frameCounter == randomInitEveryNiterations) {
            this.frameCounter = 0;

            this.KnechtExplorationIteration(inputTex, textureSize, clusteringRTsAndBuffers);
        } else {
            this.KnechtNormalIteration(inputTex, textureSize, clusteringRTsAndBuffers);
        }
    }

    private void KnechtNormalIteration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        KMuntilConvergesResult result = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
        );

        if (result.converged) {
            return;
        } else {
            this.DoExploration(inputTex, textureSize, clusteringRTsAndBuffers, result);
            Debug.Log("did not converge");
        }
    }

    private void KnechtExplorationIteration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        KMuntilConvergesResult result = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
        );

        this.DoExploration(inputTex, textureSize, clusteringRTsAndBuffers, result);
    }

    private void DoExploration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        KMuntilConvergesResult currentResult
    ) {
        clusteringRTsAndBuffers.RandomizeClusterCenters();

        KMuntilConvergesResult newResult = this.KMuntilConverges(
            inputTex, textureSize, clusteringRTsAndBuffers
        );

        if (currentResult.variance < newResult.variance) {
            clusteringRTsAndBuffers.clusterCenters = currentResult.clusterCenters;
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

        for (int i = 0; i < maxKMiterations; i++) {
            Vector4[] clusterCenters = clusteringRTsAndBuffers.clusterCenters;
            float variance = clusterCenters[0].z;

            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );

            newClusterCenters = clusteringRTsAndBuffers.clusterCenters;
            newVariance = clusteringRTsAndBuffers.clusterCenters[0].z;

            if (variance - newVariance < varianceChangeThreshold) {
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