import { SimpleClusteringAlgorithm } from '../SimpleClusteringAlgorithm';

export class KMeans extends SimpleClusteringAlgorithm {
    override get name() {
        return 'KM';
    }

    override attributeSamples() {
        for (const [sampleIndex, sample] of Object.entries(this.samples)) {
            this.attribution[sampleIndex as any as number] = this.centers
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
    }

    updateCenters() {
        for (const centerId of this.centers.keys()) {
            const mySamples = [...this.attribution.keys()]
                // go over all sample ids
                .filter(
                    // choose samples attributed to this cluster
                    (sampleId) => this.attribution[sampleId] == centerId
                )
                .map(
                    // get samples from ids
                    (sampleId) => this.samples[sampleId]
                );

            if (mySamples.length == 0) {
                // if no samples belong to this cluster, leave it unchanged
                mySamples.push([...this.centers[centerId]]);
            }

            this.centers[centerId] = mySamples
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
    }
}
