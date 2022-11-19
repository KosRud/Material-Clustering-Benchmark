using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class ScalingVsSubsampling : AWorkGenerator
    {
        public ScalingVsSubsampling(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.Variance, "Scaling vs subsampling");

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (int textureSize = 256; textureSize >= 8; textureSize /= 2)
                {
                    foreach (bool doDownscale in new bool[] { true, false })
                    {
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: doDownscale,
                                dispatcher: new DispatcherRSfixed(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: 3,
                                    doRandomizeEmptyClusters: false,
                                    useFullResTexRef: true,
                                    parameters: new DispatcherRSfixed.Parameters(
                                        numIterationsKm: 2
                                    ),
                                    doReadback: false,
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
