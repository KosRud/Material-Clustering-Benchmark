using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class KHMp : AWorkGenerator
    {
        private const int textureSize = 64;
        private const bool doRandomizeEmptyClusters = false;
        const int numIterations = 10;

        public KHMp(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.Variance, "KHM parameter p");

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (float p = 2.0f; p <= 4f; p += 0.1f)
                {
                    workList.runs.Push(
                        new LaunchParameters(
                            staggeredJitter: false,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherKHM(
                                computeShader: csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                useFullResTexRef: false,
                                parameters: new DispatcherKHM.Parameters(p),
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
