import {
    ClusteringAlgorithm,
    StopCondition,
    AlgorithmResult,
} from '../ClusteringAlgorithm';
import { KMeans } from './SimpleClusteringAlgorithm/concrete/KMeans';

import assert from 'assert/strict';

import copy from 'deepcopy';

export class RandomSwap extends ClusteringAlgorithm {
    private kmeans: KMeans;

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
        this.centers[Math.floor(Math.random() * this.centers.length)] = copy(
            this.samples[Math.floor(Math.random() * this.samples.length)]
        );
    }

    override runClustering() {
        const results: AlgorithmResult[] = [];

        this.kmeans.attributeSamples();

        let oldVariance = this.getVariance();
        let oldCenters = copy(this.centers);

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

                    if (newVariance >= oldVariance) {
                        for (const centerIndex in this.centers) {
                            this.centers[centerIndex] = oldCenters[centerIndex];
                        }
                    } else {
                        oldVariance = newVariance;
                        oldCenters = this.centers.map((center) =>
                            center.slice()
                        );
                    }

                    results.push({
                        algorithm: 'Random swap',
                        numIterations: iteration,
                        variance: oldVariance,
                    });
                }

                this.kmeans.attributeSamples();

                return results;

            default:
                throw 'not imlpemented';
        }
    }
}
