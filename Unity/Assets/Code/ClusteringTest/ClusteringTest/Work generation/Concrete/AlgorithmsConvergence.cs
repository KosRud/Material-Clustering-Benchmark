using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class AlgorithmsConvergence : AWorkGenerator
    {
        private const int textureSize = 64;

        public AlgorithmsConvergence(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.Variance, "Algorithm convergence");

            foreach (UnityEngine.Video.VideoClip video in this.videos)
            {
                const int numIterations = 3;

                AddFixedIterations(
                    workList: workList,
                    video: video,
                    textureSize: textureSize,
                    numIterations: numIterations,
                    csHighlightRemoval: this.csHighlightRemoval
                );

                AddStopCondtion(
                    workList: workList,
                    video: video,
                    textureSize: textureSize,
                    csHighlightRemoval: this.csHighlightRemoval
                );
            }

            return workList;
        }

        private static void AddFixedIterations(
            WorkList workList,
            UnityEngine.Video.VideoClip video,
            int textureSize,
            int numIterations,
            ComputeShader csHighlightRemoval
        )
        {
            // KM
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherKM(
                        computeShader: csHighlightRemoval,
                        numIterations: numIterations,
                        doRandomizeEmptyClusters: true,
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

            // KHM
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherKHM(
                        computeShader: csHighlightRemoval,
                        numIterations: numIterations,
                        doRandomizeEmptyClusters: true,
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

            // RS
            if (DispatcherRSfixed.IsNumIterationsValid(iterations: numIterations, iterationsKM: 2))
            {
                workList.runs.Push(
                    new LaunchParameters(
                        staggeredJitter: false,
                        video: video,
                        doDownscale: false,
                        dispatcher: new DispatcherRSfixed(
                            computeShader: csHighlightRemoval,
                            numIterations: numIterations,
                            doRandomizeEmptyClusters: true,
                            useFullResTexRef: false,
                            numIterationsKM: 2,
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

        private static void AddStopCondtion(
            WorkList workList,
            UnityEngine.Video.VideoClip video,
            int textureSize,
            ComputeShader csHighlightRemoval
        )
        {
            // Knecht
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherKnecht(
                        computeShader: csHighlightRemoval,
                        doRandomizeEmptyClusters: true,
                        clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                            numClusters: 6,
                            workingSize: textureSize,
                            fullSize: ClusteringTest.fullTextureSize,
                            jitterSize: 1
                        )
                    )
                )
            );

            // RS stop condition
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherRSstopCondition(
                        computeShader: csHighlightRemoval,
                        doRandomizeEmptyClusters: true,
                        useFullResTexRef: false,
                        numIterationsKM: 2,
                        clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                            numClusters: 6,
                            workingSize: textureSize,
                            fullSize: ClusteringTest.fullTextureSize,
                            jitterSize: 1
                        )
                    )
                )
            );

            // KM stop condition
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new WrapperStopCondition(
                        new DispatcherKM(
                            computeShader: csHighlightRemoval,
                            doRandomizeEmptyClusters: true,
                            useFullResTexRef: false,
                            numIterations: 1,
                            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                numClusters: 6,
                                workingSize: textureSize,
                                fullSize: ClusteringTest.fullTextureSize,
                                jitterSize: 1
                            )
                        )
                    )
                )
            );

            // KHM stop condition
            workList.runs.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new WrapperStopCondition(
                        new DispatcherKHM(
                            computeShader: csHighlightRemoval,
                            doRandomizeEmptyClusters: true,
                            useFullResTexRef: false,
                            numIterations: 1,
                            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                numClusters: 6,
                                workingSize: textureSize,
                                fullSize: ClusteringTest.fullTextureSize,
                                jitterSize: 1
                            )
                        )
                    )
                )
            );
        }
    }
}
