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
      for (int i = 0; i < 20; i++) {
        foreach (UnityEngine.Video.VideoClip video in this.videos) {
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
                    kernelSize: this.kernelSize,
                    computeShader: this.csHighlightRemoval,
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
                    kernelSize: this.kernelSize,
                    computeShader: this.csHighlightRemoval,
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
                      kernelSize: this.kernelSize,
                      computeShader: this.csHighlightRemoval,
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
                    kernelSize: this.kernelSize,
                    computeShader: this.csHighlightRemoval,
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
                    kernelSize: this.kernelSize,
                    computeShader: this.csHighlightRemoval,
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
                  kernelSize: this.kernelSize,
                  computeShader: this.csHighlightRemoval,
                  doRandomizeEmptyClusters: false,
                  numClusters: 6
                )
              ).ThrowIfExists()
            );

            // RS stop condition
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                textureSize: 64,
                staggeredJitter: false,
                jitterSize: 1,
                video: video,
                doDownscale: false,
                dispatcher: new DispatcherRSstopCondition(
                  kernelSize: kernelSize,
                  computeShader: csHighlightRemoval,
                  doRandomizeEmptyClusters: false,
                  numClusters: 6,
                  numIterationsKM: 2
                )
              ).ThrowIfExists()
            );
          }
        }
      }
    }
  }
}