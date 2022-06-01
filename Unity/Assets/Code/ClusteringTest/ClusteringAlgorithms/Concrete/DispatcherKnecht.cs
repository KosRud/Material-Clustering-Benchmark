using UnityEngine;

namespace ClusteringAlgorithms
{
    public class DispatcherKnecht : WrapperStopCondition
    {
        private const int randomInitEveryNiterations = 5;
        private int frameCounter = 0;

        public DispatcherKnecht(
            ComputeShader computeShader,
            bool doRandomizeEmptyClusters,
            ClusteringRTsAndBuffers clusteringRTsAndBuffers
        )
            : base(
                new DispatcherKM(
                    computeShader,
                    1,
                    doRandomizeEmptyClusters,
                    useFullResTexRef: false,
                    clusteringRTsAndBuffers
                )
            )
        {
            this.frameCounter = 0;
        }

        public override string name => "Knecht";

        public override void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.frameCounter++;

            using (RunUntilConvergesResult result = this.RunUntilConverges(clusteringTextures))
            {
                if (result.converged == false || this.frameCounter == randomInitEveryNiterations)
                {
                    this.DoExploration(clusteringTextures, result);
                }

                if (this.frameCounter == randomInitEveryNiterations)
                {
                    this.frameCounter = 0;
                }
            }
        }

        private void DoExploration(
            ClusteringTextures textures,
            RunUntilConvergesResult currentResult
        )
        {
            // alters (currentResult.clusterCenters) - same array is filled with new data and re-used
            this.clusteringRTsAndBuffers.RandomizeClusterCenters();

            using (RunUntilConvergesResult newResult = this.RunUntilConverges(textures))
            {
                if (currentResult.clusterCenters.variance < newResult.clusterCenters.variance)
                {
                    this.clusteringRTsAndBuffers.SetClusterCenters(
                        currentResult.clusterCenters.centers
                    );
                }
            }
        }
    }
}
