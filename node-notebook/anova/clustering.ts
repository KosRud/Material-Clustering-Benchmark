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

const kHarmonicMeans: ClusteringAlgorithm = {
    runClustering({ samples, attribution, centers, numIterations }) {
        const weights = Array(samples.length)
            .fill(0)
            .map(() => Array(centers.length).fill(0));

        for (const _ of Array(numIterations).fill(0)) {
            this.computeWeights({ samples, centers, weights });
            this.updateCenters(samples, centers, weights);
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
        //
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
        for (const sampleId in samples) {
            const distanceInfos = Object.entries(
                // array of distances
                centers.map(
                    (center) =>
                        [...center.keys()]
                            // go over all coordinates
                            .map(
                                // coordinate differences
                                (coordId) =>
                                    center[coordId] - samples[sampleId][coordId]
                            )
                            .map(
                                // squared coordinate differences
                                (difference) => difference ** 2
                            )
                            .reduce(
                                // sum squared coordinate differences
                                (a, b) => a + b
                            ) ** 0.5
                )
            ).map((entry) => {
                const [clusterId, distance] = entry;
                return { clusterId, distance };
            });

            const minDistanceInfo = distanceInfos.reduce(
                (distanceInfoA, distanceInfoB) => {
                    if (distanceInfoA.distance <= distanceInfoB.distance) {
                        return distanceInfoA;
                    }
                    return distanceInfoB;
                }
            );

            for (const centerId in centers) {
                let top = minDistanceInfo.distance;

                if (centerId != minDistanceInfo.clusterId) {
                    top *=
                        (minDistanceInfo.distance /
                            distanceInfos[centerId].distance) **
                        5;
                }

                let bottom = 1.0;

                for (const otherCenterId in centers) {
                    if (otherCenterId == minDistanceInfo.clusterId) {
                        continue;
                    }

                    bottom +=
                        (minDistanceInfo.distance /
                            distanceInfos[otherCenterId].distance) **
                        3;
                }

                weights[sampleId][centerId] = top / bottom ** 2;
            }
        }
    },
};

const kMeans: ClusteringAlgorithm = {
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

        for (const _ of Array(numIterations / 2).fill(0)) {
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

export { kMeans, randomSwap, getVariance };
