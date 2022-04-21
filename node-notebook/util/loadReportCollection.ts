import { ReportCollection } from './validators/Validators';
import * as validatorTemplates from './validators/generated/Validators-ti';
import { createCheckers } from 'ts-interface-checker';

const validators = createCheckers(validatorTemplates.default);

interface ProcessedReport {
    measurement: {
        aggregated: {
            mean: number;
            peak: number;
        };
    };
    launchParameters;
    logType: string;
}

export type ProcessedReportCollection = ReportCollection & {
    reports: [ProcessedReport];
};

export default function loadReportCollection(
    reportCollection: ReportCollection
): ProcessedReportCollection {
    validators.ReportCollection.check(reportCollection);

    for (const report of reportCollection.reports) {
        const processedReport = report as undefined as ProcessedReport;

        const varianceByFrame = report.measurement.varianceByFrame;
        const arrVariance = varianceByFrame.map(
            (frameRecord) => frameRecord.variance
        );
        const aggregated = {
            mean: -1,
            peak: -1,
        };
        aggregated.mean =
            arrVariance.reduce((a, b) => a + b) / varianceByFrame.length;
        aggregated.peak = arrVariance.reduce((a, b) => Math.max(a, b));

        ////////////////////////////////

        processedReport.launchParameters = report.serializableLaunchParameters;
        delete report.serializableLaunchParameters;

        processedReport.measurement.aggregated = aggregated;

        switch (report.logType) {
            case 1:
                processedReport.logType = 'variance';
                break;
            default:
                throw 'invalid log type!';
        }
    }

    return reportCollection as undefined as ProcessedReportCollection;
}
