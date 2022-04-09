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
      UnityEngine.Video.VideoClip video,
      int numIterations,
      ComputeShader csHighlightRemoval
    ) {
      // KM
      workStack.Push(
        new ClusteringTest.LaunchParameters(
          staggeredJitter: false,
          video: video,
          doDownscale: false,
          dispatcher: new DispatcherKM(
            computeShader: csHighlightRemoval,
            numIterations: numIterations,
            doRandomizeEmptyClusters: false,
            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
              numClusters: 6,
              workingSize: 64,
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
            computeShader: csHighlightRemoval,
            numIterations: numIterations,
            doRandomizeEmptyClusters: false,
            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
              numClusters: 6,
              workingSize: 64,
              fullSize: ClusteringTest.fullTextureSize,
              jitterSize: 1
            )
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
            staggeredJitter: false,
            video: video,
            doDownscale: false,
            dispatcher: new DispatcherRSfixed(
              computeShader: csHighlightRemoval,
              numIterations: numIterations,
              doRandomizeEmptyClusters: false,
              numIterationsKM: 2,
              doReadback: false,
              clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                numClusters: 6,
                workingSize: 64,
                fullSize: ClusteringTest.fullTextureSize,
                jitterSize: 1
              )
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
          staggeredJitter: false,
          video: video,
          doDownscale: false,
          dispatcher: new DispatcherKnecht(
            computeShader: csHighlightRemoval,
            doRandomizeEmptyClusters: false,
            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
              numClusters: 6,
              workingSize: 64,
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
            computeShader: csHighlightRemoval,
            doRandomizeEmptyClusters: false,
            numIterationsKM: 2,
            clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
              numClusters: 6,
              workingSize: 64,
              fullSize: ClusteringTest.fullTextureSize,
              jitterSize: 1
            )
          )
        ).ThrowIfExists()
      );
    }
  }
}