using UnityEngine;
using ClusteringAlgorithms;

namespace BenchmarkGeneration
{
    public class Subsampling : ABenchmarkGenerator
    {
        public Subsampling(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(
                ClusteringTest.LogType.Variance,
                "Subsampling (KHM)"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                /*
                  ! lowest textureSize must be no less, than kernel size
                */
                for (int textureSize = 512; textureSize >= 8; textureSize /= 2)
                {
                    foreach (int numClusters in new int[] { 3, 4, 6, 9, 12, 15, 19, 24, 32 })
                    {
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHMp(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: 3,
                                    doRandomizeEmptyClusters: false,
                                    useFullResTexRef: true,
                                    parameters: DispatcherKHMp.Parameters.Default(),
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: numClusters,
                                        workingSize: textureSize,
                                        fullSize: ClusteringTest.fullTextureSize,
                                        jitterSize: 1
                                    )
                                )
                            )
                        );
                    }
                }
            }

            return workList;
        }
    }
}
