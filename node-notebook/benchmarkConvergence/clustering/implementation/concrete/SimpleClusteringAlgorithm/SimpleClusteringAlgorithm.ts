import {
    ClusteringAlgorithm,
    StopCondition,
    AlgorithmResult,
} from '../../ClusteringAlgorithm';

export abstract class SimpleClusteringAlgorithm extends ClusteringAlgorithm {
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

            case 'object':
                const stopCondition: StopCondition = this.numIterations;

                this.attributeSamples();

                let lastVariance = this.getVariance();
                for (let numIterations = 1; ; numIterations++) {
                    this.updateCenters();
                    this.attributeSamples();

                    const newVariance = this.getVariance();
                    if (
                        lastVariance - newVariance <
                        stopCondition.deltaVariance
                    ) {
                        return [
                            {
                                algorithm: this.name,
                                numIterations: numIterations,
                                variance: this.getVariance(),
                            },
                        ];
                    }
                }

            default:
                throw new Error('not imlpemented');
        }
    }

    abstract attributeSamples();
    abstract updateCenters();
}
