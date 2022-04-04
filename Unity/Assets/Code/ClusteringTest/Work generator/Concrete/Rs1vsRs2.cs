using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGenerator {
  public class Rs1VsRs2 : AWorkGenerator {

    public Rs1VsRs2(
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
          foreach (
            int numIterationsKM in new int[1, 2]
          ) {
            this.AddRs(
              workStack: workStack,
              kernelSize: this.kernelSize,
              video: video,
              numIterations: numIterations,
              csHighlightRemoval: this.csHighlightRemoval,
              numIterationsKM: numIterationsKM
            );
          }
        }
      }
    }

    private void AddRs(
      Stack<ClusteringTest.LaunchParameters> workStack,
      int kernelSize,
      UnityEngine.Video.VideoClip video,
      int numIterations,
      ComputeShader csHighlightRemoval,
      int numIterationsKM
    ) {
      if (
        DispatcherRSfixed.IsNumIterationsValid(
          iterations: numIterations,
          iterationsKM: numIterationsKM
        )
      ) {
        workStack.Push(
          new ClusteringTest.LaunchParameters(
            textureSize: 64,
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
              numIterationsKM: numIterationsKM,
              doReadback: false
            )
          ).ThrowIfExists()
        );
      }
    }
  }
}