using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration {
  public class ScanlineJitter : AWorkGenerator {

    public ScanlineJitter(
      int kernelSize,
      UnityEngine.Video.VideoClip[] videos,
      ComputeShader csHighlightRemoval
    ) : base(
        kernelSize: kernelSize,
        videos: videos,
        csHighlightRemoval: csHighlightRemoval) { }

    public override WorkList GenerateWork() {
      var workList = new WorkList(ClusteringTest.LogType.Variance);

      foreach (UnityEngine.Video.VideoClip video in this.videos) {
        for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
          for (
            int jitterSize = 1;
            jitterSize * textureSize <= 512 && jitterSize <= 16;
            jitterSize *= 2
          ) {
            workList.runs.Push(
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

      return workList;
    }
  }
}