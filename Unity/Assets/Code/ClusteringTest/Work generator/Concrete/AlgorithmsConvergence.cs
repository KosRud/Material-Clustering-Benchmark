using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGenerator {
  public class AlgorithmsConvergence : AWorkGenerator {

    public AlgorithmsConvergence(
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
      foreach (UnityEngine.Video.VideoClip video in this.videos) {
        for (
          int numIterations = 1;
          numIterations < 31;
          numIterations++
        ) {
          this.AddFixedIterations(
            workStack,
            this.kernelSize,
            video,
            numIterations,
            this.csHighlightRemoval
          );

        }
        this.AddStopCondtion(
          workStack,
          this.kernelSize,
          video,
          this.csHighlightRemoval
        );
      }
    }

    private void AddFixedIterations(
      Stack<ClusteringTest.LaunchParameters> workStack,
      int kernelSize,
      UnityEngine.Video.VideoClip video,
      int numIterations,
      ComputeShader csHighlightRemoval
    ) {
      // KM
      workStack.Push(
        new ClusteringTest.LaunchParameters(
          workingTextureSize: 64,
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
          workingTextureSize: 64,
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

      if (
        DispatcherRSfixed.IsNumIterationsValid(
          iterations: numIterations,
          iterationsKM: 2
        )
      ) {
        workStack.Push(
          new ClusteringTest.LaunchParameters(
            workingTextureSize: 64,
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
              doReadback: false
            )
          ).ThrowIfExists()
        );
      }
    }

    private void AddStopCondtion(
      Stack<ClusteringTest.LaunchParameters> workStack,
      int kernelSize,
      UnityEngine.Video.VideoClip video,
      ComputeShader csHighlightRemoval
    ) {
      // Knecht
      workStack.Push(
        new ClusteringTest.LaunchParameters(
          workingTextureSize: 64,
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

      // RS stop condition
      workStack.Push(
        new ClusteringTest.LaunchParameters(
          workingTextureSize: 64,
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