"use strict";
exports.__esModule = true;
exports.ReportCollection = exports.Report = exports.FrameTimeMeasurement = exports.VarianceMeasurement = exports.Measurement = void 0;
/**
 * This module was automatically generated by `ts-interface-builder`
 */
var t = require("ts-interface-checker");
// tslint:disable:object-literal-key-quotes
exports.Measurement = t.iface([], {});
exports.VarianceMeasurement = t.iface(["Measurement"], {
    "varianceByFrame": t.array(t.iface([], {
        "frameIdex": "number",
        "variance": "number"
    }))
});
exports.FrameTimeMeasurement = t.iface(["Measurement"], {
    "peakFrameTime": "number",
    "avgFrameTime": "number"
});
exports.Report = t.iface([], {
    "measurement": t.union("VarianceMeasurement", "FrameTimeMeasurement"),
    "serializableLaunchParameters": t.iface([], {
        "dispatcherParameters": "any",
        "videoName": "string",
        "numIterations": "number",
        "workingTextureSize": "number",
        "numClusters": "number",
        "jitterSize": "number",
        "staggeredJitter": "boolean",
        "doDownscale": "boolean",
        "algorithm": "string",
        "doRandomizeEmptyClusters": "boolean",
        "stopCondition": t.opt("boolean")
    }),
    "logType": "number"
});
exports.ReportCollection = t.iface([], {
    "reports": t.array("Report")
});
var exportedTypeSuite = {
    Measurement: exports.Measurement,
    VarianceMeasurement: exports.VarianceMeasurement,
    FrameTimeMeasurement: exports.FrameTimeMeasurement,
    Report: exports.Report,
    ReportCollection: exports.ReportCollection
};
exports["default"] = exportedTypeSuite;
