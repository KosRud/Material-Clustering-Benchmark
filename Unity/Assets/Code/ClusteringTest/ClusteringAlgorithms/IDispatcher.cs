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

        void RunClustering(ClusteringTextures clusteringTextures);

        void UpdateClusterCenters(ClusteringTextures textures, bool rejectOld);

        void AttributeClusters(ClusteringTextures textures, bool final, bool khm);

        /// <summary>
        /// Computes variance on full-resolution input texture, without thresholding of dark pixels.
        /// </summary>
        float GetVariance();
    }
}
