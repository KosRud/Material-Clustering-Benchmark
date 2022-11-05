using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGeneration
{
    public class StaggeredJitter : AWorkGenerator
    {
        public StaggeredJitter(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.Variance, "Staggered jitter");

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
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: 3,
                                    doRandomizeEmptyClusters: true,
                                    useFullResTexRef: true,
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: 6,
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
