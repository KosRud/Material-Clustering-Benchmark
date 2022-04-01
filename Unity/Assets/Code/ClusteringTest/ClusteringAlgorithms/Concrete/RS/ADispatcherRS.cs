using UnityEngine;

public abstract class ADispatcherRS : DispatcherKM {
    public readonly int iterationsKM;

    protected readonly int kernelHandleRandomSwap;
    protected readonly int kernelHandleValidateCandidates;

    public ADispatcherRS(
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


    protected float ValidateCandidatesReadback(
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        float varianceChange = ClusterCenters.invalidVariance;

        using (
                ClusterCenters clusterCenters = clusteringRTsAndBuffers.GetClusterCenters()
            ) {
            for (int i = 0; i < this.numClusters; i++) {
                if (
                    clusterCenters.centers[i].z < ClusterCenters.invalidVariance &&
                    clusterCenters.centers[i + this.numClusters].z <
                        ClusterCenters.invalidVariance
                ) {
                    varianceChange =
                        clusterCenters.centers[i].z -
                        clusterCenters.centers[i + this.numClusters].z;
                }

                if (clusterCenters.centers[i].z < clusterCenters.centers[i + this.numClusters].z) {
                    clusterCenters.centers[i + this.numClusters] = clusterCenters.centers[i];
                } else {
                    clusterCenters.centers[i] = clusterCenters.centers[i + this.numClusters];
                }
            }
            clusteringRTsAndBuffers.SetClusterCenters(clusterCenters.centers);
        }

        if (varianceChange == ClusterCenters.invalidVariance) {
            throw new ClusterCenters.InvalidClustersException("all clusters are invalid");
        }

        return varianceChange;
    }

    protected void ValidateCandidatesGPU(
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.computeShader.SetBuffer(
            this.kernelHandleValidateCandidates,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.Dispatch(this.kernelHandleValidateCandidates, 1, 1, 1);
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