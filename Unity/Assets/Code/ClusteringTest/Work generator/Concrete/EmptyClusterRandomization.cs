using UnityEngine;
using ClusteringAlgorithms;

namespace WorkGenerator {

public class EmptyClusterRandomization : AWorkGenerator {

  public EmptyClusterRandomization(
    int kernelSize,
    UnityEngine.Video.VideoClip[] videos,
    ComputeShader csHighlightRemoval
  ) : base(
      kernelSize: kernelSize,
      videos: videos,
      csHighlightRemoval: csHighlightRemoval) { }

  public override void GenerateWork(
    System.Collections.Generic.Stack<ClusteringTest.LaunchParameters> workStack
  ) {
    foreach (UnityEngine.Video.VideoClip video in this.videos) {
      for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
        foreach (
          bool doRandomizeEmptyClusters in new bool[] { true, false }
        ) {
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
                numIterations: 3,
                doRandomizeEmptyClusters: doRandomizeEmptyClusters,
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