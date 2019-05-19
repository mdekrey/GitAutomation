import React from "react";
import { TextLine, TextParagraph } from "../loading";
import "./CreateReserve.css";
import {
  Button,
  Card,
  CardContents,
  CardActionBar,
  DisabledButton,
  ButtonStyle,
} from "../common";
import { ReserveConfiguration } from "../../api";

export function ReserveSelection({
  reserveTypes,
  onSelectReserveType,
}: {
  reserveTypes: Record<string, ReserveConfiguration> | undefined;
  onSelectReserveType: (reserveType: string) => void;
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
              onSelect={() => onSelectReserveType(t)}
            />
          ))
        ) : (
          <>
            <ReserveSelectionCard />
            <ReserveSelectionCard />
            <ReserveSelectionCard />
            <ReserveSelectionCard />
          </>
        )}
      </div>
    </>
  );
}

function ReserveSelectionCard({
  reserveName,
  reserveType,
  onSelect,
}: {
  reserveName?: string;
  reserveType?: ReserveConfiguration;
  onSelect?: () => void;
}) {
  return (
    <Card>
      <CardContents>
        <h2>
          {reserveType ? reserveType.Title : <TextLine />}
          {reserveType && reserveType.Color ? (
            <span
              className="ReserveSelection_dot"
              style={
                {
                  "--dot-color": `#${reserveType.Color}`,
                } as any
              }
            />
          ) : null}
        </h2>
        {reserveType ? <p>{reserveType.Description}</p> : <TextParagraph />}
      </CardContents>

      <CardActionBar>
        {reserveType && reserveType.HelpLink ? (
          <ButtonStyle
            Component="a"
            href={reserveType.HelpLink}
            target="_blank">
            More Info
          </ButtonStyle>
        ) : null}
        {onSelect ? (
          <Button onClick={onSelect}>Select</Button>
        ) : (
          <DisabledButton>Select</DisabledButton>
        )}
      </CardActionBar>
    </Card>
  );
}
