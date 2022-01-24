using UnityEngine;

public class ClusteringAlgorithmDispatcherKM : AClusteringAlgorithmDispatcher {
    private readonly int kernelUpdateClusterCenters;

    public ClusteringAlgorithmDispatcherKM(
        int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) {
        this.kernelUpdateClusterCenters = computeShader.FindKernel("UpdateClusterCenters");
    }

    public override string descriptionString => "KM";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.computeShader.SetBool("do_random_sample_empty_clusters", this.doRandomizeEmptyClusters);
        this.computeShader.SetInt("num_clusters", this.numClusters);
        this.computeShader.SetBool("KHM", false);

        for (int i = 0; i < this.numIterations; i++) {
            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );
        }
    }

    protected void KMiteration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        bool rejectOld
    ) {
        this.computeShader.SetBool(
            "do_random_sample_empty_clusters",
            this.doRandomizeEmptyClusters
        );
        this.computeShader.SetInt("num_clusters", this.numClusters);

        this.AttributeClusters(inputTex, clusteringRTsAndBuffers);
        clusteringRTsAndBuffers.rtArr.GenerateMips();
        this.UpdateClusterCenters(inputTex, textureSize, clusteringRTsAndBuffers, rejectOld);
    }

    private void UpdateClusterCenters(
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
}