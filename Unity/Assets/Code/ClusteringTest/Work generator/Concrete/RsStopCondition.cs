using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGenerator {
  public class RsStopCondition : AWorkGenerator {

    public RsStopCondition(
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
            // RS stop condition
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                workingTextureSize: textureSize,
                staggeredJitter: false,
                jitterSize: 1,
                video: video,
                doDownscale: false,
                dispatcher: new DispatcherRSstopCondition(
                  kernelSize: this.kernelSize,
                  computeShader: this.csHighlightRemoval,
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