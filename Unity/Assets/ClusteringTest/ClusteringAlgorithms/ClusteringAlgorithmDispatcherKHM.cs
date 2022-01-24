using UnityEngine;

public class ClusteringAlgorithmDispatcherKHM : AClusteringAlgorithmDispatcher {
    public ClusteringAlgorithmDispatcherKHM(
        int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) { }

    public override string descriptionString => "KHM";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.computeShader.SetBool("do_random_sample_empty_clusters", this.doRandomizeEmptyClusters);
        this.computeShader.SetInt("num_clusters", this.numClusters);

        for (int i = 0; i < this.numIterations; i++) {
            this.KHMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers
            );
        }
    }

    protected void KHMiteration(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.AttributeClusters(inputTex, clusteringRTsAndBuffers, final: false, khm: true);
        clusteringRTsAndBuffers.rtArr.GenerateMips();
        this.UpdateClusterCenters(inputTex, textureSize, clusteringRTsAndBuffers, rejectOld: false);
    }
}