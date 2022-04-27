using UnityEngine.Pool;
using UnityEngine;

namespace ClusteringAlgorithms
{
    public class WrapperStopCondition : IDispatcher
    {
        private readonly ASimpleDispatcer wrappedDispatcher;

        public WrapperStopCondition(ASimpleDispatcer wrappedDispatcher)
        {
            this.wrappedDispatcher = wrappedDispatcher;
            Debug.Assert(wrappedDispatcher.parameters.stopCondition == false);
            this.wrappedDispatcher.parameters.stopCondition = true;
        }

        public void Dispose()
        {
            this.wrappedDispatcher.Dispose();
        }

        public bool usesStopCondition => true;

        public virtual string name => this.wrappedDispatcher.name;
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

        public class RunUntilConvergesResult : System.IDisposable
        {
            public ClusterCenters clusterCenters;
            public bool converged;

            public static readonly IObjectPool<RunUntilConvergesResult> pool =
                new ObjectPoolMaxAssert<RunUntilConvergesResult>(
                    createFunc: () => new RunUntilConvergesResult(),
                    maxActive: 2
                );

            private RunUntilConvergesResult() { }

            public void Dispose()
            {
                this.clusterCenters.Dispose();
                pool.Release(this);
            }

            public static RunUntilConvergesResult Get(ClusterCenters clusterCenters, bool converged)
            {
                RunUntilConvergesResult obj = pool.Get();
                obj.converged = converged;
                obj.clusterCenters = clusterCenters;

                return obj;
            }
        }

        public virtual void RunClustering(ClusteringTextures clusteringTextures)
        {
            this.RunUntilConverges(clusteringTextures).Dispose();
        }

        public RunUntilConvergesResult RunUntilConverges(ClusteringTextures clusteringTextures)
        {
            this.wrappedDispatcher.SingleIteration(clusteringTextures);

            ClusterCenters clusterCenters = null;
            ClusterCenters newClusterCenters =
                this.wrappedDispatcher.clusteringRTsAndBuffers.GetClusterCenters();

            /*
                start at 1
                because 1 iteration was already performed

                we need variance change between 2 iterations
                thus we can't check stop condition after the first iteration

                variance check between last iteration of the previous frame
                and the first iteration of the current frame
                makes no sense
                because the input texture changed
            */

            for (int kmIteration = 1; kmIteration < StopCondition.maxKMiterations; kmIteration++)
            {
                /*
                    * dispose previous cluster centers
                    * (only hold one instance at a time)
                */
                clusterCenters?.Dispose();
                clusterCenters = newClusterCenters;

                this.wrappedDispatcher.SingleIteration(clusteringTextures);

                newClusterCenters =
                    this.wrappedDispatcher.clusteringRTsAndBuffers.GetClusterCenters();

                if (
                    clusterCenters.variance - newClusterCenters.variance
                    < StopCondition.varianceChangeThreshold
                )
                {
                    // * dispose latest cluster centers
                    clusterCenters.Dispose();
                    return RunUntilConvergesResult.Get(
                        converged: true,
                        clusterCenters: newClusterCenters
                    );
                }
            }

            // * dispose latest cluster centers
            clusterCenters.Dispose();
            return RunUntilConvergesResult.Get(converged: false, clusterCenters: newClusterCenters);
        }
    }
}
