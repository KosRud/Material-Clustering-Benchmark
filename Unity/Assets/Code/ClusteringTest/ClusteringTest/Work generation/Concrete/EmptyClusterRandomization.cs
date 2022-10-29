using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class EmptyClusterRandomization : AWorkGenerator
    {
        public EmptyClusterRandomization(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(
                ClusteringTest.LogType.Variance,
                "Empty cluster randomization"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (int textureSize = 512; textureSize >= 32; textureSize /= 2)
                {
                    foreach (bool doRandomizeEmptyClusters in new bool[] { true, false })
                    {
                        workList.runs.Push(
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
