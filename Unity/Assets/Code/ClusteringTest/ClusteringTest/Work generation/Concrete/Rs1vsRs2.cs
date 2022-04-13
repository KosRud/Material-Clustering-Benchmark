using UnityEngine;
using System.Collections.Generic;
using ClusteringAlgorithms;

namespace WorkGeneration {
  public class Rs1VsRs2 : AWorkGenerator {

    public Rs1VsRs2(
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
        for (
          int numIterations = 1;
          numIterations < 31;
          numIterations++
        ) {
          foreach (
            int numIterationsKM in new int[1, 2]
          ) {
            this.AddRs(
              workList: workList,
              video: video,
              numIterations: numIterations,
              csHighlightRemoval: this.csHighlightRemoval,
              numIterationsKM: numIterationsKM
            );
          }
        }
      }

      return workList;
    }

    private void AddRs(
      WorkList workList,
      UnityEngine.Video.VideoClip video,
      int numIterations,
      ComputeShader csHighlightRemoval,
      int numIterationsKM
    ) {
      if (
        DispatcherRSfixed.IsNumIterationsValid(
          iterations: numIterations,
          iterationsKM: numIterationsKM
        )
      ) {
        workList.runs.Push(
          new LaunchParameters(
            staggeredJitter: false,
            video: video,
            doDownscale: false,
            dispatcher: new DispatcherRSfixed(
              computeShader: csHighlightRemoval,
              numIterations: numIterations,
              doRandomizeEmptyClusters: false,
              numIterationsKM: numIterationsKM,
              doReadback: false,
              clusteringRTsAndBuffers: new ClusteringRTsAndBuffers(
                numClusters: 6,
                workingSize: 64,
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