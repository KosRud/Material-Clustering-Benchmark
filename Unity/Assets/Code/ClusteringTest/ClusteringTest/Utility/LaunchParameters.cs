using ClusteringAlgorithms;
using System;
using UnityEngine;

namespace BenchmarkGeneration
{
    /// <summary>
    /// Call <see cref="Dispose" /> after using.
    /// </summary>
    public class LaunchParameters : IDisposable
    {
        [Serializable]
        public class SerializableLaunchParameters
        {
            [SerializeReference]
            public DispatcherParameters abstractDispatcherParameters;

            public string videoName;
            public int numIterations;
            public int workingTextureSize;
            public int numClusters;
            public int jitterSize;
            public bool staggeredJitter;
            public bool doDownscale;
            public string algorithm;
            public bool doRandomizeEmptyClusters;
            public bool stopCondition;
            public bool readback;
            public bool useFullResTexRef;

            public SerializableLaunchParameters(
                string videoName,
                int numIterations,
                int workingTextureSize,
                int numClusters,
                int jitterSize,
                bool staggeredJitter,
                bool doDownscale,
                string algorithm,
                bool doRandomizeEmptyClusters,
                bool stopCondition,
                bool readback,
                bool useFullResTexRef,
                DispatcherParameters dispatcherParameters
            )
            {
                this.videoName = videoName;
                this.numIterations = numIterations;
                this.workingTextureSize = workingTextureSize;
                this.numClusters = numClusters;
                this.jitterSize = jitterSize;
                this.staggeredJitter = staggeredJitter;
                this.doDownscale = doDownscale;
                this.algorithm = algorithm;
                this.doRandomizeEmptyClusters = doRandomizeEmptyClusters;
                this.abstractDispatcherParameters = dispatcherParameters;
                this.stopCondition = stopCondition;
                this.useFullResTexRef = useFullResTexRef;
                this.readback = readback;
            }
        }

        public SerializableLaunchParameters GetSerializable()
        {
            return new SerializableLaunchParameters(
                videoName: this.video.name,
                numIterations: this.dispatcher.numIterations,
                workingTextureSize: this.dispatcher.clusteringRTsAndBuffers.texturesWorkRes.size,
                numClusters: this.dispatcher.clusteringRTsAndBuffers.numClusters,
                jitterSize: this.dispatcher.clusteringRTsAndBuffers.jitterSize,
                staggeredJitter: this.staggeredJitter,
                doDownscale: this.doDownscale,
                algorithm: this.dispatcher.name,
                doRandomizeEmptyClusters: this.dispatcher.doRandomizeEmptyClusters,
                dispatcherParameters: this.dispatcher.abstractParameters,
                stopCondition: this.dispatcher.usesStopCondition,
                useFullResTexRef: this.dispatcher.useFullResTexRef,
                readback: this.dispatcher.doesReadback
            );
        }

        public readonly bool staggeredJitter;
        public readonly UnityEngine.Video.VideoClip video;
        public readonly bool doDownscale;
        public readonly IDispatcher dispatcher;

        /// <summary>
        /// Takes ownership of the dispatcher
        /// </summary>
        public LaunchParameters(
            bool staggeredJitter,
            UnityEngine.Video.VideoClip video,
            bool doDownscale,
            IDispatcher dispatcher
        )
        {
            this.staggeredJitter = staggeredJitter;
            this.video = video;
            this.doDownscale = doDownscale;
            this.dispatcher = dispatcher;
        }

        public void Dispose()
        {
            this.dispatcher.Dispose();
        }
    }
}
