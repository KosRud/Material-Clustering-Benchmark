using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public abstract class ADispatcherRS : DispatcherKM
    {
        [Serializable]
        public class Parameters : DispatcherParameters
        {
            public int numIterationsKm;

            public Parameters(int numIterationsKm)
            {
                this.numIterationsKm = numIterationsKm;
            }
        }

        protected readonly Parameters parameters;
        public override DispatcherParameters abstractParameters => this.parameters;

        public override abstract bool usesStopCondition { get; }

        protected readonly int kernelHandleRandomSwap;
        protected readonly int kernelHandleValidateCandidates;

        private static ObjectPoolMaxAssert<RandomSwapResult> RandomSwapResultPool =
            new ObjectPoolMaxAssert<RandomSwapResult>(
                createFunc: () =>
                    new RandomSwapResult(
                        stopConditionOverride: RandomSwapResult.StopConditionOverride.Default,
                        varianceReduction: 0,
                        swapFailed: false
                    ),
                maxActive: 1
            );

        public ADispatcherRS(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            bool useFullResTexRef,
            Parameters parameters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                computeShader: computeShader,
                numIterations: numIterations,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                useFullResTexRef: useFullResTexRef,
                clusteringRTsAndBuffers: clusteringRTsAndBuffers
            )
        {
            this.parameters = parameters;
            this.kernelHandleRandomSwap = this.computeShader.FindKernel("RandomSwap");
            this.kernelHandleValidateCandidates = this.computeShader.FindKernel(
                "ValidateCandidates"
            );
        }

        public override string name => "RS";

        public class RandomSwapResult : System.IDisposable
        {
            public enum StopConditionOverride
            {
                KeepRunning,
                Stop,
                Default
            }

            public StopConditionOverride stopConditionOverride;
            public float varianceReduction;
            public bool swapFailed;

            public RandomSwapResult(
                StopConditionOverride stopConditionOverride,
                float varianceReduction,
                bool swapFailed
            )
            {
                this.varianceReduction = varianceReduction;
                this.stopConditionOverride = stopConditionOverride;
                this.swapFailed = swapFailed;
            }

            public void Dispose()
            {
                RandomSwapResultPool.Release(this);
            }
        }

        public abstract override void RunClustering(ClusteringTextures clusteringTextures);

        /// <summary>
        ///
        /// </summary>
        /// <returns>Must be disposed.</returns>
        protected RandomSwapResult ValidateCandidatesReadback()
        {
            RandomSwapResult randomSwapResult = RandomSwapResultPool.Get();
            randomSwapResult.varianceReduction = 0;
            randomSwapResult.stopConditionOverride = RandomSwapResult.StopConditionOverride.Default;
            randomSwapResult.swapFailed = false;

            using (ClusterCenters clusterCenters = this.clusteringRTsAndBuffers.GetClusterCenters())
            {
                /*
                    variance = ...
                    
                    positive number 	==	valid variance
                    -1 					==	not a single pixel has sufficient chromatic component

                    |0              |numClusters	|
                    |---------------|---------------|
                    |  new centers	| old centers	|
                */
                if (clusterCenters.variance != null)
                {
                    // In the new frame at least one pixel has sufficient chromatic portion, i.e. new variance is not null.

                    if (
                        clusterCenters.oldVariance == null
                        || clusterCenters.variance < clusterCenters.oldVariance
                    )
                    {
                        // Either in the previous run not a single pixel had sufficient chromatic portion, or variance improved.

                        if (clusterCenters.oldVariance != null)
                        {
                            /*
                                old variance and new variance are both not null
                                new variance < old variance
                            */

                            randomSwapResult.varianceReduction =
                                (float)clusterCenters.oldVariance - (float)clusterCenters.variance;
                        }
                        else
                        {
                            /*
                                old variance is null
                                new variance is not null
                            */

                            randomSwapResult.stopConditionOverride = RandomSwapResult
                                .StopConditionOverride
                                .KeepRunning;
                        }

                        // save new cluster centers as old
                        for (int i = 0; i < this.clusteringRTsAndBuffers.numClusters; i++)
                        {
                            clusterCenters.centers[i + this.clusteringRTsAndBuffers.numClusters] =
                                clusterCenters.centers[i];
                        }
                    }
                    else
                    {
                        // variance did not improve (failed swap)

                        randomSwapResult.swapFailed = true;

                        // restore old cluster centers as new
                        for (int i = 0; i < this.clusteringRTsAndBuffers.numClusters; i++)
                        {
                            clusterCenters.centers[i] = clusterCenters.centers[
                                i + this.clusteringRTsAndBuffers.numClusters
                            ];
                        }
                    }
                }
                else
                {
                    // in the current frame not a single pixel has sufficient chromatic portion

                    randomSwapResult.stopConditionOverride = RandomSwapResult
                        .StopConditionOverride
                        .Stop;
                }
                this.clusteringRTsAndBuffers.SetClusterCenters(clusterCenters.centers);
            }

            return randomSwapResult;
        }

        protected void ValidateCandidatesGPU()
        {
            this.computeShader.SetInt("num_clusters", this.clusteringRTsAndBuffers.numClusters);

            this.computeShader.SetBuffer(
                this.kernelHandleValidateCandidates,
                "cbuf_cluster_centers",
                this.clusteringRTsAndBuffers.cbufClusterCenters
            );

            this.computeShader.Dispatch(this.kernelHandleValidateCandidates, 1, 1, 1);
        }

        protected void RandomSwap(ClusteringTextures clusteringTextures)
        {
            this.clusteringRTsAndBuffers.UpdateRandomPositions();

            this.computeShader.SetBuffer(
                this.kernelHandleRandomSwap,
                "cbuf_cluster_centers",
                this.clusteringRTsAndBuffers.cbufClusterCenters
            );
            this.computeShader.SetBuffer(
                this.kernelHandleRandomSwap,
                "cbuf_random_positions",
                this.clusteringRTsAndBuffers.cbufRandomPositions
            );
            this.computeShader.SetTexture(
                this.kernelHandleRandomSwap,
                "tex_input",
                clusteringTextures.rtInput
            );
            this.computeShader.SetInt(
                "randomClusterCenter",
                this.clusteringRTsAndBuffers.PickRandomCluster(
                    this.clusteringRTsAndBuffers.numClusters
                )
            );
            this.computeShader.Dispatch(this.kernelHandleRandomSwap, 1, 1, 1);
        }
    }
}
