using System;

namespace ClusteringAlgorithms
{
    public interface IDispatcher : IDisposable
    {
        bool doRandomizeEmptyClusters { get; }
        int numIterations { get; }
        ClusteringRTsAndBuffers clusteringRTsAndBuffers { get; }

        DispatcherParameters parameters { get; }

        string name { get; }

        bool usesStopCondition { get; }
        bool doesReadback { get; }
        bool useFullResTexRef { get; }

        void RunClustering(ClusteringTextures clusteringTextures);

        void UpdateClusterCenters(ClusteringTextures textures, bool rejectOld);

        void AttributeClusters(ClusteringTextures textures, bool khm);

        /// <summary>
        /// Computes variance for the current cluster centers. Depending on <see cref="this.useFullResTexRef"/> uses either working resolution (<see cref="this.clusteringRTsAndBuffers.texturesWorkRes"/>), or full resolution (<see cref="this.clusteringRTsAndBuffers.texturesFullRes"/>) input.
        /// </summary>
        float? GetVariance();
    }
}
