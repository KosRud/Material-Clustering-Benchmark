using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration {
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
      Stack<LaunchParameters> workStack
    ) {
      foreach (UnityEngine.Video.VideoClip video in this.videos) {
        for (int textureSize = 256; textureSize >= 8; textureSize /= 2) {
          foreach (bool doDownscale in new bool[] { true, false }) {
            workStack.Push(
              new LaunchParameters(
                staggeredJitter: false,
                video: video,
                doDownscale: doDownscale,
                dispatcher: new DispatcherKM(
                  computeShader: this.csHighlightRemoval,
                  numIterations: 3,
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
        }
      }
    }

  }
}