using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGenerator {
  public class ScalingVsSubsampling : AWorkGenerator {

    public ScalingVsSubsampling(
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
        for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
          foreach (bool doDownscale in new bool[] { true, false }) {
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                workingTextureSize: textureSize,
                staggeredJitter: false,
                jitterSize: 1,
                video: video,
                doDownscale: doDownscale,
                dispatcher: new DispatcherKM(
                  kernelSize: this.kernelSize,
                  computeShader: this.csHighlightRemoval,
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

  }
}