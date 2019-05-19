import React from "react";
import { TextLine, TextParagraph } from "../loading";
import "./CreateReserve.css";
import { Card, ActionBar, LinkButton, DisabledLinkButton } from "../common";
import { ReserveConfiguration } from "../../api";

export function ReserveSelection({
  reserveTypes,
}: {
  reserveTypes: Record<string, ReserveConfiguration> | undefined;
}) {
  return (
    <>
      <h1>Reserve Selection</h1>
      <div className="CreateReserve_reserves">
        {reserveTypes ? (
          Object.keys(reserveTypes).map(t => (
            <ReserveSelectionCard
              key={t}
              reserveName={t}
              reserveType={reserveTypes[t]}
            />
          ))
        ) : (
          <>
            <ReserveSelectionCard />
            <ReserveSelectionCard />
            <ReserveSelectionCard />
          </>
        )}
        <ReserveSelectionCard />
        <ReserveSelectionCard />
      </div>
    </>
  );
}

function ReserveSelectionCard({
  reserveName,
  reserveType,
}: {
  reserveName?: string;
  reserveType?: ReserveConfiguration;
}) {
  return (
    <Card>
      <h2>{reserveName || <TextLine />}</h2>
      {reserveType ? <p>{reserveType.Description}</p> : <TextParagraph />}

      <ActionBar>
        {reserveName ? (
          <LinkButton to={`?type=${reserveName}`}>Select</LinkButton>
        ) : (
          <>
            <DisabledLinkButton to="#">Select</DisabledLinkButton>
            <DisabledLinkButton to="#">Select</DisabledLinkButton>
            <DisabledLinkButton to="#">Select</DisabledLinkButton>
          </>
        )}
      </ActionBar>
    </Card>
  );
}
