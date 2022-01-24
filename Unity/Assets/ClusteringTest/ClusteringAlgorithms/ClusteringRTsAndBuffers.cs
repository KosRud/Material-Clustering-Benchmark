using UnityEngine;

public class ClusteringRTsAndBuffers {
    public const int max_num_clusters = 32;

    public RenderTexture rtArr;
    public RenderTexture rtVariance;
    public ComputeBuffer cbufClusterCenters;
    public ComputeBuffer cbufRandomPositions;

    private Position[] randomPositions;
    private readonly System.Random random;

    private readonly Vector4[] _clusterCenters;
    public Vector4[] clusterCenters {
        get {
            this.cbufClusterCenters.GetData(this._clusterCenters);
            return this._clusterCenters;
        }
        set => this.cbufClusterCenters.SetData(value);
    }

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

    public ClusteringRTsAndBuffers(int numClusters, int textureSize, int referenceTextureSize) {
        this.random = new System.Random();

        this.cbufRandomPositions = new ComputeBuffer(max_num_clusters, sizeof(int) * 4);
        this.randomPositions = new Position[max_num_clusters];
        this.UpdateRandomPositions(textureSize);

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
        this.cbufClusterCenters = new ComputeBuffer(numClusters * 2, sizeof(float) * 4);

        this._clusterCenters = new Vector4[numClusters * 2];

        for (int i = 0; i < this._clusterCenters.Length; i++) {
            // "old" cluster centers with infinite Variance
            // to make sure new ones will overwrite them when validated
            var c = Color.HSVToRGB(
                i / (float)(numClusters),
                1,
                1
            );
            c *= 1.0f / (c.r + c.g + c.b);
            this.clusterCenters[i] = new Vector4(c.r, c.g, Mathf.Infinity, 0);
        }
        this.cbufClusterCenters.SetData(this.clusterCenters);
    }

    public void Release() {
        this.rtArr.Release();
        this.rtVariance.Release();
        this.cbufClusterCenters.Release();
    }
}