namespace ClusteringAlgorithms
{
    public static class StopCondition
    {
        public const float varianceChangeThreshold = 1e-4f;
        public const int maxConsecutiveFailedSwaps = 2;
        public const int maxKMiterations = 20;
    }
}
