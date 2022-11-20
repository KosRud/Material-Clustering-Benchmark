using UnityEngine;
using ClusteringAlgorithms;

namespace BenchmarkGeneration
{
    public class ScalingVsSubsampling : ABenchmarkGenerator
    {
        public ScalingVsSubsampling(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(
                ClusteringTest.LogType.Variance,
                "Scaling vs subsampling (KHM)"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (int textureSize = 256; textureSize >= 8; textureSize /= 2)
                {
                    foreach (bool doDownscale in new bool[] { true, false })
                    {
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: doDownscale,
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
