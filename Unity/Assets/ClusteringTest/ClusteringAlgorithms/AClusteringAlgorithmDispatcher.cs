using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusteringRTsAndBuffers {
    public RenderTexture rtArr;
    public RenderTexture rtVariance;
    public ComputeBuffer cbufClusterCenters;

    private readonly Vector4[] _clusterCenters;
    public Vector4[] clusterCenters {
        get {
            this.cbufClusterCenters.GetData(this._clusterCenters);
            return this._clusterCenters;
        }
        set => this.cbufClusterCenters.SetData(value);
    }


    public ClusteringRTsAndBuffers(int numClusters, int textureSize, int referenceTextureSize) {
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

public abstract class AClusteringAlgorithmDispatcher {

    // reported in the file
    public readonly int numClusters;
    public readonly bool doRandomizeEmptyClusters;
    public readonly int numIterations;

    public string descriptionString {
        get;
    }

    // internal
    private readonly int kernelSize;
    private readonly UnityEngine.ComputeShader computeShader;
    private int kernelAttributeClusters;

    public abstract void RunClustering();

    private void AttributeClusters(
        Texture inputTex,
        ClusteringRTsAndBuffers clusteringRTsAndBuffers,
        bool final = false
    ) {
        this.computeShader.SetBool("final", final);  // replace with define
        this.computeShader.SetTexture(
            this.kernelAttributeClusters,
            "tex_input",
            inputTex
        );
        this.computeShader.SetTexture(
            this.kernelAttributeClusters,
            "tex_variance",
            clusteringRTsAndBuffers.rtVariance
        );
        this.computeShader.SetTexture(
            this.kernelAttributeClusters,
            "tex_arr_clusters_rw",
            clusteringRTsAndBuffers.rtArr
        );
        this.computeShader.SetBuffer(
            this.kernelAttributeClusters,
            "cbuf_cluster_centers",
            clusteringRTsAndBuffers.cbufClusterCenters
        );
        this.computeShader.Dispatch(
            this.kernelAttributeClusters,
            inputTex.width / this.kernelSize,
            inputTex.height / this.kernelSize,
            1
        );
    }
}
