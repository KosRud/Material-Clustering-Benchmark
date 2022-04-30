import cluster from 'cluster';

type attributeSamples = (
    samples: number[][],
    attribution: number[],
    centers: number[][]
) => void;

type updateCenters = (
    samples: number[][],
    attribution: number[],
    centers: number[][]
) => void;

interface ClusteringAlgorithm {
    attributeSamples: attributeSamples;
    updateCenters: updateCenters;
}

function getVariance(
    samples: number[][],
    attribution: number[],
    centers: number[][]
): number {
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
    attributeSamples: (
        samples: number[][],
        attribution: number[],
        centers: number[][]
    ) => {
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

    updateCenters: (
        samples: number[][],
        attribution: number[],
        centers: number[][]
    ) => {
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

export { Kmeans, getVariance };
