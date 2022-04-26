/**
 * This module was automatically generated by `ts-interface-builder`
 */
import * as t from "ts-interface-checker";
// tslint:disable:object-literal-key-quotes

export const ReportCollection = t.iface([], {
  "reports": t.tuple(t.iface([], {
    "measurement": t.iface([], {
      "varianceByFrame": t.tuple(t.iface([], {
        "frameIdex": "number",
        "variance": "number",
      })),
    }),
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
      "stopCondition": t.opt("boolean"),
    }),
    "logType": "number",
  })),
});

const exportedTypeSuite: t.ITypeSuite = {
  ReportCollection,
};
export default exportedTypeSuite;
