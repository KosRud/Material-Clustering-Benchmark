using System.Collections.Generic;

namespace WorkGeneration {
  public class WorkList {
    public readonly Stack<LaunchParameters> runs;
    public readonly ClusteringTest.LogType logType;

    public WorkList(
      ClusteringTest.LogType logType
    ) {
      this.runs = new Stack<LaunchParameters>();
      this.logType = logType;
    }
  }
}