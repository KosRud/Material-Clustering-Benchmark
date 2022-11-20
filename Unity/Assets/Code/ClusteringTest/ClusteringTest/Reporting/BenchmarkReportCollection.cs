using System;
using System.Collections.Generic;
using BenchmarkGeneration;
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
    public class FrameVarianceRecord
    {
        public long frameIndex;
        public float variance;

        public FrameVarianceRecord(long frameIndex, float? variance)
        {
            this.frameIndex = frameIndex;
            this.variance = variance ?? -1;
        }
    }

    public List<FrameVarianceRecord> frameVarianceRecords;

    public BenchmarkMeasurementVariance()
    {
        this.frameVarianceRecords = new List<FrameVarianceRecord>();
    }
}

[Serializable]
public class BenchmarkMeasurementFrameTime : ABenchmarkMeasurement
{
    [Serializable]
    public class FrameTimeRecord
    {
        public long frameIndex;
        public float time;

        public FrameTimeRecord(long frameIndex, float time)
        {
            this.frameIndex = frameIndex;
            this.time = time;
        }
    }

    public List<FrameTimeRecord> frameTimeRecords;

    public BenchmarkMeasurementFrameTime()
    {
        this.frameTimeRecords = new List<FrameTimeRecord>();
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
