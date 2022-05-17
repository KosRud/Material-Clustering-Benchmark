namespace ClusteringAlgorithms
{
    public static class StopCondition
    {
        public const float varianceChangeThreshold = 1e-4f;
        public const int maxFailedSwaps = 1;
        public const int maxIterations = 20;
    }
}
