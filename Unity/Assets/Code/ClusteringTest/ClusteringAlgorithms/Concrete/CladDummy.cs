using UnityEngine;

public class CladDummy : AClad {
    public CladDummy(ComputeShader computeShader, int numClusters) : base(
        kernelSize: 4,
        computeShader: computeShader,
        numIterations: 1,
        doRandomizeEmptyClusters: false,
        numClusters: numClusters) {

    }

    public override string descriptionString => "Null";

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        // do nothing
    }
}