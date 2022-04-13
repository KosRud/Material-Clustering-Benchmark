using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration {

  public class Subsampling : AWorkGenerator {

    public Subsampling(
      int kernelSize,
      UnityEngine.Video.VideoClip[] videos,
      ComputeShader csHighlightRemoval
    ) : base(
        kernelSize: kernelSize,
        videos: videos,
        csHighlightRemoval: csHighlightRemoval
      ) { }

    public override WorkList GenerateWork() {
      var workList = new WorkList(ClusteringTest.LogType.Variance);

      foreach (UnityEngine.Video.VideoClip video in this.videos) {
        /*
          ! lowest textureSize must be no less, than kernel size
        */
        for (int textureSize = 512; textureSize >= 8; textureSize /= 2) {
          foreach (int numClusters in new int[] { 4, 6, 8, 12, 16 }) {
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
                    numClusters: numClusters,
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