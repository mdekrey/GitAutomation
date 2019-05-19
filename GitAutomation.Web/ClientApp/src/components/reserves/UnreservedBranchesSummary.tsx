import React from "react";
import { BranchReserve } from "../../api";
import { TextParagraph } from "../loading";
import { Link } from "react-router-dom";

export function determineUnreservedBranches(
  reserves: Record<string, BranchReserve> | undefined,
  branches: Record<string, string> | undefined
) {
  if (reserves === undefined || branches === undefined) {
    return undefined;
  } else {
    const reservedBranches = Object.keys(reserves).flatMap(reserve =>
      Object.keys(reserves[reserve].IncludedBranches)
    );
    return Object.keys(branches).filter(
      b => reservedBranches.indexOf(b) === -1
    );
  }
}

export function UnreservedBranchesSummary({
  unreservedBranches,
}: {
  unreservedBranches: string[] | undefined;
}) {
  return (
    <>
      <h2>Unreserved Branches</h2>

      <p className="hint">
        Branches that aren't allocated don't serve a purpose in gitauto.
      </p>
      {unreservedBranches === undefined ? (
        <TextParagraph />
      ) : unreservedBranches.length === 0 ? (
        <>
          <p>
            All branches are allocated to a reserve. <em>That's great!</em>
          </p>
        </>
      ) : (
        <ul>
          {unreservedBranches.map(key => (
            <li key={key}>
              <Link to={`/create-reserve?branch=${key}`}>
                Create reserve for {key}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
