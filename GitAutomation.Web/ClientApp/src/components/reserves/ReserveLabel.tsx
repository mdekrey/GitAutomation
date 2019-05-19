import React from "react";
import { TextLine } from "../loading";
import "./CreateReserve.css";
import { ReserveConfiguration } from "../../api";

export function ReserveLabel({
  reserveType,
}: {
  reserveType: ReserveConfiguration | undefined;
}) {
  return reserveType ? (
    <>
      {reserveType.Title}
      {reserveType.Color ? (
        <span
          className="ReserveSelection_dot"
          style={
            {
              "--dot-color": `#${reserveType.Color}`,
            } as any
          }
        />
      ) : null}
    </>
  ) : (
    <TextLine />
  );
}
