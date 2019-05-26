import React from "react";
import { TextLine } from "../loading";
import "./ReserveLabel.css";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";

export function ReserveLabel({ reserveName }: { reserveName: string | null }) {
  const api = useService("api");
  const reserveTypes = useObservable(api.reserveTypes$, undefined, [api]);
  const reserveType =
    reserveName && reserveTypes ? reserveTypes[reserveName] : null;

  return reserveType ? (
    <>
      {reserveType.Title}
      {reserveType.Color ? (
        <span
          className="ReserveLabel_dot"
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
