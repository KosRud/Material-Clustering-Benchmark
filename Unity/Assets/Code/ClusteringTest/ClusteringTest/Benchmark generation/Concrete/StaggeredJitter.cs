using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace BenchmarkGeneration
{
    public class StaggeredJitter : ABenchmarkGenerator
    {
        public StaggeredJitter(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(
                ClusteringTest.LogType.Variance,
                "Staggered jitter (KHM)"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                /*
                  ! lowest textureSize must be no less, than kernel size
                */
                for (int textureSize = 512; textureSize >= 8; textureSize /= 2)
                {
                    for (
                        int jitterSize = 1;
                        jitterSize * textureSize <= 512 && jitterSize <= 32;
                        jitterSize *= 2
                    )
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
                                        numClusters: 32,
                                        workingSize: textureSize,
                                        fullSize: ClusteringTest.fullTextureSize,
                                        jitterSize: jitterSize
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