import {
    ClusteringAlgorithm,
    StopCondition,
    AlgorithmResult,
} from '../ClusteringAlgorithm';
import { KMeans } from './SimpleClusteringAlgorithm/concrete/KMeans';

import assert from 'assert/strict';

export class RandomSwap extends ClusteringAlgorithm {
    private kmeans: KMeans;

    override get name() {
        return 'RS';
    }

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
        super({
            samples,
            attribution,
            centers,
            numIterations,
        });

        this.kmeans = new KMeans({
            samples,
            attribution,
            centers,
            numIterations: 2,
        });
    }

    private randomSwap() {
        this.centers[Math.floor(Math.random() * this.centers.length)] =
            this.samples[
                Math.floor(Math.random() * this.samples.length)
            ].slice();
    }

    override runClustering() {
        const results: AlgorithmResult[] = [];

        this.kmeans.attributeSamples();

        let oldVariance = this.getVariance();
        let oldCenters = this.centers.map((center) => center.slice());

        switch (typeof this.numIterations) {
            case 'number':
                assert.equal(this.numIterations % 2, 0);

                for (
                    let iteration = 2;
                    iteration <= this.numIterations;
                    iteration += 2
                ) {
                    this.randomSwap();
                    this.kmeans.runClustering();

                    const newVariance = this.getVariance();

                    if (newVariance < oldVariance) {
                        oldVariance = newVariance;
                        oldCenters = this.centers.map((center) =>
                            center.slice()
                        );
                    } else {
                        for (const centerIndex of this.centers.keys()) {
                            this.centers[centerIndex] = oldCenters[centerIndex];
                        }
                    }

                    results.push({
                        algorithm: 'Random swap',
                        numIterations: iteration,
                        variance: oldVariance,
                    });
                }

                this.kmeans.attributeSamples();

                return results;
            case 'object':
                const stopCondition: StopCondition = this.numIterations;

                let failedSwaps = 0;
                for (let numIterations = 2; ; numIterations += 2) {
                    this.randomSwap();
                    this.kmeans.runClustering();

                    const newVariance = this.getVariance();

                    if (newVariance < oldVariance) {
                        if (
                            oldVariance - newVariance <
                            stopCondition.deltaVariance
                        ) {
                            return [
                                {
                                    algorithm: this.name,
                                    numIterations: numIterations,
                                    variance: this.getVariance(),
                                    stopCondition: stopCondition,
                                },
                            ];
                        }

                        oldVariance = newVariance;
                        oldCenters = this.centers.map((center) =>
                            center.slice()
                        );
                    } else {
                        for (const centerIndex of this.centers.keys()) {
                            this.centers[centerIndex] = oldCenters[centerIndex];
                        }

                        failedSwaps++;

                        if (failedSwaps > stopCondition.failedSwaps) {
                            return [
                                {
                                    algorithm: this.name,
                                    numIterations: numIterations,
                                    variance: this.getVariance(),
                                    stopCondition: stopCondition,
                                },
                            ];
                        }
                    }
                }
            default:
                throw new Error('not imlpemented');
        }
    }
}
