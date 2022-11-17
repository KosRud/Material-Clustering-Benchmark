using UnityEngine;
using System.Collections.Generic;

namespace ClusteringAlgorithms
{
    public class ClusterCenters : System.IDisposable
    {
        public Vector4[] centers;
        public float? variance;
        public float? oldVariance;

        private readonly int numClusters;

        private static readonly Dictionary<
            int,
            UnityEngine.Pool.IObjectPool<ClusterCenters>
        > pools = new Dictionary<int, UnityEngine.Pool.IObjectPool<ClusterCenters>>();

        private static UnityEngine.Pool.IObjectPool<ClusterCenters> GetPool(int numClusters)
        {
            if (pools.ContainsKey(numClusters) == false)
            {
                int localNumClusters = numClusters; // local copy prevent variable capture in lambda
                pools.Add(
                    numClusters,
                    new ObjectPoolMaxAssert<ClusterCenters>(
                        createFunc: () => new ClusterCenters(localNumClusters),
                        maxActive: 4
                    )
                );
            }

            return pools[numClusters];
        }

        private ClusterCenters(int numClusters)
        {
            this.numClusters = numClusters;
            this.centers = new Vector4[this.numClusters * 2];
        }

        public void Dispose()
        {
            pools[this.numClusters].Release(this);
        }

        private static bool AreAllClusterCentersEmpty(int numClusters, Vector4[] centersBufferData)
        {
            for (int i = 0; i < numClusters; i++)
            {
                Vector4 center = centersBufferData[i];

                // 1 = not empty
                // 0 = empty
                if (center.w > 0.5)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets a pooled instance of ClusterCenters. The data is copied, making is safe to edit. Don't forget to dispose!
        /// </summary>
        /// <returns></returns>
        public static ClusterCenters Get(int numClusters, Vector4[] centersBufferData)
        {
            ClusterCenters clusterCenters = GetPool(numClusters).Get();

            centersBufferData.CopyTo(clusterCenters.centers, 0);

            for (int i = 0; i < numClusters; i++)
            {
                Vector4 center = centersBufferData[i];

                if (float.IsNaN(center.x) || float.IsNaN(center.y))
                {
                    LogClusterCenters(numClusters, centersBufferData);
                    throw new System.Exception("NaN in shader");
                }

                if (center.x < -0.5 || center.x > 0.5 || center.y < -0.5 || center.y > 0.5)
                {
                    LogClusterCenters(numClusters, centersBufferData);
                    throw new System.Exception($"invalid cluster center record: {center}");
                }
            }

            /*
                positive number = valid variance
                -1 = not a single pixel has sufficient chromatic component

                |0              |numClusters
                |---------------|---------------|
            */
            if (centersBufferData[numClusters].z < -0.5)
            {
                // not a single pixel has sufficient chromatic component
                clusterCenters.oldVariance = null;
            }
            else
            {
                clusterCenters.oldVariance = centersBufferData[numClusters].z;
            }

            /*
                positive number = valid variance
                -1 = not a single pixel has sufficient chromatic component

                |0              |numClusters
                |---------------|---------------|
            */
            if (centersBufferData[0].z < -0.5)
            {
                // not a single pixel has sufficient chromatic component
                clusterCenters.variance = null;
                Debug.Log("Not a single pixel has sufficient chromatic component");
                return clusterCenters;
            }
            else
            {
                clusterCenters.variance = centersBufferData[0].z;
            }

            if (!AreAllClusterCentersEmpty(numClusters, centersBufferData))
            {
                LogClusterCenters(numClusters, centersBufferData);
                Debug.Log("All cluster centers are empty");
            }
            return clusterCenters;
        }

        private static void LogClusterCenters(int numClusters, Vector4[] centersBufferData)
        {
            for (int i = 0; i < numClusters; i++)
            {
                Vector4 center = centersBufferData[i];

                Debug.Log(center);
            }
        }

        public class InvalidClustersException : System.Exception
        {
            public InvalidClustersException() : base() { }

            public InvalidClustersException(string message) : base(message) { }
        }
    }
}
