using UnityEngine;

public class ClusteringAlgorithmDispatcherKnecht : ClusteringAlgorithmDispatcherKM {
    private readonly int kernelHandleRandomSwap;
    private readonly int kernelHandleValidateCandidates;

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
        public readonly float variance;
        public readonly bool converged;
        public readonly Vector4[] clusterCenters;

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
                return new KMuntilConvergesResult(newVariance, true, newClusterCenters);
            }
        }

        Debug.Assert(newClusterCenters != null);

        return new KMuntilConvergesResult(newVariance, false, newClusterCenters);
    }
}