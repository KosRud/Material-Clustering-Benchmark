using UnityEngine;

namespace ClusteringAlgorithms
{
    public class ClusteringRTsAndBuffers : System.IDisposable
    {
        public const int max_num_clusters = 32;
        public const bool randomInit = false;

        public RenderTexture rtResult;
        public ClusteringTextures texturesWorkRes { get; private set; }
        public ClusteringTextures texturesFullRes { get; private set; }
        public ComputeBuffer cbufClusterCenters { get; private set; }
        public ComputeBuffer cbufRandomPositions { get; private set; }
        public readonly int numClusters;
        public readonly int jitterSize;

        private readonly int[] scanlinePixelOffset = new int[2];
        private int[][] jitterOffsets;

        /// <summary>
        /// Get a pooled instance of ClusterCenters with a copy of the data from ComputeBuffer. Safe to modify. Don't forget to dispose!
        /// </summary>
        /// <returns></returns>
        public ClusterCenters GetClusterCenters()
        {
            this.cbufClusterCenters.GetData(this.clusterCentersTempData);
            return ClusterCenters.Get(this.numClusters, this.clusterCentersTempData);
        }

        /// <summary>
        /// Reads the cluster centers data from the array and loads it into the ComputeBuffer.
        /// </summary>
        /// <param name="clusterCentersBufferData"></param>
        public void SetClusterCenters(Vector4[] clusterCentersBufferData)
        {
            this.cbufClusterCenters.SetData(clusterCentersBufferData);
        }

        private Vector4[] clusterCentersTempData;

        /// <summary>
        /// For working resolution
        /// </summary>
        private Position[] randomPositions;
        private System.Random random;

        private struct Position
        {
            public int x;
            public int y;

            /*
                NVidia says structures not aligned to 128 bits are slow
                https://developer.nvidia.com/content/understanding-structured-buffer-performance
            */
            private readonly int padding_1;
            private readonly int padding_2;
        }

        public int PickRandomCluster(int numClusters)
        {
            return this.random.Next(numClusters);
        }

        public void UpdateRandomPositions()
        {
            int textureSize = this.texturesWorkRes.size;

            for (int k = 0; k < this.randomPositions.Length; k++)
            {
                this.randomPositions[k].x = this.random.Next(textureSize);
                this.randomPositions[k].y = this.random.Next(textureSize);
            }
            this.cbufRandomPositions.SetData(this.randomPositions);
        }

        public readonly int workingSize;
        public readonly int fullSize;

        /// <summary>
        /// Textures are not allocated in the constructor. Call Allocate() manually before using.
        /// </summary>
        public ClusteringRTsAndBuffers(
            int numClusters,
            int workingSize,
            int fullSize,
            int jitterSize
        )
        {
            this.numClusters = numClusters;
            this.workingSize = workingSize;
            this.fullSize = fullSize;
            this.jitterSize = jitterSize;
        }

        public void Allocate()
        {
            Debug.Assert(this.isAllocated == false);

            this.rtResult = new RenderTexture(
                this.workingSize,
                this.workingSize,
                0,
                RenderTextureFormat.ARGBFloat
            )
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };

            this.jitterOffsets = JitterPattern.Get(this.jitterSize);

            this.texturesWorkRes = new ClusteringTextures(this.workingSize);
            this.texturesFullRes = new ClusteringTextures(this.fullSize);

            this.random = new System.Random();

            this.cbufRandomPositions = new ComputeBuffer(max_num_clusters, sizeof(int) * 4);
            this.randomPositions = new Position[max_num_clusters];
            this.UpdateRandomPositions();

            /*
                second half of the buffer contains candidate cluster centers
                first half contains current cluster centers
      
                NVidia says structures not aligned to 128 bits are slow
                https://developer.nvidia.com/content/understanding-structured-buffer-performance
            */
            this.cbufClusterCenters = new ComputeBuffer(this.numClusters * 2, sizeof(float) * 4);
            this.clusterCentersTempData = new Vector4[this.numClusters * 2];
            if (randomInit)
            {
                this.RandomizeClusterCenters();
            }
            else
            {
                this.SetDeterministicClusterCenters();
            }
        }

