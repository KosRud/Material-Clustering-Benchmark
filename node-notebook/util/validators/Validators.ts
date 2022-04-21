/*
    node notebook addon caches imports
    restart VS code after re-generating validators
*/

export interface ReportCollection {
    reports: [
        {
            measurement: {
                varianceByFrame: [
                    {
                        frameIdex: number;
                        variance: number;
                    }
                ];
            };
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
            };
            logType: number;
        }
    ];
}
