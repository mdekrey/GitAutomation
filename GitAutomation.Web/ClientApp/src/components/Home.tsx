import React from "react";
import { useService } from "../injector";
import { useObservable } from "../rxjs";
import "../api";

export function Home() {
  const api = useService("api");
  const reserveTypes = useObservable(
    api.reserveTypes$,
    {} as Record<string, { Description: string }>,
    [api]
  );
  return (
    <div>
      <h1>Hello, world!</h1>
      <h2>Reserve Types</h2>
      <dl>
        {Object.keys(reserveTypes).map(key => (
          <React.Fragment key={key}>
            <dt>{key}</dt>
            <dd>{reserveTypes[key].Description}</dd>
          </React.Fragment>
        ))}
      </dl>
    </div>
  );
}
