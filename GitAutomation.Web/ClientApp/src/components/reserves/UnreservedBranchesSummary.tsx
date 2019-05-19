import React from "react";
import { TextParagraph } from "../loading";
import { Link } from "react-router-dom";
import { CardContents } from "../common";

export function UnreservedBranchesSummary({
  unreservedBranches,
}: {
  unreservedBranches: string[] | undefined;
}) {
  return (
    <CardContents>
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
    </CardContents>
  );
}
