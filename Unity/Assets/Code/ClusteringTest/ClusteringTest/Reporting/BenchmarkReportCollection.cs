using System;
using System.Collections.Generic;
using WorkGeneration;
using UnityEngine;

/*
  BenchmarkReportCollection {
    reports : [

      {
        launchParameters,
        logType
        measurement : {
          varianceByFrame: [
            {
              frame,
              variance
            },
            ...
          ]
        },
      },

      {
        launchParameters,
        logType
        measurement : {
          peakFrameTime,
          avgFrameTime
        }
      },

      ...
    ]
  }
*/

[Serializable]
public abstract class ABenchmarkMeasurement {}

[Serializable]
public class BenchmarkMeasurementVariance : ABenchmarkMeasurement {
  [Serializable]
  public class FrametVariance {
    public int frameIdex;
    public float variance;

    public FrametVariance (int frameIndex, float variance) {
      this.frameIdex = frameIndex;
      this.variance = variance;
    }
  }

  public List<FrametVariance> varianceByFrame;

  public BenchmarkMeasurementVariance() {
    this.varianceByFrame = new List<FrametVariance>();
  }
}

[Serializable]
public class BenchmarkMeasurementFrameTime : ABenchmarkMeasurement {
  public float peakFrameTime;
  public float avgFrameTime;

  public BenchmarkMeasurementFrameTime(float peakFrameTime, float avgFrameTime) {
    this.peakFrameTime = peakFrameTime;
    this.avgFrameTime = avgFrameTime;
  }
}

[Serializable]
public class BenchmarkReport {
  [SerializeReference]
  public ABenchmarkMeasurement measurement;
  public LaunchParameters launchParameters;
  public ClusteringTest.LogType logType;

  public BenchmarkReport(
    ABenchmarkMeasurement measurement,
    LaunchParameters launchParameters,
    ClusteringTest.LogType logType
  ) {
    this.measurement = measurement;
    this.launchParameters = launchParameters;
    this.logType = logType;
  }
}

[Serializable]
public class BenchmarkReportCollection {
  public List<BenchmarkReport> reports;

  public BenchmarkReportCollection(List<BenchmarkReport> reports) {
    this.reports = reports;
  }
}