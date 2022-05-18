using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGeneration
{
    public class FrameTime : AWorkGenerator
    {
        public FrameTime(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override WorkList GenerateWork()
        {
            var workList = new WorkList(ClusteringTest.LogType.FrameTime, "Frame time");

            const int textureSize = 64;

            for (int i = 0; i < 5; i++)
            {
                foreach (UnityEngine.Video.VideoClip video in this.videos)
                {
                    // 3 iterations
                    {
                        const int numIterations = 3;

                        // KM
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
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
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: 6,
                                        workingSize: textureSize,
                                        fullSize: ClusteringTest.fullTextureSize,
                                        jitterSize: 1
                                    )
                                )
                            )
                        );

                        foreach (bool doReadback in new bool[] { true, false })
                        {
                            // RS(2KM)
                            workList.runs.Push(
                                new LaunchParameters(
                                    staggeredJitter: false,
                                    video: video,
                                    doDownscale: false,
                                    dispatcher: new DispatcherRSfixed(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
                                        numIterationsKM: 2,
                                        doReadback: doReadback,
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

                    // 2 iterations
                    {
                        const int numIterations = 2;

                        // KM
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: 6,
                                        workingSize: textureSize,
                                        fullSize: ClusteringTest.fullTextureSize,
                                        jitterSize: 1
                                    )
                                )
                            )
                        );
                        // KM + readback
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new WrapperArtificialReadback(
                                    new DispatcherKM(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
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

                        // KHM
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                                        numClusters: 6,
                                        workingSize: textureSize,
                                        fullSize: ClusteringTest.fullTextureSize,
                                        jitterSize: 1
                                    )
                                )
                            )
                        );
                        // KHM + readback
                        workList.runs.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new WrapperArtificialReadback(
                                    new DispatcherKHM(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: false,
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

                    AddStopCondtion(
                        workList: workList,
                        video: video,
                        textureSize: textureSize,
                        this.csHighlightRemoval
                    );
                }
            }

            return workList;
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
                        doRandomizeEmptyClusters: false,
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
                        doRandomizeEmptyClusters: false,
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
                            doRandomizeEmptyClusters: false,
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
                            doRandomizeEmptyClusters: false,
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
