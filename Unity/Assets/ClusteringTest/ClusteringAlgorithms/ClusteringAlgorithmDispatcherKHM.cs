using UnityEngine;

public class ClusteringAlgorithmDispatcherKHM : ClusteringAlgorithmDispatcherKM {
    public readonly int paramKHMp;

    public ClusteringAlgorithmDispatcherKHM(
        int kernelSize, ComputeShader computeShader, int numIterations, bool doRandomizeEmptyClusters,
        int numClusters, int paramKHMp
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) {
        this.paramKHMp = paramKHMp;
    }

    public override string descriptionString => $"KHM({this.paramKHMp})";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.computeShader.SetBool("do_random_sample_empty_clusters", this.doRandomizeEmptyClusters);
        this.computeShader.SetInt("num_clusters", this.numClusters);
        this.computeShader.SetBool("KHM", true);

        for (int i = 0; i < this.numIterations; i++) {
            this.KMiteration(
                inputTex, textureSize, clusteringRTsAndBuffers,
                rejectOld: false
            );
        }
    }
}