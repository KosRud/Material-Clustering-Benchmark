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

  public class ClusteringRTsAndBuffers {
    public const int max_num_clusters = 32;
    public const bool randomInit = false;

    public readonly RenderTexture rtArr;
    public readonly RenderTexture rtVariance;
    public readonly ComputeBuffer cbufClusterCenters;
    public readonly ComputeBuffer cbufRandomPositions;
    public readonly Texture texReference;
    public readonly int numClusters;

    /// <summary>
    /// Get a pooled instance of ClusterCenters with a copy of the data from ComputeBuffer. Safe to modify. Don't forget to dispose!
    /// </summary>
    /// <returns></returns>
    public ClusterCenters GetClusterCenters() {
      this.cbufClusterCenters.GetData(this.clusterCentersBufferData);
      return ClusterCenters.Get(this.numClusters, this.clusterCentersBufferData);
    }

    /// <summary>
    /// Reads the cluster centers data from the array and loads it into the ComputeBuffer.
    /// </summary>
    /// <param name="clusterCentersBufferData"></param>
    public void SetClusterCenters(Vector4[] clusterCentersBufferData) {
      this.cbufClusterCenters.SetData(clusterCentersBufferData);
    }

    private readonly Vector4[] clusterCentersBufferData;
    private readonly Position[] randomPositions;
    private readonly System.Random random;

    private struct Position {
      public int x;
      public int y;

      /*
          NVidia says structures not aligned to 128 bits are slow
          https://developer.nvidia.com/content/understanding-structured-buffer-performance
      */
      private readonly int padding_1;
      private readonly int padding_2;
    }

    public int PickRandomCluster(int numClusters) {
      return this.random.Next(numClusters);
    }

    public void UpdateRandomPositions(int textureSize) {
      for (int k = 0; k < this.randomPositions.Length; k++) {
        this.randomPositions[k].x = this.random.Next(textureSize);
        this.randomPositions[k].y = this.random.Next(textureSize);
      }
      this.cbufRandomPositions.SetData(this.randomPositions);
    }

    public ClusteringRTsAndBuffers(
      int numClusters,
      int textureSize,
      int referenceTextureSize,
      Texture texReference
    ) {
      this.numClusters = numClusters;

      this.random = new System.Random();

      this.cbufRandomPositions = new ComputeBuffer(max_num_clusters, sizeof(int) * 4);
      this.randomPositions = new Position[max_num_clusters];
      this.UpdateRandomPositions(textureSize);

      this.texReference = texReference;

      var rtDesc = new RenderTextureDescriptor(
        textureSize,
        textureSize,
        RenderTextureFormat.ARGBFloat,
        0
      ) {
        dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
        volumeDepth = 16,
        useMipMap = true,
        autoGenerateMips = false
      };

      this.rtArr = new RenderTexture(rtDesc) {
        enableRandomWrite = true
      };

      this.rtVariance = new RenderTexture(
        referenceTextureSize,
        referenceTextureSize,
        0,
        RenderTextureFormat.RGFloat
      ) {
        enableRandomWrite = true,
        useMipMap = true,
        autoGenerateMips = false
      };

      /*
          second half of the buffer contains candidate cluster centers
          first half contains current cluster centers

          NVidia says structures not aligned to 128 bits are slow
          https://developer.nvidia.com/content/understanding-structured-buffer-performance
      */
      this.cbufClusterCenters = new ComputeBuffer(this.numClusters * 2,
        sizeof(float) * 4);
      this.clusterCentersBufferData = new Vector4[this.numClusters * 2];
      if (randomInit) {
        this.RandomizeClusterCenters();
      } else {
        this.DeterministicClusterCenters();
      }
    }

    public void DeterministicClusterCenters() {
      for (int i = 0; i < this.numClusters; i++) {
        var c = Color.HSVToRGB(
            i / (float)(this.numClusters),
            1,
            1
          );
        c *= 1.0f / (c.r + c.g + c.b);

        // variance infinity to ensure new cluster centers will replace these ones
        this.clusterCentersBufferData[i] = new Vector4(c.r, c.g, Mathf.Infinity,
          0); // "new"
        this.clusterCentersBufferData[i + this.numClusters] = new Vector4(c.r, c.g,
          Mathf.Infinity, 0); // "old"
      }
      this.cbufClusterCenters.SetData(this.clusterCentersBufferData);
    }

    public void RandomizeClusterCenters() {
      for (int i = 0; i < this.numClusters; i++) {
        var c = Color.HSVToRGB(
            (float)this.random.NextDouble(),
            1,
            1
          );
        c *= 1.0f / (c.r + c.g + c.b);

        // variance infinity to ensure new cluster centers will replace these ones
        this.clusterCentersBufferData[i] = new Vector4(c.r, c.g, Mathf.Infinity,
          0); // "new"
        this.clusterCentersBufferData[i + this.numClusters] = new Vector4(c.r, c.g,
          Mathf.Infinity, 0); // "old"
      }
      this.cbufClusterCenters.SetData(this.clusterCentersBufferData);
    }

    public void Release() {
      this.rtArr.Release();
      this.rtVariance.Release();
      this.cbufClusterCenters.Release();
      this.cbufRandomPositions.Release();
    }
  }
}