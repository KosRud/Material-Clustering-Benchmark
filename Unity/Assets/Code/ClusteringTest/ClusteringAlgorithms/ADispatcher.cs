using UnityEngine;

namespace ClusteringAlgorithms {

  public abstract class ADispatcher {

    // reported in the file
    public readonly bool doRandomizeEmptyClusters;
    public readonly int numIterations;
    public readonly ClusteringRTsAndBuffers clusteringRTsAndBuffers;

    public abstract string descriptionString {
      get;
    }

    // internal
    protected readonly ComputeShader computeShader;
    private readonly int kernelHandleAttributeClusters;
    private readonly int kernelUpdateClusterCenters;

    /* ToDo
      remove redundant arguments
      move them into clusteringRTsAndBuffers

      clusteringRTsAndBuffers is re-created
      for every new work item

      later reference work directly?

      move inputTex to clusteringRTsAndBuffers
      or work
    */
    protected ADispatcher(
      ComputeShader computeShader,
      int numIterations,
      bool doRandomizeEmptyClusters,
      ClusteringRTsAndBuffers clusteringRTsAndBuffers
    ) {
      this.computeShader = computeShader;
      this.kernelHandleAttributeClusters =
        this.computeShader.FindKernel("AttributeClusters");
      this.kernelUpdateClusterCenters =
        computeShader.FindKernel("UpdateClusterCenters");
      this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
      this.numIterations = numIterations;
      this.clusteringRTsAndBuffers = clusteringRTsAndBuffers;
    }

    public abstract void RunClustering(ClusteringTextures clusteringTextures);

    public void UpdateClusterCenters(
      ClusteringTextures textures,
      bool rejectOld
    ) {
      this.clusteringRTsAndBuffers.UpdateRandomPositions();

      this.computeShader.SetBool("reject_old", rejectOld);
      this.computeShader.SetTexture(
        this.kernelUpdateClusterCenters,
        "tex_arr_clusters_r",
        textures.rtArr
      );
      this.computeShader.SetTexture(
        this.kernelUpdateClusterCenters, "tex_input",
        textures.rtInput
      );
      this.computeShader.SetBuffer(
        this.kernelUpdateClusterCenters,
        "cbuf_cluster_centers",
        this.clusteringRTsAndBuffers.cbufClusterCenters
      );
      this.computeShader.SetBuffer(
        this.kernelUpdateClusterCenters,
        "cbuf_random_positions",
        this.clusteringRTsAndBuffers.cbufRandomPositions
      );
      this.computeShader.Dispatch(this.kernelUpdateClusterCenters, 1, 1, 1);
    }

    public void AttributeClusters(
      ClusteringTextures textures,
      bool final,
      bool khm
    ) {
      this.computeShader.SetBool("KHM", khm);
      this.computeShader.SetBool("final", final);

      this.computeShader.SetTexture(
        this.kernelHandleAttributeClusters,
        "tex_input",
        textures.rtInput
      );
      this.computeShader.SetTexture(
        this.kernelHandleAttributeClusters,
        "tex_arr_clusters_rw",
        textures.rtArr
      );
      this.computeShader.SetBuffer(
        this.kernelHandleAttributeClusters,
        "cbuf_cluster_centers",
        this.clusteringRTsAndBuffers.cbufClusterCenters
      );
      this.computeShader.Dispatch(
        this.kernelHandleAttributeClusters,
        textures.size / ClusteringTest.kernelSize,
        textures.size / ClusteringTest.kernelSize,
        1
      );

      textures.rtArr.GenerateMips();
    }

    /// <summary>
    /// Computes variance on full-resolution input texture, without thresholding of dark pixels.
    /// </summary>
    public float GetVariance() {
      using (
        ClusterCenters backupCenters = this.clusteringRTsAndBuffers.GetClusterCenters()
      ) {
        /*
          one final attribution
          (we finished by getting cluster centers)

          also ensure final=true (no threshold)
        */
        this.AttributeClusters(
          textures: this.clusteringRTsAndBuffers.texturesFullRes,
          final: true,
          khm: false
        );

        /*
          the variance computation is delayed by 1 iteration

          after updating cluster centers for the 1st time
          we get the variance of 0 iterations

          so in order to get current variance,
          we need one more cluster center update

          additionally, we want to get tha variance
          from attribution with "final: true"
          which disables thresholding of dark pixels
        */
        this.UpdateClusterCenters(
          textures: this.clusteringRTsAndBuffers.texturesFullRes,
          rejectOld: false
        );

        using (
          ClusterCenters centers = this.clusteringRTsAndBuffers.GetClusterCenters()
        ) {
          this.clusteringRTsAndBuffers.SetClusterCenters(backupCenters.centers);
          return centers.variance;
        }
      }
    }
  }
}