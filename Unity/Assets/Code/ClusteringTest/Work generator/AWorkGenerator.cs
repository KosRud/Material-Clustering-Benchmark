using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGenerator {
public abstract class AWorkGenerator {

  protected readonly int kernelSize;
  protected readonly UnityEngine.Video.VideoClip[] videos;
  protected readonly ComputeShader csHighlightRemoval;

  public AWorkGenerator(
    int kernelSize,
    UnityEngine.Video.VideoClip[] videos,
    ComputeShader csHighlightRemoval
  ) {
    this.kernelSize = kernelSize;
    this.videos = videos;
    this.csHighlightRemoval = csHighlightRemoval;
  }

  public abstract void GenerateWork(
    Stack<ClusteringTest.LaunchParameters> workStack
  );

  public void GenerateWorkFrameTime(
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
        }
      }
    }

  }
}
}