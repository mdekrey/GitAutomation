import * as R from "ramda";
type R = typeof R;
export const any: R["any"] = require("ramda/src/any");
export const difference: R["difference"] = require("ramda/src/difference");
export const equals: R["equals"] = require("ramda/src/equals");
export const flatten: R["flatten"] = require("ramda/src/flatten");
export const fromPairs: R["fromPairs"] = require("ramda/src/fromPairs");
export const indexBy: {
  <T>(fn: (v: T) => string, input: T[]): Record<string, T>;
  <T>(fn: (v: T) => string): (input: T[]) => Record<string, T>;
} = require("ramda/src/indexBy");
export const intersection: R["intersection"] = require("ramda/src/intersection");
export const mapObjIndexed: R["mapObjIndexed"] = require("ramda/src/mapObjIndexed");
export const merge: R["merge"] = require("ramda/src/merge");
export const sortBy: R["sortBy"] = require("ramda/src/sortBy");
export const take: R["take"] = require("ramda/src/take");
export const toPairs: R["toPairs"] = require("ramda/src/toPairs");
export const values: <T>(
  record: Record<string, T>
) => T[] = require("ramda/src/values");
export const zip: R["zip"] = require("ramda/src/zip");
