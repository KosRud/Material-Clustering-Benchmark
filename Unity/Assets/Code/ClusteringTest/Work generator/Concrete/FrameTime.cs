using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGenerator {
  public class FrameTime : AWorkGenerator {

    public FrameTime(
      int kernelSize,
      UnityEngine.Video.VideoClip[] videos,
      ComputeShader csHighlightRemoval
    ) : base(
        kernelSize: kernelSize,
        videos: videos,
        csHighlightRemoval: csHighlightRemoval) { }


    public override void GenerateWork(
      Stack<ClusteringTest.LaunchParameters> workStack
    ) {
      for (int i = 0; i < 5; i++) {
        foreach (UnityEngine.Video.VideoClip video in this.videos) {
          foreach (int textureSize in new int[] { 512, 64 }) {
            // 3 iterations
            {
              const int numIterations = 3;

              // KM
              workStack.Push(
                new ClusteringTest.LaunchParameters(
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
                ).ThrowIfExists()
              );

              // KHM
              workStack.Push(
                new ClusteringTest.LaunchParameters(
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
                ).ThrowIfExists()
              );

              foreach (bool doReadback in new bool[] { true, false }) {
                // RS(2KM)
                workStack.Push(
                  new ClusteringTest.LaunchParameters(
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
                ).ThrowIfExists()
              );

              // KHM
              workStack.Push(
                new ClusteringTest.LaunchParameters(
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
                ).ThrowIfExists()
              );
            }
            // Knecht
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                staggeredJitter: false,
                video: video,
                doDownscale: false,
                dispatcher: new DispatcherKnecht(
                  computeShader: this.csHighlightRemoval,
                  doRandomizeEmptyClusters: false,
                  clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                    numClusters: 6,
                    workingSize: textureSize,
                    fullSize: ClusteringTest.fullTextureSize,
                    jitterSize: 1
                  )
                )
              ).ThrowIfExists()
            );

            // RS stop condition
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                staggeredJitter: false,
                video: video,
                doDownscale: false,
                dispatcher: new DispatcherRSstopCondition(
                  computeShader: this.csHighlightRemoval,
                  doRandomizeEmptyClusters: false,
                  numIterationsKM: 2,
                  clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                    numClusters: 6,
                    workingSize: textureSize,
                    fullSize: ClusteringTest.fullTextureSize,
                    jitterSize: 1
                  )
                )
              ).ThrowIfExists()
            );
          }
        }
      }
    }
  }
}