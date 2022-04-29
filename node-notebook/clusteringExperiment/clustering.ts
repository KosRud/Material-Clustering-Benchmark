type attributeClusters = (
    data: number[][],
    attribution: number[],
    centers: number[][]
) => void;

type updateCenters = (
    data: number[][],
    attribution: number[],
    centers: number[][]
) => void;

interface ClusteringAlgorithm {
    attributeClusters: attributeClusters;
    updateCenters: updateCenters;
}

const Kmeans: ClusteringAlgorithm = {
    attributeClusters: (
        data: number[][],
        attribution: number[],
        centers: number[][]
    ) => {
        for (const [sampleIndex, sample] of Object.entries(data)) {
            attribution[sampleIndex] = Object.entries(centers)
                // go over each cluster center
                .map((entry) => {
                    const [centerIndex, center] = entry;
                    return {
                        centerIndex,
                        center,
                        distanceToSample: -1,
                    };
                })
                .map(
                    // generate L2 distance to the current sample for each center
                    (centerInfo) => {
                        centerInfo.distanceToSample =
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
                            centerInfoA.distanceToSample <
                            centerInfoB.distanceToSample
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
        data: number[][],
        attribution: number[],
        centers: number[][]
    ) => {
        [...centers.keys()]
            // go over all centers
            .map((centerIndex) => {
                centers[centerIndex] = Object.entries(data)
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
                    )
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
                        (coordValue) => coordValue / data.length
                    );
            });
    },
};

export { Kmeans };
