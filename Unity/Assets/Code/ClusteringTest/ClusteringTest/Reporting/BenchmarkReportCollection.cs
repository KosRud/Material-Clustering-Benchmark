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
public abstract class ABenchmarkMeasurement { }

[Serializable]
public class BenchmarkMeasurementVariance : ABenchmarkMeasurement
{
    [Serializable]
    public class FrametVariance
    {
        public long frameIdex;
        public float variance;

        public FrametVariance(long frameIndex, float variance)
        {
            this.frameIdex = frameIndex;
            this.variance = variance;
        }
    }

    public List<FrametVariance> varianceByFrame;

    public BenchmarkMeasurementVariance()
    {
        this.varianceByFrame = new List<FrametVariance>();
    }
}

[Serializable]
public class BenchmarkMeasurementFrameTime : ABenchmarkMeasurement
{
    public float peakFrameTime;
    public float avgFrameTime;

    public BenchmarkMeasurementFrameTime(float peakFrameTime, float avgFrameTime)
    {
        this.peakFrameTime = peakFrameTime;
        this.avgFrameTime = avgFrameTime;
    }
}

[Serializable]
public class BenchmarkReport
{
    [SerializeReference]
    public ABenchmarkMeasurement measurement;

    [SerializeReference]
    public LaunchParameters.SerializableLaunchParameters serializableLaunchParameters;

    public ClusteringTest.LogType logType;

    public BenchmarkReport(
        ABenchmarkMeasurement measurement,
        LaunchParameters.SerializableLaunchParameters serializableLaunchParameters,
        ClusteringTest.LogType logType
    )
    {
        this.measurement = measurement;
        this.serializableLaunchParameters = serializableLaunchParameters;
        this.logType = logType;
    }
}

[Serializable]
public class BenchmarkReportCollection
{
    public List<BenchmarkReport> reports;

    public BenchmarkReportCollection()
    {
        this.reports = new List<BenchmarkReport>();
    }
}
