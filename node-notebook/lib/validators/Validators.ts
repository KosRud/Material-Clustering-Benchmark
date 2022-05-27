/*
    node notebook addon caches imports
    restart VS code after re-generating validators
*/

export interface Measurement {}

export interface VarianceMeasurement extends Measurement {
    varianceByFrame: {
        frameIndex: number;
        variance: number;
    }[];
}

export interface FrameTimeMeasurement extends Measurement {
    peakFrameTime: number;
    avgFrameTime: number;
}

export interface Report {
    measurement: VarianceMeasurement | FrameTimeMeasurement;
    serializableLaunchParameters: {
        dispatcherParameters: any;
        videoName: string;
        numIterations: number;
        workingTextureSize: number;
        numClusters: number;
        jitterSize: number;
        staggeredJitter: boolean;
        doDownscale: boolean;
        algorithm: string;
        doRandomizeEmptyClusters: boolean;
        stopCondition: boolean;
    };
    logType: number;
}
export interface ReportCollection {
    reports: Report[];
}
