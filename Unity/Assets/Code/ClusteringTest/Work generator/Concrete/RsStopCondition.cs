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
                staggeredJitter: false,
                video: video,
                doDownscale: false,
                dispatcher: new DispatcherRSstopCondition(
                  computeShader: this.csHighlightRemoval,
                  doRandomizeEmptyClusters: false,
                  numIterationsKM: 2,
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