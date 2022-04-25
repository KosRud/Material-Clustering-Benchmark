using UnityEngine;
using System;

namespace ClusteringAlgorithms
{
    public abstract class ADispatcherRS : DispatcherKM
    {
        public class Parameters : DispatcherParameters
        {
            public int numIterationsKm;
            public bool stopCondition;

            public Parameters(int numIterationsKm, bool stopCondition)
            {
                this.numIterationsKm = numIterationsKm;
                this.stopCondition = stopCondition;
            }
        }

        protected Parameters _parameters;
        public override DispatcherParameters parameters => this._parameters;

        protected readonly int kernelHandleRandomSwap;
        protected readonly int kernelHandleValidateCandidates;

        public ADispatcherRS(
            ComputeShader computeShader,
            int numIterations,
            bool doRandomizeEmptyClusters,
            int numIterationsKm,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        ) : base(computeShader, numIterations, doRandomizeEmptyClusters, clusteringRTsAndBuffers)
        {
            this._parameters = new Parameters(
                numIterationsKm: numIterationsKm,
                stopCondition: false
            );
            this.kernelHandleRandomSwap = this.computeShader.FindKernel("RandomSwap");
            this.kernelHandleValidateCandidates = this.computeShader.FindKernel(
                "ValidateCandidates"
            );
        }

        public sealed override string name => "RS";

        public abstract override void RunClustering(ClusteringTextures clusteringTextures);

        protected float ValidateCandidatesReadback()
        {
            float varianceChange = ClusterCenters.invalidVariance;

            using (ClusterCenters clusterCenters = this.clusteringRTsAndBuffers.GetClusterCenters())
            {
                for (int i = 0; i < this.clusteringRTsAndBuffers.numClusters; i++)
                {
                    if (
                        clusterCenters.centers[i].z < ClusterCenters.invalidVariance
                        && clusterCenters.centers[i + this.clusteringRTsAndBuffers.numClusters].z
                            < ClusterCenters.invalidVariance
                    )
                    {
                        varianceChange =
                            clusterCenters.centers[i].z
                            - clusterCenters.centers[
                                i + this.clusteringRTsAndBuffers.numClusters
                            ].z;
                    }

                    if (
                        clusterCenters.centers[i].z
                        < clusterCenters.centers[i + this.clusteringRTsAndBuffers.numClusters].z
                    )
                    {
                        clusterCenters.centers[i + this.clusteringRTsAndBuffers.numClusters] =
                            clusterCenters.centers[i];
                    }
                    else
                    {
                        clusterCenters.centers[i] = clusterCenters.centers[
                            i + this.clusteringRTsAndBuffers.numClusters
                        ];
                    }
                }
                this.clusteringRTsAndBuffers.SetClusterCenters(clusterCenters.centers);
            }

            if (varianceChange == ClusterCenters.invalidVariance)
            {
                throw new ClusterCenters.InvalidClustersException("all clusters are invalid");
            }

            return varianceChange;
        }

        protected void ValidateCandidatesGPU()
        {
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
