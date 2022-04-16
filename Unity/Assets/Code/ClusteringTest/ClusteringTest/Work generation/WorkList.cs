using System.Collections.Generic;

namespace WorkGeneration
{
    public class WorkList
    {
        public readonly Stack<LaunchParameters> runs;
        public readonly ClusteringTest.LogType logType;
        public readonly string name;

        public WorkList(ClusteringTest.LogType logType, string name)
        {
            this.runs = new Stack<LaunchParameters>();
            this.logType = logType;
            this.name = name;
        }
    }
}
