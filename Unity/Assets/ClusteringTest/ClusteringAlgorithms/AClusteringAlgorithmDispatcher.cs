using UnityEngine;

public abstract class AClusteringAlgorithmDispatcher {

    // reported in the file
    public readonly int numClusters;
    public readonly bool doRandomizeEmptyClusters;
    public readonly int numIterations;

    public abstract string descriptionString {
        get;
    }

    // internal
    protected readonly int kernelSize;
    protected readonly ComputeShader computeShader;
    protected readonly int kernelHandleAttributeClusters;
    protected readonly int kernelUpdateClusterCenters;

    protected AClusteringAlgorithmDispatcher(int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters) {
        this.kernelSize = kernelSize;
        this.computeShader = computeShader;
        this.kernelHandleAttributeClusters = this.computeShader.FindKernel("AttributeClusters");
        this.kernelUpdateClusterCenters = computeShader.FindKernel("UpdateClusterCenters");
        this.numClusters = numClusters;
        this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
        this.numIterations = numIterations;
    }

    public abstract void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    );

    protected void UpdateClusterCenters(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        bool rejectOld
    ) {
        clusteringRTsAndBuffers.UpdateRandomPositions(textureSize);

        this.computeShader.SetBool("reject_old", rejectOld);
        this.computeShader.SetTexture(
            this.kernelUpdateClusterCenters,
            "tex_arr_clusters_r",
            clusteringRTsAndBuffers.rtArr
        );
        this.computeShader.SetTexture(this.kernelUpdateClusterCenters, "tex_input", inputTex);
        this.computeShader.SetBuffer(
            this.kernelUpdateClusterCenters,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.SetBuffer(
            this.kernelUpdateClusterCenters,
            "cbuf_random_positions",
            clusteringRTsAndBuffers.cbufRandomPositions
        );
        this.computeShader.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
    }

    public void AttributeClusters(
        Texture inputTex,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        bool final,
        bool khm
    ) {
        this.computeShader.SetBool("KHM", khm);
        this.computeShader.SetBool("final", final);

        this.computeShader.SetTexture(
            this.kernelHandleAttributeClusters,
            "tex_input",
            inputTex
        );
        this.computeShader.SetTexture(
            this.kernelHandleAttributeClusters,
            "tex_arr_clusters_rw",
            clusteringRTsAndBuffers.rtArr
        );
        this.computeShader.SetBuffer(
            this.kernelHandleAttributeClusters,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.Dispatch(
            this.kernelHandleAttributeClusters,
            inputTex.width / this.kernelSize,
            inputTex.height / this.kernelSize,
            1
        );
    }
}
