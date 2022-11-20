using UnityEngine;
using ClusteringAlgorithms;

namespace BenchmarkGeneration
{
    public class KHMp : ABenchmarkGenerator
    {
        private const int textureSize = 64;
        private const bool doRandomizeEmptyClusters = false;
        const int numIterations = 10;

        public KHMp(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(
                ClusteringTest.LogType.Variance,
                "KHM parameter p"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (float p = 2.0f; p <= 4f; p += 0.05f)
                {
                    workList.dispatches.Push(
                        new LaunchParameters(
                            staggeredJitter: false,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherKHMp(
                                computeShader: csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                useFullResTexRef: false,
                                parameters: new DispatcherKHMp.Parameters(p),
                                clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                    numClusters: 6,
                                    workingSize: textureSize,
                                    fullSize: ClusteringTest.fullTextureSize,
                                    jitterSize: 1
                                )
                            )
                        )
                    );
                }
            }

            return workList;
        }
    }
}
