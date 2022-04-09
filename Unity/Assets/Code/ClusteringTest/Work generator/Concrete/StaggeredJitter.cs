using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGenerator {
  public class StaggeredJitter : AWorkGenerator {

    public StaggeredJitter(
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
        for (int textureSize = 64; textureSize >= 4; textureSize /= 2) {
          for (
            int jitterSize = 1;
            jitterSize <= 16 && jitterSize * textureSize <= 64;
            jitterSize *= 2
          ) {
            workStack.Push(
              new ClusteringTest.LaunchParameters(
                staggeredJitter: false,
                video: video,
                doDownscale: false,
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