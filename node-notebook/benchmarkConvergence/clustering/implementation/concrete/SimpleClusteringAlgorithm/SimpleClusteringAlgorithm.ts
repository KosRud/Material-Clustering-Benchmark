import {
    ClusteringAlgorithm,
    StopCondition,
    AlgorithmResult,
} from '../../ClusteringAlgorithm';

export abstract class SimpleClusteringAlgorithm extends ClusteringAlgorithm {
    abstract get name(): string;

    override runClustering() {
        switch (typeof this.numIterations) {
            case 'number':
                const results: AlgorithmResult[] = [];

                for (
                    let iteration = 1;
                    iteration <= this.numIterations;
                    iteration++
                ) {
                    this.attributeSamples();
                    this.updateCenters();
                    results.push({
                        algorithm: this.name,
                        numIterations: iteration,
                        variance: this.getVariance(),
                    });
                }

                return results;

            default:
                throw 'not imlpemented';
        }
    }

    abstract attributeSamples();
    abstract updateCenters();
}
