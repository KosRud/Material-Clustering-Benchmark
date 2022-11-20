using UnityEngine;
using ClusteringAlgorithms;

namespace BenchmarkGeneration
{
    public class FrameTime : ABenchmarkGenerator
    {
        private const int textureSize = 64;
        private const bool doRandomizeEmptyClusters = false;

        public FrameTime(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        ) : base(kernelSize: kernelSize, videos: videos, csHighlightRemoval: csHighlightRemoval) { }

        public override BenchmarkDescription GenerateBenchmark()
        {
            var workList = new BenchmarkDescription(ClusteringTest.LogType.FrameTime, "Frame time");

            for (int i = 0; i < 5; i++)
            {
                foreach (UnityEngine.Video.VideoClip video in this.videos)
                {
                    // 3 iterations
                    {
                        const int numIterations = 3;

                        // KM
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
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

                        // KHM
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHMp(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters,
                                    useFullResTexRef: false,
                                    parameters: DispatcherKHMp.Parameters.Default(),
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
                            workList.dispatches.Push(
                                new LaunchParameters(
                                    staggeredJitter: false,
                                    video: video,
                                    doDownscale: false,
                                    dispatcher: new DispatcherRSfixed(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                        useFullResTexRef: false,
                                        parameters: DispatcherRSfixed.Parameters.Default(),
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
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
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
                        // KM + readback
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new WrapperArtificialReadback(
                                    new DispatcherKM(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
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
                            )
                        );

                        // KHM
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHMp(
                                    computeShader: this.csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                    useFullResTexRef: false,
                                    parameters: DispatcherKHMp.Parameters.Default(),
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
                        workList.dispatches.Push(
                            new LaunchParameters(
                                staggeredJitter: false,
                                video: video,
                                doDownscale: false,
                                dispatcher: new WrapperArtificialReadback(
                                    new DispatcherKHMp(
                                        computeShader: this.csHighlightRemoval,
                                        numIterations: numIterations,
                                        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                                        useFullResTexRef: false,
                                        parameters: DispatcherKHMp.Parameters.Default(),
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
            BenchmarkDescription workList,
            UnityEngine.Video.VideoClip video,
            int textureSize,
            ComputeShader csHighlightRemoval
        )
        {
            // Knecht
            workList.dispatches.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherKnecht(
                        computeShader: csHighlightRemoval,
                        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
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
            workList.dispatches.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherRSstopCondition(
                        computeShader: csHighlightRemoval,
                        doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                        useFullResTexRef: false,
                        parameters: DispatcherRSfixed.Parameters.Default(),
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
            workList.dispatches.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new WrapperStopCondition(
                        new DispatcherKM(
                            computeShader: csHighlightRemoval,
                            doRandomizeEmptyClusters: doRandomizeEmptyClusters,
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
            workList.dispatches.Push(
                new LaunchParameters(
                    staggeredJitter: false,
                    video: video,
                    doDownscale: false,
                    dispatcher: new WrapperStopCondition(
                        new DispatcherKHMp(
                            computeShader: csHighlightRemoval,
                            doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                            useFullResTexRef: false,
                            numIterations: 1,
                            parameters: DispatcherKHMp.Parameters.Default(),
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
