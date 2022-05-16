const copy = require('deepcopy');

interface AlgorithmResult {
    algorithm: string;
    numIterations: number;
    variance: number;
}

interface StopCondition {
    deltaVariance: number;
    failedSwaps: number;
}

abstract class ClusteringAlgorithm {
    protected samples: number[][];
    protected attribution: number[];
    protected centers: number[][];
    protected numIterations: number | StopCondition;

    abstract get name(): string;

    constructor({
        samples,
        attribution,
        centers,
        numIterations,
    }: {
        samples: number[][];
        attribution: number[];
        centers: number[][];
        numIterations: number | StopCondition;
    }) {
        this.samples = samples;
        this.numIterations = numIterations;
        this.centers = centers;
        this.attribution = attribution;
    }

    abstract runClustering(): AlgorithmResult[];

    /**
     * Uses current attribution array (does not update attribution).
     */
    getVariance(): number {
        return (
            this.samples
                .map((sample, sampleId) => {
                    return {
                        sampleId,
                        sample,
                        cluster: this.centers[this.attribution[sampleId]],
                    };
                })
                .map(
                    // squared distances to cluster center
                    (sampleInfo) =>
                        // go over all coordinates
                        sampleInfo.cluster
                            .map(
                                // differences between cluster coordinate and sample coordinate
                                (_, coordIndex) =>
                                    sampleInfo.cluster[coordIndex] -
                                    sampleInfo.sample[coordIndex]
                            )
                            .map(
                                // squared coordinate differences
                                (difference) => difference ** 2
                            )
                            .reduce(
                                // sum of squared coordinate differences
                                (a, b) => a + b
                            )
                )
                .reduce(
                    // sum of squared distances to cluster center
                    (a, b) => a + b
                ) / this.samples.length
        );
    }
}

export { ClusteringAlgorithm, StopCondition, AlgorithmResult };
