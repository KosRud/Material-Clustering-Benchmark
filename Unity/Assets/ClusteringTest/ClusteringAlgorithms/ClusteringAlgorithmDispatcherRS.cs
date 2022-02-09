using UnityEngine;

public class ClusteringAlgorithmDispatcherRS : ClusteringAlgorithmDispatcherKM {
    public readonly int iterationsKM;
    public readonly bool doReadback;

    private readonly int kernelHandleRandomSwap;
    private readonly int kernelHandleValidateCandidates;

    public ClusteringAlgorithmDispatcherRS(
        int kernelSize, ComputeShader computeShader, int numIterations,
        bool doRandomizeEmptyClusters, int numClusters, int numIterationsKM,
        bool doReadback
    ) : base(kernelSize, computeShader, numIterations, doRandomizeEmptyClusters, numClusters) {
        Debug.Assert(
            IsNumIterationsValid(
                iterationsKM: numIterationsKM,
                iterations: numIterations
            )
        );
        this.iterationsKM = numIterationsKM;
        this.kernelHandleRandomSwap = this.computeShader.FindKernel("RandomSwap");
        this.kernelHandleValidateCandidates = this.computeShader.FindKernel("ValidateCandidates");
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
            this.ValidateCandidates(clusteringRTsAndBuffers);
        }
    }

    private void ValidateCandidates(ClusteringRTsAndBuffers clusteringRTsAndBuffers) {
        if (this.doReadback) {
            Vector4[] clusterCenters = clusteringRTsAndBuffers.clusterCenters;
            for (int i = 0; i < this.numClusters; i++) {
                if (clusterCenters[i].z < clusterCenters[i + this.numClusters].z) {
                    clusterCenters[i + this.numClusters] = clusterCenters[i];
                } else {
                    clusterCenters[i] = clusterCenters[i + this.numClusters];
                }
            }
            clusteringRTsAndBuffers.clusterCenters = clusterCenters;
        } else {
            this.computeShader.SetBuffer(
                this.kernelHandleValidateCandidates,
                "cbuf_cluster_centers",
                clusteringRTsAndBuffers.cbufClusterCenters
            );
            this.computeShader.Dispatch(this.kernelHandleValidateCandidates, 1, 1, 1);
        }
    }

    private void RandomSwap(Texture inputTex, int textureSize, ClusteringRTsAndBuffers clusteringRTsAndBuffers) {
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