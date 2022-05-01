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
        samples
            .map((sample, sampleId) => {
                return {
                    sampleId,
                    sample,
                    cluster: centers[attribution[sampleId]],
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
            ) / samples.length
    );
}

const kHarmonicMeans: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        const weights = Array(samples.length)
            .fill(0)
            .map(() => Array(centers.length).fill(0));

        for (const _ of Array(numIterations)) {
            this.computeWeights({ samples, centers, weights });
            this.updateCenters(samples, centers, weights);
        }

        for (const sampleId of samples.keys()) {
            attribution[sampleId] = Object.entries(weights[sampleId])
                // go over all cluster weights
                .map((entry) => {
                    const [clusterId, weight] = entry;
                    return { clusterId: Number.parseInt(clusterId), weight };
                })
                .reduce((weightInfoA, weightInfoB) => {
                    // find biggest cluster wieght
                    if (weightInfoA.weight >= weightInfoB.weight) {
                        return weightInfoA;
                    }
                    return weightInfoB;
                }).clusterId;
        }
    },

    updateCenters: ({
        samples,
        centers,
        weights,
    }: {
        samples: number[][];
        centers: number[][];
        weights: number[][];
    }) => {
        for (const centerId of centers.keys()) {
            centers[centerId] = samples
                // go over evey sample
                .map((sample, sampleId) =>
                    sample.map(
                        // multiply every coordinate by cluster weight
                        (coord) => coord * weights[sampleId][centerId]
                    )
                )
                .reduce((prevSample, curSample) =>
                    // weighted sum of samples
                    prevSample.map(
                        (coord, coordId) => coord + curSample[coordId]
                    )
                );
        }
    },

    computeWeights: ({
        samples,
        centers,
        weights,
    }: {
        samples: number[][];
        centers: number[][];
        weights: number[][];
    }) => {
        for (const sampleId of samples.keys()) {
            const distances = centers.map(
                // go over all cluster centers
                (center) =>
                    center
                        // go over all coordinates
                        .map(
                            // coordinate differences
                            (coord, coordId) =>
                                coord - samples[sampleId][coordId]
                        )
                        .map(
                            // squared coordinate differences
                            (difference) => difference ** 2
                        )
                        .reduce(
                            // sum squared coordinate differences
                            (a, b) => a + b
                        ) ** 0.5
            );

            const minDistanceInfo = distances
                .map((distance, clusterId) => {
                    return { distance, clusterId };
                })
                .reduce((distanceInfoA, distanceInfoB) => {
                    if (distanceInfoA.distance <= distanceInfoB.distance) {
                        return distanceInfoA;
                    }
                    return distanceInfoB;
                });

            for (const clusterId of centers.keys()) {
                let top = minDistanceInfo.distance;

                if (clusterId != minDistanceInfo.clusterId) {
                    top *=
                        (minDistanceInfo.distance / distances[clusterId]) ** 5;
                }

                let bottom = 1.0;

                for (const otherClusterId of centers.keys()) {
                    if (otherClusterId == minDistanceInfo.clusterId) {
                        continue;
                    }

                    bottom +=
                        (minDistanceInfo.distance /
                            distances[otherClusterId]) **
                        3;
                }

                weights[sampleId][clusterId] = top / bottom ** 2;
            }

            // normalize weights (ensure they add up to 1)
            const sumWeights = weights[sampleId].reduce((a, b) => a + b);
            for (const clusterId of weights[sampleId].keys()) {
                weights[sampleId][clusterId] /= sumWeights;
            }
        }
    },
};

const kMeans: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        for (const _ of Array(numIterations)) {
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
            attribution[sampleIndex] = centers
                .map((center, centerId) => {
                    return {
                        centerId,
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
                ).centerId;
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
        for (const centerId of centers.keys()) {
            const mySamples = [...attribution.keys()]
                // go over all sample ids
                .filter(
                    // choose samples attributed to this cluster
                    (sampleId) => attribution[sampleId] == centerId
                )
                .map(
                    // get samples from ids
                    (sampleId) => samples[sampleId]
                );

            if (mySamples.length == 0) {
                // if no samples belong to this cluster, leave it unchanged
                mySamples.push([...centers[centerId]]);
            }

            centers[centerId] = mySamples
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
        }
    },
};

const randomSwap: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        assert.equal(numIterations % 2, 0);

        kMeans.attributeSamples({
            samples,
            attribution,
            centers,
        });

        let oldVariance = getVariance({ samples, attribution, centers });
        let oldCenters = centers.map((center) => center.slice());

        //console.log(`old variance: ${oldVariance}`);

        for (const _ of Array(numIterations / 2)) {
            // swap

            centers[Math.floor(Math.random() * centers.length)] =
                samples[Math.floor(Math.random() * samples.length)].slice();

            kMeans.runClustering({
                samples,
                attribution,
                centers,
                numIterations: 2,
            });

            const newVariance = getVariance({ samples, attribution, centers });

            if (newVariance >= oldVariance) {
                for (const centerIndex in centers) {
                    centers[centerIndex] = oldCenters[centerIndex];
                }
            } else {
                oldVariance = newVariance;
                oldCenters = centers.map((center) => center.slice());
            }
        }

        kMeans.attributeSamples({
            samples,
            attribution,
            centers,
        });
    },
};

export { kMeans, randomSwap, kHarmonicMeans, getVariance };
