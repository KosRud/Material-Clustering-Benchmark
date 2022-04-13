using UnityEngine;
using ClusteringAlgorithms;
using System.Collections.Generic;

namespace WorkGeneration {
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
      Stack<LaunchParameters> workStack
    ) {
      foreach (UnityEngine.Video.VideoClip video in this.videos) {
        for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
          for (
            int jitterSize = 1;
            jitterSize * textureSize <= 512 && jitterSize <= 16;
            jitterSize *= 2
          ) {
            workStack.Push(
              new LaunchParameters(
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
                    jitterSize: jitterSize
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