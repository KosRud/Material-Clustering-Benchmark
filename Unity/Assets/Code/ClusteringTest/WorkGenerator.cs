using UnityEngine;

public static class WorkGenerator {

    public static void GenerateWorkSubsampling(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
                foreach (int numClusters in new int[] { 4, 6, 8, 12, 16 }) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherKM(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                numIterations: 3,
                                doRandomizeEmptyClusters: false,
                                numClusters: numClusters
                            )
                        ).ThrowIfExists()
                    );
                }
            }
        }
    }

    public static void GenerateWorkScalingVsSubsampling(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {

        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
                foreach (bool doDownscale in new bool[] { true, false }) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: doDownscale,
                            dispatcher: new DispatcherKM(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                numIterations: 3,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6
                            )
                        ).ThrowIfExists()
                    );
                }
            }
        }
    }

    public static void GenerateWorkStaggeredJitter(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        /*
        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int textureSize = 64; textureSize >= 4; textureSize /= 2) {
                for (
                        int jitterSize = 1;
                        jitterSize <= 16 && jitterSize * textureSize <= 64;
                        jitterSize *= 2
                    ) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            numIterations: 3,
                            numClusters: 6,
                            doRandomSwap: false,
                            doRandomizeEmptyClusters: false,
                            doKHM: false,
                            staggeredJitter: true,
                            jitterSize: jitterSize,
                            video: video,
                            doDownscale: false
                        )
                    );

                    string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

                    if (System.IO.File.Exists(fileName)) {
                        UnityEditor.EditorApplication.isPlaying = false;
                        throw new System.Exception($"File exists: {fileName}");
                    }
                }
            }
        }
        */
    }

    public static void GenerateWorkScanlineJitter(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        /*
        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int textureSize = 64; textureSize >= 4; textureSize /= 2) {
                for (
                        int jitterSize = 1;
                        jitterSize <= 16 && jitterSize * textureSize <= 64;
                        jitterSize *= 2
                    ) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            numIterations: 3,
                            numClusters: 6,
                            doRandomSwap: false,
                            doRandomizeEmptyClusters: false,
                            doKHM: false,
                            staggeredJitter: false,
                            jitterSize: jitterSize,
                            video: video,
                            doDownscale: false
                        )
                    );

                    string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

                    if (System.IO.File.Exists(fileName)) {
                        UnityEditor.EditorApplication.isPlaying = false;
                        throw new System.Exception($"File exists: {fileName}");
                    }
                }
            }
        }
        */
    }

    public static void GenerateWorkEmptyClusterRandomization(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        /*
        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
                foreach (bool doRandomizeEmptyClusters in new bool[] { true, false }) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            numIterations: 3,
                            numClusters: 6,
                            doRandomSwap: false,
                            doRandomizeEmptyClusters: doRandomizeEmptyClusters,
                            doKHM: false,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false
                        )
                    );

                    string fileName = $"Variance logs/{this.GetFileName(this.currentWorkParameters)}";

                    if (System.IO.File.Exists(fileName)) {
                        UnityEditor.EditorApplication.isPlaying = false;
                        throw new System.Exception($"File exists: {fileName}");
                    }
                }
            }
        }
        */
    }

    public static void GenerateWorkKHMandRadomSwap(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        /*
        foreach (UnityEngine.Video.VideoClip video in videos) {
            for (int numIterations = 1; numIterations < 31; numIterations += 1) {
                // KM
                workStack.Push(
                    new ClusteringTest.LaunchParameters(
                        textureSize: 64,
                        numClusters: 6,
                        staggeredJitter: false,
                        jitterSize: 1,
                        video: video,
                        doDownscale: false,
                        dispatcher: new DispatcherKM(
                            kernelSize: kernelSize,
                            computeShader: csHighlightRemoval,
                            numIterations: numIterations,
                            doRandomizeEmptyClusters: false,
                            numClusters: 6
                        )
                    ).ThrowIfExists()
                );

                // KHM
                workStack.Push(
                    new ClusteringTest.LaunchParameters(
                        textureSize: 64,
                        numClusters: 6,
                        staggeredJitter: false,
                        jitterSize: 1,
                        video: video,
                        doDownscale: false,
                        dispatcher: new DispatcherKHM(
                            kernelSize: kernelSize,
                            computeShader: csHighlightRemoval,
                            numIterations: numIterations,
                            doRandomizeEmptyClusters: false,
                            numClusters: 6
                        )
                    ).ThrowIfExists()
                );

                // RS(1KM)
                if (
                    DispatcherRS.IsNumIterationsValid(
                        iterations: numIterations,
                        iterationsKM: 1
                    )
                ) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: 64,
                            numClusters: 6,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherRS(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6,
                                numIterationsKM: 1,
                                doReadback: false
                            )
                        ).ThrowIfExists()
                    );
                }

                //RS(2KM)
                if (
                    DispatcherRS.IsNumIterationsValid(
                        iterations: numIterations,
                        iterationsKM: 2
                    )
                ) {
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: 64,
                            numClusters: 6,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherRS(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                numIterations: numIterations,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6,
                                numIterationsKM: 2,
                                doReadback: false
                            )
                        ).ThrowIfExists()
                    );
                }
            }
            // Knecht
            workStack.Push(
                new ClusteringTest.LaunchParameters(
                    textureSize: 64,
                    numClusters: 6,
                    staggeredJitter: false,
                    jitterSize: 1,
                    video: video,
                    doDownscale: false,
                    dispatcher: new DispatcherKnecht(
                        kernelSize: kernelSize,
                        computeShader: csHighlightRemoval,
                        doRandomizeEmptyClusters: false,
                        numClusters: 6
                    )
                ).ThrowIfExists()
            );
        }
        */
    }

    public static void GenerateWorkFrameTime(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        for (int i = 0; i < 20; i++) {
            foreach (UnityEngine.Video.VideoClip video in videos) {
                foreach (int textureSize in new int[] { 512, 64 }) {
                    // 3 iterations
                    {
                        const int numIterations = 3;

                        // KM
                        workStack.Push(
                            new ClusteringTest.LaunchParameters(
                                textureSize: textureSize,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    kernelSize: kernelSize,
                                    computeShader: csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6
                                )
                            ).ThrowIfExists()
                        );

                        // KHM
                        workStack.Push(
                            new ClusteringTest.LaunchParameters(
                                textureSize: textureSize,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHM(
                                    kernelSize: kernelSize,
                                    computeShader: csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6
                                )
                            ).ThrowIfExists()
                        );

                        foreach (bool doReadback in new bool[] { true, false }) {
                            // RS(2KM)
                            workStack.Push(
                                 new ClusteringTest.LaunchParameters(
                                     textureSize: textureSize,
                                     staggeredJitter: false,
                                     jitterSize: 1,
                                     video: video,
                                     doDownscale: false,
                                     dispatcher: new DispatcherRSfixed(
                                         kernelSize: kernelSize,
                                         computeShader: csHighlightRemoval,
                                         numIterations: numIterations,
                                         doRandomizeEmptyClusters: false,
                                         numClusters: 6,
                                         numIterationsKM: 2,
                                         doReadback: doReadback
                                     )
                                 ).ThrowIfExists()
                             );
                        }
                    }

                    // 1 iteration
                    {
                        const int numIterations = 1;

                        // KM
                        workStack.Push(
                            new ClusteringTest.LaunchParameters(
                                textureSize: textureSize,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKM(
                                    kernelSize: kernelSize,
                                    computeShader: csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6
                                )
                            ).ThrowIfExists()
                        );

                        // KHM
                        workStack.Push(
                            new ClusteringTest.LaunchParameters(
                                textureSize: textureSize,
                                staggeredJitter: false,
                                jitterSize: 1,
                                video: video,
                                doDownscale: false,
                                dispatcher: new DispatcherKHM(
                                    kernelSize: kernelSize,
                                    computeShader: csHighlightRemoval,
                                    numIterations: numIterations,
                                    doRandomizeEmptyClusters: false,
                                    numClusters: 6
                                )
                            ).ThrowIfExists()
                        );
                    }
                    // Knecht
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherKnecht(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6
                            )
                        ).ThrowIfExists()
                    );
                }
            }
        }

    }

    public static void GenerateWorkRSstopCondition(
        System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack,
        int kernelSize,
        UnityEngine.Video.VideoClip[] videos,
        ComputeShader csHighlightRemoval
    ) {
        for (int i = 0; i < 5; i++) {
            foreach (UnityEngine.Video.VideoClip video in videos) {
                foreach (int textureSize in new int[] { 512, 64 }) {
                    // RS stop condition
                    workStack.Push(
                        new ClusteringTest.LaunchParameters(
                            textureSize: textureSize,
                            staggeredJitter: false,
                            jitterSize: 1,
                            video: video,
                            doDownscale: false,
                            dispatcher: new DispatcherRSstopCondition(
                                kernelSize: kernelSize,
                                computeShader: csHighlightRemoval,
                                doRandomizeEmptyClusters: false,
                                numClusters: 6,
                                numIterationsKM: 2,
                                maxFailedSwaps: 1
                            )
                        ).ThrowIfExists()
                    );
                }
            }
        }

    }
}