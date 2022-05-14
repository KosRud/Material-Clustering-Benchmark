namespace ClusteringAlgorithms
{
    /// <summary>
    /// Runs the clustering function from the wrapped dispatcher and performs one (useless) readback after every iteration.
    /// </summary>
    public class WrapperArtificialReadback : IDispatcher
    {
        private readonly ASimpleDispatcer wrappedDispatcher;

        public WrapperArtificialReadback(ASimpleDispatcer wrappedDispatcher)
        {
            this.wrappedDispatcher = wrappedDispatcher;
            if (wrappedDispatcher.usesStopCondition)
            {
                throw new System.InvalidOperationException(
                    "WrapperArtificialReadback must be given a dispatcher, which does not use stop condition."
                );
            }
            if (wrappedDispatcher.doesReadback)
            {
                throw new System.InvalidOperationException(
                    "WrapperArtificialReadback must be given a dispatcher, which does not use readback."
                );
            }
        }

        public void Dispose()
        {
            this.wrappedDispatcher.Dispose();
        }

        public bool usesStopCondition => this.wrappedDispatcher.usesStopCondition;
        public bool doesReadback => true;

        public virtual string name => $"{this.wrappedDispatcher.name} + readback";
        public bool doRandomizeEmptyClusters => this.wrappedDispatcher.doRandomizeEmptyClusters;
        public int numIterations => this.wrappedDispatcher.numIterations;
        public ClusteringRTsAndBuffers clusteringRTsAndBuffers =>
            this.wrappedDispatcher.clusteringRTsAndBuffers;
        public DispatcherParameters parameters => this.wrappedDispatcher.parameters;

        public void UpdateClusterCenters(ClusteringTextures clusteringTextures, bool rejectOld)
        {
            this.wrappedDispatcher.UpdateClusterCenters(
                clusteringTextures: clusteringTextures,
                rejectOld: rejectOld
            );
        }

        public void AttributeClusters(ClusteringTextures clusteringTextures, bool final, bool khm)
        {
            this.wrappedDispatcher.AttributeClusters(
                clusteringTextures: clusteringTextures,
                final: final,
                khm: khm
            );
        }

        public float GetVariance()
        {
            return this.wrappedDispatcher.GetVariance();
        }

        public virtual void RunClustering(ClusteringTextures clusteringTextures)
        {
            for (int i = 0; i < this.wrappedDispatcher.numIterations; i++)
            {
                this.wrappedDispatcher.SingleIteration(clusteringTextures);

                // artificial readback
                this.wrappedDispatcher.clusteringRTsAndBuffers.GetClusterCenters().Dispose();
            }
        }
    }
}
