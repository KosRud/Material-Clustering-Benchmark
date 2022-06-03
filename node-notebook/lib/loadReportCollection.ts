import {
    ReportCollection,
    VarianceMeasurement,
    FrameTimeMeasurement,
    Measurement,
} from './validators/Validators';
import * as validatorTemplates from './validators/generated/Validators-ti';
import { createCheckers } from 'ts-interface-checker';
import assert from 'assert/strict';
import smoothPeak from './smoothPeak';

const validators = createCheckers(validatorTemplates.default);

export interface QualityMeasurement extends Measurement {
    rmseByFrame: {
        frameIndex: number;
        rmse: number;
    }[];
    aggregated: {
        mean: number;
        peak: number;
    };
}

interface ProcessedReport {
    measurement: QualityMeasurement | FrameTimeMeasurement;
    launchParameters: any;
    logTypeName: 'variance' | 'frame time';
}

export default function loadReportCollection(
    reportCollection: ReportCollection
): ProcessedReport[] {
    validators.ReportCollection.check(reportCollection);

    return reportCollection.reports.map((report) => {
        return {
            measurement: (() => {
                switch (report.measurement['frameVarianceRecords']) {
                    case undefined:
                        return report.measurement as FrameTimeMeasurement;
                    default:
                        const measurement: VarianceMeasurement =
                            report.measurement as VarianceMeasurement;

                        const varianceByFrame = measurement.frameVarianceRecords;

                        const arrSqrtVariance = varianceByFrame.map(
                            (frameRecord) => frameRecord.variance ** 0.5
                        );
                        const aggregated = {
                            mean: -1,
                            peak: -1,
                        };
                        aggregated.mean =
                            arrSqrtVariance.reduce((a, b) => a + b) /
                            varianceByFrame.length;
                        aggregated.peak = smoothPeak(arrSqrtVariance);

                        return {
                            rmseByFrame: measurement.frameVarianceRecords.map(
                                (record) => {
                                    return {
                                        frameIndex: record.frameIndex,
                                        rmse: record.variance ** 0.5,
                                    };
                                }
                            ),
                            aggregated,
                        };
                }
            })(),
            launchParameters: report.serializableLaunchParameters,
            logTypeName: ((): 'variance' | 'frame time' => {
                switch (report.logType) {
                    case 0:
                        assert.notEqual(
                            report.measurement['frameTimeRecords'],
                            undefined
                        );
                        return 'frame time';
                    case 1:
                        assert.notEqual(
                            report.measurement['frameVarianceRecords'],
                            undefined
                        );
                        return 'variance';
                    default:
                        throw new Error('incorrect report type');
                }
            })(),
        };
    });
}
