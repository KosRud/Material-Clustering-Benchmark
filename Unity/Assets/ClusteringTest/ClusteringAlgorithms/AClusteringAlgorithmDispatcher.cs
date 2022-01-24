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
    protected int kernelHandleAttributeClusters;

    protected AClusteringAlgorithmDispatcher(int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters) {
        this.kernelSize = kernelSize;
        this.computeShader = computeShader;
        this.kernelHandleAttributeClusters = this.computeShader.FindKernel("AttributeClusters");
        this.numClusters = numClusters;
        this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
        this.numIterations = numIterations;
    }

    public abstract void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    );

    public void AttributeClusters(
        Texture inputTex,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        bool final = false
    ) {
        this.computeShader.SetBool("final", final);  // replace with define
        this.computeShader.SetTexture(
            this.kernelHandleAttributeClusters,
            "tex_input",
            inputTex
        );
        this.computeShader.SetTexture(
            this.kernelHandleAttributeClusters,
            "tex_variance",
            clusteringRTsAndBuffers.rtVariance
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
