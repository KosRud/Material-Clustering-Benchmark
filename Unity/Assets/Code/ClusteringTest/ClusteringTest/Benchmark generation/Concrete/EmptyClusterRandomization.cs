using UnityEngine;
using ClusteringAlgorithms;

namespace BenchmarkGeneration
{
    public class EmptyClusterRandomization : ABenchmarkGenerator
    {
        public EmptyClusterRandomization(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(
                ClusteringTest.LogType.Variance,
                "Empty cluster randomization (KM)"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                /*
                  ! lowest textureSize must be no less, than kernel size
                */
                for (int textureSize = 64; textureSize >= 8; textureSize /= 2)
                {
                    foreach (bool doRandomizeEmptyClusters in new bool[] { true, false })
                    {
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: 3,
                                    doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                    useFullResTexRef: false,
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: 32,
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