        public bool isAllocated => this.cbufClusterCenters != null;

        public void SetDeterministicClusterCenters()
        {
            for (int i = 0; i < this.numClusters; i++)
            {
                var c = Color.HSVToRGB(i / (float)(this.numClusters), 1, 1);
                c *= 1.0f / (c.r + c.g + c.b);

                // variance infinity to ensure new cluster centers will replace these ones
                this.clusterCentersTempData[i] = new Vector4(c.r, c.g, Mathf.Infinity, 0); // "new"
                this.clusterCentersTempData[i + this.numClusters] = new Vector4(
                    c.r,
                    c.g,
                    Mathf.Infinity,
                    0
                ); // "old"
            }
            this.cbufClusterCenters.SetData(this.clusterCentersTempData);
        }

        public void SetFakeValidClusterCenters()
        {
            for (int i = 0; i < this.numClusters; i++)
            {
                var c = Color.HSVToRGB(i / (float)(this.numClusters), 1, 1);
                c *= 1.0f / (c.r + c.g + c.b);

                // variance infinity to ensure new cluster centers will replace these ones
                this.clusterCentersTempData[i] = new Vector4(c.r, c.g, 0.1f, 0); // "new"
                this.clusterCentersTempData[i + this.numClusters] = new Vector4(c.r, c.g, 0.1f, 0); // "old"
            }
            this.cbufClusterCenters.SetData(this.clusterCentersTempData);
        }

        public void RandomizeClusterCenters()
        {
            for (int i = 0; i < this.numClusters; i++)
            {
                var c = Color.HSVToRGB((float)this.random.NextDouble(), 1, 1);
                c *= 1.0f / (c.r + c.g + c.b);

                // variance infinity to ensure new cluster centers will replace these ones
                this.clusterCentersTempData[i] = new Vector4(c.r, c.g, Mathf.Infinity, 0); // "new"
                this.clusterCentersTempData[i + this.numClusters] = new Vector4(
                    c.r,
                    c.g,
                    Mathf.Infinity,
                    0
                ); // "old"
            }
            this.cbufClusterCenters.SetData(this.clusterCentersTempData);
        }

        /// <summary>
        /// Update textureWorkRes by downsampling textureFullRes
        /// </summary>
        public void Downsample(
            ComputeShader csHighlightRemoval,
            bool staggeredJitter,
            bool doDownscale
        )
        {
            int kernelSubsample = csHighlightRemoval.FindKernel("SubSample");

            csHighlightRemoval.SetInt(
                "sub_sample_multiplier",
                this.texturesFullRes.size / this.texturesWorkRes.size
            );

            if (staggeredJitter)
            {
                csHighlightRemoval.SetInts(
                    "sub_sample_offset",
                    this.jitterOffsets[Time.frameCount % this.jitterOffsets.Length]
                );
            }
            else
            {
                this.scanlinePixelOffset[0] = Time.frameCount % this.jitterSize;
                this.scanlinePixelOffset[1] =
                    (Time.frameCount / this.jitterOffsets.Length) % this.jitterSize;
                csHighlightRemoval.SetInts("sub_sample_offset", this.scanlinePixelOffset);
            }

            csHighlightRemoval.SetTexture(
                kernelSubsample,
                "tex_input",
                this.texturesFullRes.rtInput
            );

            csHighlightRemoval.SetTexture(
                kernelSubsample,
                "tex_output",
                this.texturesWorkRes.rtInput
            );

            csHighlightRemoval.SetBool("downscale", doDownscale);

            csHighlightRemoval.Dispatch(
                kernelSubsample,
                this.texturesWorkRes.size / ClusteringTest.kernelSize,
                this.texturesWorkRes.size / ClusteringTest.kernelSize,
                1
            );
        }

        public void Dispose()
        {
            this.cbufClusterCenters.Release();
            this.cbufRandomPositions.Release();
            this.rtResult.Release();
            this.texturesFullRes.Dispose();
            this.texturesWorkRes.Dispose();
        }
    }
}
