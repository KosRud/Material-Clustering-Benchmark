import {
    ReportCollection,
    VarianceMeasurement,
    FrameTimeMeasurement,
    Measurement,
} from "./validators/Validators";
import * as validatorTemplates from "./validators/generated/Validators-ti";
import { createCheckers } from "ts-interface-checker";
import assert from "assert/strict";
import smoothPeak from "./smoothPeak";

const validators = createCheckers(validatorTemplates.default);

export interface QualityMeasurement extends Measurement {
    rmseByFrame: {
        frameIndex: number;
        variance: number;
    }[];
    aggregated: {
        rmse: number;
        peakRmse: number;
    };
}

interface ProcessedReport {
    measurement: QualityMeasurement | FrameTimeMeasurement;
    launchParameters: any;
    logTypeName: "variance" | "frame time";
}

export default function loadReportCollection(
    reportCollection: ReportCollection
): ProcessedReport[] {
    validators.ReportCollection.check(reportCollection);

    return reportCollection.reports.map((report) => {
        return {
            measurement: (() => {
                switch (report.measurement["frameVarianceRecords"]) {
                    case undefined:
                        return report.measurement as FrameTimeMeasurement;
                    default:
                        const measurement: VarianceMeasurement =
                            report.measurement as VarianceMeasurement;

                        const varianceByFrame =
                            measurement.frameVarianceRecords;
                        varianceByFrame.forEach((varianceRecord) => {
                            if (varianceRecord.variance < 0) {
                                varianceRecord.variance = null;
                            }
                        });

                        const aggregated = {
                            rmse: -1,
                            peakRmse: -1,
                        };
                        aggregated.rmse =
                            (varianceByFrame
                                .map((frameRecord) => frameRecord.variance)
                                .reduce((a, b) => a + b) /
                                varianceByFrame.filter((x) => x !== null)
                                    .length) **
                            0.5;
                        aggregated.peakRmse = Math.max(
                            ...varianceByFrame.map(
                                (frameRecord) => frameRecord.variance ** 0.5
                            )
                        );

                        return {
                            rmseByFrame: measurement.frameVarianceRecords.map(
                                (record) => {
                                    return {
                                        frameIndex: record.frameIndex,
                                        variance: record.variance,
                                    };
                                }
                            ),
                            aggregated,
                        };
                }
            })(),
            launchParameters: report.serializableLaunchParameters,
            logTypeName: ((): "variance" | "frame time" => {
                switch (report.logType) {
                    case 0:
                        assert.notEqual(
                            report.measurement["frameTimeRecords"],
                            undefined
                        );
                        return "frame time";
                    case 1:
                        assert.notEqual(
                            report.measurement["frameVarianceRecords"],
                            undefined
                        );
                        return "variance";
                    default:
                        throw new Error("incorrect report type");
                }
            })(),
        };
    });
}
