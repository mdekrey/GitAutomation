import { rgb } from "csx";
import { BranchType } from "../api/basic-branch";

// hotfixes
export const hotfixColors = [rgb(255, 115, 59)];

// features
export const featureColors = [rgb(55, 127, 192)];

// release candidates
export const releaseCandidateColors = [rgb(111, 37, 111)];

// service lines
export const serviceLineColors = [rgb(111, 206, 31)];

export const integrationBranchColors = [rgb(98, 98, 98)];

export const infrastructureBranchColors = [rgb(83, 166, 111)];

export const branchTypeColors: Record<BranchType, typeof featureColors> = {
  ServiceLine: serviceLineColors,
  Hotfix: hotfixColors,
  Infrastructure: infrastructureBranchColors,
  Feature: featureColors,
  Integration: integrationBranchColors,
  ReleaseCandidate: releaseCandidateColors
};
