using UnityEngine;

public abstract class ACladRS : CladKM {
    public readonly int iterationsKM;

    protected readonly int kernelHandleRandomSwap;
    protected readonly int kernelHandleValidateCandidates;

    public ACladRS(
        int kernelSize, ComputeShader computeShader, int numIterations,
        bool doRandomizeEmptyClusters, int numClusters, int numIterationsKM
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) {
        this.iterationsKM = numIterationsKM;
        this.kernelHandleRandomSwap = this.computeShader.FindKernel("RandomSwap");
        this.kernelHandleValidateCandidates = this.computeShader.FindKernel("ValidateCandidates");
    }

    public override abstract void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    );

    protected enum ClusterValidationResult {
        Unknown,
        Improved,
        NotImproved
    }

    protected ClusterValidationResult ValidateCandidatesReadback(
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        bool success = false;

        using (
                ClusterCenters clusterCenters = clusteringRTsAndBuffers.GetClusterCenters()
            ) {
            for (int i = 0; i < this.numClusters; i++) {
                if (clusterCenters.centers[i].z < clusterCenters.centers[i + this.numClusters].z) {
                    clusterCenters.centers[i + this.numClusters] = clusterCenters.centers[i];

                    success = true;
                } else {
                    clusterCenters.centers[i] = clusterCenters.centers[i + this.numClusters];
                }
            }
            clusteringRTsAndBuffers.SetClusterCenters(clusterCenters.centers);
        }

        if (success) {
            return ClusterValidationResult.Improved;
        } else {
            return ClusterValidationResult.NotImproved;
        }
    }

    protected ClusterValidationResult ValidateCandidatesGPU(
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.computeShader.SetBuffer(
            this.kernelHandleValidateCandidates,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.Dispatch(this.kernelHandleValidateCandidates, 1, 1, 1);

        return ClusterValidationResult.Unknown;
    }

    protected void RandomSwap(Texture inputTex, int textureSize, ClusteringRTsAndBuffers clusteringRTsAndBuffers) {
        clusteringRTsAndBuffers.UpdateRandomPositions(textureSize);

        this.computeShader.SetBuffer(
            this.kernelHandleRandomSwap,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.SetBuffer(
            this.kernelHandleRandomSwap,
            "cbuf_random_positions",
            clusteringRTsAndBuffers.cbufRandomPositions
        );
        this.computeShader.SetTexture(this.kernelHandleRandomSwap, "tex_input", inputTex);
        this.computeShader.SetInt(
            "randomClusterCenter",
            clusteringRTsAndBuffers.PickRandomCluster(this.numClusters)
        );
        this.computeShader.Dispatch(this.kernelHandleRandomSwap, 1, 1, 1);
    }

    public static bool IsNumIterationsValid(int iterationsKM, int iterations) {
        if (iterations <= 1) {
            return false;
        }
        if (iterationsKM == 1) {
            return true;
        }
        return iterations % iterationsKM == 1;
    }
}