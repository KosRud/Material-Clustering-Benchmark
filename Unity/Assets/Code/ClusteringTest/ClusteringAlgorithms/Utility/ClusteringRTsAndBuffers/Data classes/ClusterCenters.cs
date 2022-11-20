using UnityEngine;
using System.Collections.Generic;

namespace ClusteringAlgorithms
{
    public class ClusterCenters : System.IDisposable
    {
        public Vector4[] centers;
        public float? variance;
        public float? oldVariance;
        public bool warning;

        private readonly int numClusters;

        private static System.Text.StringBuilder sb = new System.Text.StringBuilder("", 4096);

        private static readonly Dictionary<
            int,
            UnityEngine.Pool.IObjectPool<ClusterCenters>
        > pools = new Dictionary<int, UnityEngine.Pool.IObjectPool<ClusterCenters>>();

        private static UnityEngine.Pool.IObjectPool<ClusterCenters> GetPool(int numClusters)
        {
            if (pools.ContainsKey(numClusters) == false)
            {
                int localNumClusters = numClusters; // local copy to prevent variable capture in lambda
                pools.Add(
                    numClusters,
                    new ObjectPoolMaxAssert<ClusterCenters>(
                        createFunc: () => new ClusterCenters(localNumClusters), // lambda
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
            this.warning = false;
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
            clusterCenters.warning = false;

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
                z = ...

                positive number 	==	valid variance
                -1 					==	not a single pixel has sufficient chromatic component

                |0              |numClusters	|
                |---------------|---------------|
                |  new centers	| old centers	|
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
                z = ...
                
                positive number = valid variance
                -1 = not a single pixel has sufficient chromatic component

                |0              |numClusters	|
                |---------------|---------------|
                |  new centers	| old centers	|
            */
            if (centersBufferData[0].z < -0.5)
            {
                // not a single pixel has sufficient chromatic component
                clusterCenters.variance = null;
                clusterCenters.warning = true;
                Debug.LogWarning("Not a single pixel has sufficient chromatic component");
                return clusterCenters;
            }
            else
            {
                clusterCenters.variance = centersBufferData[0].z;
            }

            if (AreAllClusterCentersEmpty(numClusters, centersBufferData))
            {
                clusterCenters.warning = true;
                Debug.LogWarning("All cluster centers are empty!");
                LogClusterCenters(numClusters, centersBufferData);
            }

            return clusterCenters;
        }

        public static void LogClusterCenters(int numClusters, Vector4[] centersBufferData)
        {
            sb.Clear();
            sb.AppendLine("New cluster records: \n");
            for (int i = 0; i < numClusters; i++)
            {
                sb.Append($"{i, 4}  |  ");
                sb.AppendLine(centersBufferData[i].ToString());
            }
            sb.AppendLine("\nOld cluster records: \n");
            for (int i = numClusters; i < numClusters * 2; i++)
            {
                sb.Append($"{i, 4}  |  ");
                sb.AppendLine(centersBufferData[i].ToString());
            }
            Debug.Log(sb);
        }

        public class InvalidClustersException : System.Exception
        {
            public InvalidClustersException() : base() { }

            public InvalidClustersException(string message) : base(message) { }
        }
    }
}
