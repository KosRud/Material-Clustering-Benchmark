import cluster from 'cluster';
import assert from 'assert/strict';

interface ClusteringAlgorithm {
    runClustering({
        samples,
        attribution,
        centers,
        numIterations,
    }: {
        samples: number[][];
        attribution: number[];
        centers: number[][];
        numIterations: number;
    }): void;
    [key: string]: any;
}

function getVariance({
    samples,
    attribution,
    centers,
}: {
    samples: number[][];
    attribution: number[];
    centers: number[][];
}): number {
    return (
        Object.entries(samples)
            // go over all samples
            .map((entry) => {
                const [index, sample] = entry;
                return { index, sample, cluster: centers[attribution[index]] };
            })
            .map(
                // squared distances to cluster center
                (sampleInfo) =>
                    // go over all coordinates
                    [...sampleInfo.cluster.keys()]
                        .map(
                            // differences between cluster coordinate and sample coordinate
                            (coordIndex) =>
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
            ) / samples.length
    );
}

const Kmeans: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        for (const _ of Array(numIterations).fill(0)) {
            this.attributeSamples({
                samples,
                attribution,
                centers,
                numIterations,
            });
            this.updateCenters({
                samples,
                attribution,
                centers,
                numIterations,
            });
        }
    },
    attributeSamples: ({
        samples,
        attribution,
        centers,
    }: {
        samples: number[][];
        attribution: number[];
        centers: number[][];
    }) => {
        for (const [sampleIndex, sample] of Object.entries(samples)) {
            attribution[sampleIndex] = Object.entries(centers)
                // go over each cluster center
                .map((entry) => {
                    const [centerIndex, center] = entry;
                    return {
                        centerIndex,
                        center,
                        distanceToSampleSq: -1,
                    };
                })
                .map(
                    // generate squared distances to the current sample for each center
                    (centerInfo) => {
                        centerInfo.distanceToSampleSq =
                            // go over each coordinate
                            [...centerInfo.center.keys()]
                                .map(
                                    // differences in each coordinate between center and sample
                                    (coordIndex) =>
                                        centerInfo.center[coordIndex] -
                                        sample[coordIndex]
                                )
                                .map(
                                    // squared differences
                                    (difference) => difference ** 2
                                )
                                .reduce(
                                    // sum of squared differences
                                    (a, b) => a + b
                                ) / [...centerInfo.center.keys()].length;

                        return centerInfo;
                    }
                )
                .reduce(
                    // find the center with smallest distance to sample
                    (centerInfoA, centerInfoB) => {
                        if (
                            centerInfoA.distanceToSampleSq <
                            centerInfoB.distanceToSampleSq
                        ) {
                            return centerInfoA;
                        } else {
                            return centerInfoB;
                        }
                    }
                ).centerIndex;
        }
    },

    updateCenters: ({
        samples,
        attribution,
        centers,
    }: {
        samples: number[][];
        attribution: number[];
        centers: number[][];
    }) => {
        [...centers.keys()]
            // go over all centers
            .map((centerIndex) => {
                const mySamples = Object.entries(samples)
                    // go over all samples
                    .map((entry) => {
                        const [sampleIndex, sample] = entry;
                        return {
                            sampleIndex,
                            sample,
                            attribution: attribution[sampleIndex],
                        };
                    })
                    .filter(
                        // filter sampels, which belong to current center
                        (sampleInfo) => sampleInfo.attribution == centerIndex
                    )
                    .map(
                        // now we only need coords
                        (sampleInfo) => sampleInfo.sample
                    );

                if (mySamples.length == 0) {
                    mySamples.push([...centers[centerIndex]]);
                }

                centers[centerIndex] = mySamples
                    .reduce((sampleA, sampleB) =>
                        // per-coordinate sum of samples
                        [...sampleA.keys()].map(
                            // sum coordinate values from two samples
                            (coordIndex) =>
                                sampleA[coordIndex] + sampleB[coordIndex]
                        )
                    )
                    .map(
                        // divide by the number of samples
                        (coordValue) => coordValue / mySamples.length
                    );
            });
    },
};

const RandomSwap: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        assert.equal(numIterations % 2, 0);

        Kmeans.attributeSamples({
            samples,
            attribution,
            centers,
        });

        const swapLog = [];

        let oldVariance = getVariance({ samples, attribution, centers });
        let oldCenters = centers.map((center) => center.slice());

        //console.log(`old variance: ${oldVariance}`);

        for (const _ of Array(numIterations / 2).fill(0)) {
            // swap

            centers[Math.floor(Math.random() / centers.length)] =
                samples[Math.floor(Math.random() / samples.length)].slice();

            Kmeans.runClustering({
                samples,
                attribution,
                centers,
                numIterations: 2,
            });

            const newVariance = getVariance({ samples, attribution, centers });

            if (newVariance > oldVariance) {
                for (const centerIndex in centers) {
                    centers[centerIndex] = oldCenters[centerIndex];
                }
                swapLog.push(' ');
            } else {
                oldVariance = newVariance;
                oldCenters = centers.map((center) => center.slice());
                swapLog.push('+');
            }
        }

        Kmeans.attributeSamples({
            samples,
            attribution,
            centers,
        });

        return swapLog;
    },
};

export { Kmeans, RandomSwap, getVariance };
