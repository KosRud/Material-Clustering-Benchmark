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
                        distanceToSampleSq: center
                            .map(
                                // squared coordinate differences
                                (centerCoord, coordId) =>
                                    (centerCoord - sample[coordId]) ** 2
                            )
                            .reduce(
                                // sum squared differences
                                (a, b) => a + b
                            ),
                    };
                })
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
                mySamples.push(this.centers[centerId].slice());
            }

            this.centers[centerId] = mySamples
                .reduce(
                    // per-coordinate sum of samples
                    (sampleA, sampleB) => {
                        for (const coordId of sampleA.keys()) {
                            sampleA[coordId] += sampleB[coordId];
                        }
                        return sampleA;
                    },
                    mySamples[0].map(() => 0)
                )
                .map(
                    // divide by the number of samples
                    (coordValue) => coordValue / mySamples.length
                );
        }
    }
}
