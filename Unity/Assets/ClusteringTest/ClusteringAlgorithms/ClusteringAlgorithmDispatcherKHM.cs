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
        base.RunClustering(inputTex, textureSize, clusteringRTsAndBuffers);
    }
}