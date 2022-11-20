namespace ClusteringAlgorithms
{
    public static class StopCondition
    {
        public const float varianceChangeThreshold = 1e-4f;

        /// <summary>
        /// Stop when the number of failed swaps exceeds this value.
        /// </summary>
        public const int maxFailedSwaps = 0;
        public const int maxIterations = 20;
    }
}
