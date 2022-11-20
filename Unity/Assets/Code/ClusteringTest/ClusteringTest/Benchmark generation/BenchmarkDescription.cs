using System.Collections.Generic;

namespace BenchmarkGeneration
{
    public class BenchmarkDescription
    {
        public readonly Stack<LaunchParameters> dispatches;
        public readonly ClusteringTest.LogType logType;
        public readonly string name;

        public BenchmarkDescription(ClusteringTest.LogType logType, string name)
        {
            this.dispatches = new Stack<LaunchParameters>();
            this.logType = logType;
            this.name = name;
        }
    }
}
