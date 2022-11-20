using UnityEngine;

namespace BenchmarkGeneration
{
    public abstract class ABenchmarkGenerator
    {
        protected readonly int kernelSize;
        protected readonly UnityEngine.Video.VideoClip[] videos;
        protected readonly ComputeShader csHighlightRemoval;

        public ABenchmarkGenerator(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        )
        {
            this.kernelSize = kernelSize;
            this.videos = videos;
            this.csHighlightRemoval = csHighlightRemoval;
        }

        public abstract BenchmarkDescription GenerateBenchmark();
    }
}
