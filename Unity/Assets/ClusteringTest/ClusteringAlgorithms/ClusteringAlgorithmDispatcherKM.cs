using UnityEngine;

public class ClusteringAlgorithmDispatcherKM : AClusteringAlgorithmDispatcher {
    public ClusteringAlgorithmDispatcherKM(
        int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters, int numClusters
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) { }

    public override string descriptionString => "KM";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
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
        this.computeShader.SetBool("do_random_sample_empty_clusters", this.doRandomizeEmptyClusters);
        this.computeShader.SetInt("num_clusters", this.numClusters);

        this.AttributeClusters(inputTex, clusteringRTsAndBuffers, final: false, khm: false);
        this.UpdateClusterCenters(inputTex, textureSize, clusteringRTsAndBuffers, rejectOld);
    }
}