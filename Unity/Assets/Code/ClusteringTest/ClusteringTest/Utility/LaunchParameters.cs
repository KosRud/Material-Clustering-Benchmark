
using ClusteringAlgorithms;
using System;
using UnityEngine;

namespace WorkGeneration {

  public class LaunchParameters {
    [Serializable]
    private class SerializableLaunchParameters {
      public readonly string videoName;
      public readonly int numIterations;
      public readonly int workingTextureSize;
      public readonly int numClusters;
      public readonly int jitterSize;
      public readonly bool staggeredJitter;
      public readonly bool doDownscale;
      public readonly string algorithm;
      public readonly bool doRandomizeEmptyClusters;

      public string GetJson() {
        return JsonUtility.ToJson(this);
      }

      public SerializableLaunchParameters(
        string videoName,
        int numIterations,
        int workingTextureSize,
        int numClusters,
        int jitterSize,
        bool staggeredJitter,
        bool doDownscale,
        string algorithm,
        bool doRandomizeEmptyClusters
      ) {
        this.videoName = videoName;
        this.numIterations = numIterations;
        this.workingTextureSize = workingTextureSize;
        this.numClusters = numClusters;
        this.jitterSize = jitterSize;
        this.staggeredJitter = staggeredJitter;
        this.doDownscale = doDownscale;
        this.algorithm = algorithm;
        this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
      }
    }

    public string GetFileName() {
      string json = new SerializableLaunchParameters(
        videoName: this.video.name,
        numIterations: this.dispatcher.numIterations,
        workingTextureSize:
        this.dispatcher.clusteringRTsAndBuffers.texturesWorkRes.size,
        numClusters: this.dispatcher.clusteringRTsAndBuffers.numClusters,
        jitterSize: this.dispatcher.clusteringRTsAndBuffers.jitterSize,
        staggeredJitter: this.staggeredJitter,
        doDownscale: this.doDownscale,
        algorithm: this.dispatcher.descriptionString,
        doRandomizeEmptyClusters: this.dispatcher.doRandomizeEmptyClusters
      ).GetJson();

      Debug.Log(json);

      string videoName = this.video.name;
      int numIterations = this.dispatcher.numIterations;
      int workingTextureSize =
        this.dispatcher.clusteringRTsAndBuffers.texturesWorkRes.size;
      int numClusters = this.dispatcher.clusteringRTsAndBuffers.numClusters;
      int jitterSize =
        this.dispatcher.clusteringRTsAndBuffers.jitterSize;
      bool staggeredJitter = this.staggeredJitter;
      bool doDownscale = this.doDownscale;
      string algorithm = this.dispatcher.descriptionString;
      bool doRandomizeEmptyClusters = this.dispatcher.doRandomizeEmptyClusters;

      return $"video file:{videoName}|number of iterations:{numIterations}|texture size:{workingTextureSize}|number of clusters:{numClusters}|randomize empty clusters:{doRandomizeEmptyClusters}|jitter size:{jitterSize}|staggered jitter:{staggeredJitter}|downscale:{doDownscale}|algorithm:{algorithm}.csv";
    }

    public LaunchParameters ThrowIfExists() {
      string fileName = $"{ClusteringTest.varianceLogPath}/{this.GetFileName()}";

      if (System.IO.File.Exists(fileName)) {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        throw new System.Exception($"File exists: {fileName}");
      }

      return this;
    }

    public readonly bool staggeredJitter;
    public readonly UnityEngine.Video.VideoClip video;
    public readonly bool doDownscale;
    public readonly ADispatcher dispatcher;

    public LaunchParameters(
      bool staggeredJitter,
      UnityEngine.Video.VideoClip video,
      bool doDownscale,
      ADispatcher dispatcher
    ) {
      this.staggeredJitter = staggeredJitter;
      this.video = video;
      this.doDownscale = doDownscale;
      this.dispatcher = dispatcher;
    }
  }
}