using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class Rs1VsRs2 : AWorkGenerator
    {
        private const int textureSize = 64;

        public Rs1VsRs2(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(
                ClusteringTest.LogType.Variance,
                "Random swap (1KM) vs Random swap (2KM)"
            );

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                for (int numIterations = 1; numIterations < 31; numIterations++)
                {
                    foreach (int numIterationsKM in new int[] { 1, 2 })
                    {
                        AddRs(
                            workList: workList,
                            video: video,
                            numIterations: numIterations,
                            csHighlightRemoval: this.csHighlightRemoval,
                            numIterationsKM: numIterationsKM
                        );
                    }
                }
            }

            return workList;
        }

        private static void AddRs(
            WorkList workList,
            UnityEngine.Video.VideoClip video,
            int numIterations,
            ComputeShader csHighlightRemoval,
            int numIterationsKM
        )
        {
            if (
                DispatcherRSfixed.IsNumIterationsValid(
                    iterations: numIterations,
                    iterationsKM: numIterationsKM
                )
            )
            {
                workList.runs.Push(
                    new LaunchParameters(
                        staggeredJitter: false,
                        video: video,
                        doDownscale: false,
                        dispatcher: new DispatcherRSfixed(
                            computeShader: csHighlightRemoval,
                            numIterations: numIterations,
                            doRandomizeEmptyClusters: false,
                            useFullResTexRef: false,
                            numIterationsKM: numIterationsKM,
                            doReadback: false,
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
}
