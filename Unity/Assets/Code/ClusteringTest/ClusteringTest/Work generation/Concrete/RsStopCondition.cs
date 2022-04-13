using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration {
  public class RsStopCondition : AWorkGenerator {

    public RsStopCondition(
      int kernelSize,
      UnityEngine.Video.VideoClip[] videos,
      ComputeShader csHighlightRemoval
    ) : base(
        kernelSize: kernelSize,
        videos: videos,
        csHighlightRemoval: csHighlightRemoval) { }

    public override WorkList GenerateWork() {
      var workList = new WorkList(ClusteringTest.LogType.Variance);

      for (int i = 0; i < 5; i++) {
        foreach (UnityEngine.Video.VideoClip video in this.videos) {
          foreach (int textureSize in new int[] { 512, 64 }) {
            // RS stop condition
            workList.runs.Push(
              new LaunchParameters(
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

      return workList;
    }
  }
}