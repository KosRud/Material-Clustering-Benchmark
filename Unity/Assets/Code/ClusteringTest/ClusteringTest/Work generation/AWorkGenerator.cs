using UnityEngine;

namespace WorkGeneration
{
    public abstract class AWorkGenerator
    {
        protected readonly int kernelSize;
        protected readonly UnityEngine.Video.VideoClip[] videos;
        protected readonly ComputeShader csHighlightRemoval;

        public AWorkGenerator(
            int kernelSize,
            UnityEngine.Video.VideoClip[] videos,
            ComputeShader csHighlightRemoval
        )
        {
            this.kernelSize = kernelSize;
            this.videos = videos;
            this.csHighlightRemoval = csHighlightRemoval;
        }

        public abstract WorkList GenerateWork();
    }
}
