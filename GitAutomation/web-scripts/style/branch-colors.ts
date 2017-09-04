import { rgb } from "csx";
import { BranchType } from "../api/basic-branch";

// hotfixes
export const hotfixColors = [
  rgb(255, 115, 59),
  rgb(255, 174, 141),
  rgb(255, 142, 97),
  rgb(255, 84, 15),
  rgb(214, 61, 0)
];

// features
export const featureColors = [
  rgb(55, 127, 192),
  rgb(132, 181, 225),
  rgb(88, 151, 207),
  rgb(23, 105, 178),
  rgb(9, 77, 139)
];

// release candidates
export const releaseCandidateColors = [
  rgb(111, 37, 111),
  rgb(166, 111, 166),
  rgb(138, 69, 138),
  rgb(83, 14, 83),
  rgb(55, 0, 55)
];

// service lines
export const serviceLineColors = [
  rgb(111, 206, 31),
  rgb(166, 233, 110),
  rgb(137, 219, 70),
  rgb(82, 167, 12),
  rgb(60, 132, 0)
];

export const integrationBranchColors = [
  rgb(98, 98, 98),
  rgb(127, 127, 127),
  rgb(112, 112, 112),
  rgb(83, 83, 83),
  rgb(67, 67, 67)
];

export const branchTypeColors: Record<BranchType, typeof featureColors> = {
  ServiceLine: serviceLineColors,
  Hotfix: hotfixColors,
  Infrastructure: featureColors,
  Feature: featureColors,
  Integration: integrationBranchColors,
  ReleaseCandidate: releaseCandidateColors
};
