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
            if (wrappedDispatcher.usesStopCondition)
            {
                throw new System.InvalidOperationException(
                    "WrapperStopCondition must be given a dispatcher, which does not use stop condition."
                );
            }
        }

        public void Dispose()
        {
            this.wrappedDispatcher.Dispose();
        }

        /*
            impossible to use stop condition without readback
        */
        public bool doesReadback => true;
        public bool usesStopCondition => true;
        public int warningCounter => this.wrappedDispatcher.warningCounter;

        public virtual string name => this.wrappedDispatcher.name;
        public bool doRandomizeEmptyClusters => this.wrappedDispatcher.doRandomizeEmptyClusters;
        public int numIterations => this.wrappedDispatcher.numIterations;
        public ClusteringRTsAndBuffers clusteringRTsAndBuffers =>
            this.wrappedDispatcher.clusteringRTsAndBuffers;
        public DispatcherParameters abstractParameters => this.wrappedDispatcher.abstractParameters;

        public void UpdateClusterCenters(ClusteringTextures clusteringTextures, bool rejectOld)
        {
            this.wrappedDispatcher.UpdateClusterCenters(
                clusteringTextures: clusteringTextures,
                rejectOld: rejectOld
            );
        }

        public void AttributeClustersKM(ClusteringTextures clusteringTextures)
        {
            this.wrappedDispatcher.AttributeClustersKM(clusteringTextures: clusteringTextures);
        }

        public bool useFullResTexRef => this.wrappedDispatcher.useFullResTexRef;

        public float? GetVariance()
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

            /*
                direcrly runing getVariance is not an option
                because its implementation is not optimized
            */

            ClusterCenters clusterCenters = null;
            ClusterCenters newClusterCenters =
                this.wrappedDispatcher.clusteringRTsAndBuffers.GetClusterCenters();

            /*
                start at 2
                iteration #1 was already performed
                iteration #2 is the current one

                we need variance change between 2 iterations
                thus we can't check stop condition after the first iteration

                variance check between last iteration of the previous frame
                and the first iteration of the current frame
                makes no sense
                because the input texture changed
            */

            for (int kmIteration = 2; kmIteration < StopCondition.maxIterations; kmIteration++)
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
