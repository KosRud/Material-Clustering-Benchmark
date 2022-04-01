using UnityEngine;

public class CladFixedIterationsRS : ACladRS {
    public readonly bool doReadback;

    public CladFixedIterationsRS(
            int kernelSize, ComputeShader computeShader, int numIterations,
            bool doRandomizeEmptyClusters, int numClusters, int numIterationsKM,
            bool doReadback
        ) : base(
            kernelSize: kernelSize,
            computeShader: computeShader,
            numIterations: numIterations,
            doRandomizeEmptyClusters: doRandomizeEmptyClusters,
            numClusters: numClusters,
            numIterationsKM: numIterationsKM
        ) {
        Debug.Assert(
            IsNumIterationsValid(
                iterationsKM: numIterationsKM,
                iterations: numIterations
            )
        );
        this.doReadback = doReadback;
    }

    public override string descriptionString {
        get {
            string result = $"RS({this.iterationsKM}KM)";
            if (this.doReadback) {
                result += "_readback";
            }
            return result;
        }
    }

    public override void RunClustering(
        Texture inputTex,
        int textureSize,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
        this.KMiteration(
            inputTex, textureSize, clusteringRTsAndBuffers,
            rejectOld: true
        );

        for (int i = 1; i < this.numIterations; i += this.iterationsKM) {
            this.RandomSwap(inputTex, textureSize, clusteringRTsAndBuffers);
            for (int k = 0; k < this.iterationsKM; k++) {
                this.KMiteration(
                    inputTex, textureSize, clusteringRTsAndBuffers,
                    rejectOld: false
                );
            }
            if (this.doReadback) {
                this.ValidateCandidatesReadback(clusteringRTsAndBuffers);
            } else {
                this.ValidateCandidatesGPU(clusteringRTsAndBuffers);
            }
        }
    }
}