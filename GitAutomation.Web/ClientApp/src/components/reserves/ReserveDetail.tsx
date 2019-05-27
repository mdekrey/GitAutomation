import React from "react";
import { RouteComponentProps } from "react-router-dom";
import { ReserveLabel } from "./ReserveLabel";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import { map } from "rxjs/operators";
import { TextLine, TextParagraph } from "../loading";
import { never } from "rxjs";
import { DiffDisplay } from "./DiffDisplay";

function MetaDisplay({ meta }: { meta: Record<string, any> }) {
  return (
    <>
      {Object.keys(meta).map(key => (
        <React.Fragment key={key}>
          <dt>{key}</dt>
          <dd>
            {typeof meta[key] === "string"
              ? meta[key]
              : JSON.stringify(meta[key])}
          </dd>
        </React.Fragment>
      ))}
    </>
  );
}

export function ReserveDetail({
  history,
  location,
  match,
}: RouteComponentProps<{ reserve: string }>) {
  const reserve = match.params.reserve;
  const api = useService("api");
  const reserveData = useObservable(
    api.reserves$.pipe(map(r => r[reserve])),
    undefined,
    [api, reserve]
  );
  const diffData = useObservable(
    reserveData ? api.revisionDiff(reserveData.OutputCommit) : never(),
    { Reserves: [], Branches: [] },
    [api, reserveData]
  );
  const basis = Math.max(
    ...[...diffData.Reserves, ...diffData.Branches].map(r =>
      Math.max(r.ahead, r.behind)
    )
  );

  return (
    <>
      <h1>
        Reserve: {reserve}&nbsp;&mdash;&nbsp;
        <ReserveLabel
          reserveName={reserveData ? reserveData.ReserveType : null}
        />
      </h1>
      <dl>
        <dt>Status:</dt>
        <dd>{reserveData ? reserveData.Status : <TextLine />}</dd>
        <dt>Flow Type:</dt>
        <dd>{reserveData ? reserveData.FlowType : <TextLine />}</dd>
        {reserveData ? <MetaDisplay meta={reserveData.Meta} /> : null}
        <dt>Upstrem Reserves:</dt>
        <dd>
          {reserveData ? (
            <dl>
              {Object.keys(reserveData.Upstream).map(upstream => (
                <React.Fragment key={upstream}>
                  <dt>{upstream}</dt>
                  <dd>
                    <dl>
                      <dt>Diff</dt>
                      <dd>
                        {diffData.Reserves.filter(r => r.name === upstream).map(
                          r => (
                            <DiffDisplay basis={basis} {...r} />
                          )
                        )}
                      </dd>
                      <dt>Role</dt>
                      <dd>{reserveData.Upstream[upstream].Role}</dd>
                      <MetaDisplay meta={reserveData.Upstream[upstream].Meta} />
                    </dl>
                  </dd>
                </React.Fragment>
              ))}
            </dl>
          ) : (
            <TextParagraph />
          )}
        </dd>
        <dt>Included Branches:</dt>
        <dd>
          {reserveData ? (
            <dl>
              {Object.keys(reserveData.IncludedBranches).map(branch => (
                <React.Fragment key={branch}>
                  <dt>{branch}</dt>
                  <dd>
                    <dl>
                      <dt>Diff</dt>
                      <dd>
                        {diffData.Branches.filter(r => r.name === branch).map(
                          r => (
                            <DiffDisplay basis={basis} {...r} />
                          )
                        )}
                      </dd>
                      <MetaDisplay
                        meta={reserveData.IncludedBranches[branch].Meta}
                      />
                    </dl>
                  </dd>
                </React.Fragment>
              ))}
            </dl>
          ) : (
            <TextParagraph />
          )}
        </dd>
      </dl>
    </>
  );
}
