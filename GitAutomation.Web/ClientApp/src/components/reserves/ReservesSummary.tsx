import React from "react";
import { BranchReserve } from "../../api";
import { TextParagraph } from "../loading";
import { Link } from "react-router-dom";
import { ActionBar } from "../common";

export function ReservesSummary({
  reserves,
}: {
  reserves: Record<string, BranchReserve> | undefined;
}) {
  const reservesKeys = Object.keys(reserves || {});
  return (
    <>
      <h2>Reserves</h2>
      {reserves === undefined ? (
        <TextParagraph />
      ) : (
        <>
          {reservesKeys.length === 0 ? (
            <>
              <p>No reserves.</p>
              <p className="hint">
                A reserve sets the rules for one or more branches,{" "}
                <em>reserving</em> them for a specific purpose.
              </p>
            </>
          ) : (
            <dl>
              {reservesKeys.map(key => (
                <React.Fragment key={key}>
                  <dt>{key}</dt>
                  <dd>{reserves[key].ReserveType}</dd>
                </React.Fragment>
              ))}
            </dl>
          )}
          <ActionBar>
            <Link to={"/create-reserve"} className="button button-margin">
              Create a Reserve
            </Link>
            {reservesKeys.length === 0 ? null : (
              <Link to={"/reserve-flows"}>View flows</Link>
            )}
          </ActionBar>
        </>
      )}
    </>
  );
}
