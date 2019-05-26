import React from "react";
import { BranchReserve } from "../../api";
import { groupBy } from "../../data-manipulators";
import { TextLine } from "../loading";
import { CardContents, CardActionBar, LinkButton } from "../common";
import { ReserveLabel } from "./ReserveLabel";

export function ReservesSummary({
  reserves = {},
}: {
  reserves: Record<string, BranchReserve> | undefined;
}) {
  const reservesKeys = Object.keys(reserves || {});
  const groups = groupBy(
    reservesKeys,
    current => reserves[current].ReserveType
  );
  const reserveTypes = Object.keys(groups);
  reserveTypes.sort();
  return (
    <>
      <CardContents>
        <h2>Reserves</h2>
        <p className="hint">
          A reserve sets the rules for one or more branches, <em>reserving</em>{" "}
          them for a specific purpose.
        </p>
        {reserves === undefined ? (
          <ul>
            <li>
              <TextLine />
            </li>
            <li>
              <TextLine />
            </li>
            <li>
              <TextLine />
            </li>
          </ul>
        ) : (
          <>
            {reserveTypes.length === 0 ? (
              <>
                <p>No reserves.</p>
              </>
            ) : (
              <ul>
                {reserveTypes.map(reserveType => (
                  <li key={reserveType}>
                    <ReserveLabel reserveName={reserveType} />
                    &nbsp;&mdash;&nbsp;
                    {groups[reserveType].length}
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </CardContents>
      {reserves === undefined ? null : (
        <CardActionBar>
          <LinkButton to={"/create-reserve"} className="button button-margin">
            Create a Reserve
          </LinkButton>
          {reservesKeys.length === 0 ? null : (
            <>
              <LinkButton to={"/reserve-flows"}>View flows</LinkButton>
              <LinkButton to={"/reserves"}>View details</LinkButton>
            </>
          )}
        </CardActionBar>
      )}
    </>
  );
}
