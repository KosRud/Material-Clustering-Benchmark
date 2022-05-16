import { SimpleClusteringAlgorithm } from '../SimpleClusteringAlgorithm';
import { StopCondition } from '../../../ClusteringAlgorithm';

export class KHarmonicMeans extends SimpleClusteringAlgorithm {
    protected weights: number[][];

    constructor({
        samples,
        attribution,
        centers,
        numIterations,
        weights,
    }: {
        samples: number[][];
        attribution: number[];
        centers: number[][];
        numIterations: number | StopCondition;
        weights: number[][];
    }) {
        super({ samples, attribution, centers, numIterations });
        this.weights = weights;
    }

    override get name() {
        return 'KHM';
    }

    setAttribution({
        samples,
        weights,
        attribution,
    }: {
        samples: number[][];
        weights: number[][];
        attribution: number[];
    }) {
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
    }

    override updateCenters() {
        for (const centerId of this.centers.keys()) {
            this.centers[centerId] = this.samples
                // go over evey sample
                .map((sample, sampleId) =>
                    sample.map(
                        // multiply every coordinate by cluster weight
                        (coord) => coord * this.weights[sampleId][centerId]
                    )
                )
                .reduce((prevSample, curSample) =>
                    // weighted sum of samples
                    prevSample.map(
                        (coord, coordId) => coord + curSample[coordId]
                    )
                );
        }
    }

    attributeSamples() {
        for (const sampleId of this.samples.keys()) {
            const distances = this.centers.map(
                // go over all cluster centers
                (center) =>
                    center
                        // go over all coordinates
                        .map(
                            // coordinate differences
                            (coord, coordId) =>
                                coord - this.samples[sampleId][coordId]
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

            this.attribution[sampleId] = minDistanceInfo.clusterId;

            for (const clusterId of this.centers.keys()) {
                let top = minDistanceInfo.distance;

                if (clusterId != minDistanceInfo.clusterId) {
                    top *=
                        (minDistanceInfo.distance / distances[clusterId]) ** 5;
                }

                let bottom = 1.0;

                for (const otherClusterId of this.centers.keys()) {
                    if (otherClusterId == minDistanceInfo.clusterId) {
                        continue;
                    }

                    bottom +=
                        (minDistanceInfo.distance /
                            distances[otherClusterId]) **
                        3;
                }

                this.weights[sampleId][clusterId] = top / bottom ** 2;

                /*
                    * uncomment to turn this into Kmeans for debugging

                    if (minDistanceInfo.clusterId == clusterId) {
                        weights[sampleId][clusterId] = 1;
                    } else {
                        weights[sampleId][clusterId] = 0;
                    }
                */
            }
        }

        // normalize weights per-cluster (ensure they add up to 1)
        {
            const perClusterWeightSums = this.weights
                // sum the weights of each cluster over all samples
                .reduce((perClusterWeightsA, perClusterWeightsB) => {
                    return (
                        [...perClusterWeightsA.keys()]
                            // go over the weight of every cluster
                            .map(
                                // sum weights
                                (clusterId) =>
                                    perClusterWeightsA[clusterId] +
                                    perClusterWeightsB[clusterId]
                            )
                    );
                });

            for (const perClusterWeights of this.weights) {
                for (const clusterId of perClusterWeights.keys()) {
                    perClusterWeights[clusterId] /=
                        perClusterWeightSums[clusterId];
                }
            }
        }
    }
}
