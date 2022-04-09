using UnityEngine;
using System.Collections.Generic;

namespace ClusteringAlgorithms {

  public class ClusterCenters : System.IDisposable {
    public const int invalidVariance =
      10; // 2-dimensional values in range [0, 1] => 10 is impossibly large

    public Vector4[] centers;
    public float variance;

    private readonly int numClusters;

    private static readonly
    Dictionary<int, UnityEngine.Pool.IObjectPool<ClusterCenters>> pools =
        new Dictionary<int, UnityEngine.Pool.IObjectPool<ClusterCenters>>();

    private static UnityEngine.Pool.IObjectPool<ClusterCenters> GetPool(
      int numClusters) {
      if (pools.ContainsKey(numClusters) == false) {
        int localNumClusters =
          numClusters; // local copy prevent variable capture in lambda
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

    private ClusterCenters(int numClusters) {
      this.numClusters = numClusters;
      this.centers = new Vector4[this.numClusters * 2];
    }

    public void Dispose() {
      pools[this.numClusters].Release(this);
    }

    /// <summary>
    /// Gets a pooled instance of ClusterCenters. The data is copied, making is safe to edit. Don't forget to dispose!
    /// </summary>
    /// <returns></returns>
    public static ClusterCenters Get(int numClusters, Vector4[] centersBufferData) {
      ClusterCenters obj = GetPool(numClusters).Get();

      centersBufferData.CopyTo(obj.centers, 0);

      foreach (Vector4 center in centersBufferData) {
        if (center.z < invalidVariance) {
          obj.variance = center.z;
          return obj;
        }
      }

      throw new InvalidClustersException("all clusters are invalid");
    }

    public class InvalidClustersException : System.Exception {
      public InvalidClustersException() : base() {

      }

      public InvalidClustersException(string message) : base(message) {

      }
    }
  }
}