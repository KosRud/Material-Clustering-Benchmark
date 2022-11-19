using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class KHMp : AWorkGenerator
    {
        private const int textureSize = 64;
        const int numIterations = 10;
        private const bool doRandomizeEmptyClusters = false;

        public KHMp(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.Variance, "Algorithm convergence");

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (int numIterations = 1; numIterations < 30; numIterations++)
                {
                    for (float p = 2.0f; p <= 4.0f; p += 0.05f)
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
            }

            return workList;
        }
    }
}
