import * as signalR from "@aspnet/signalr";
import { Observable, of } from "rxjs";
import { ajax } from "rxjs/ajax";
import { map, switchMap, shareReplay } from "rxjs/operators";
import { BranchReserve } from "./BranchReserve";
import { ReserveConfiguration } from "./ReserveConfiguration";

function adapt<T = any>(stream: signalR.IStreamResult<T>): Observable<T> {
  return new Observable(observer => {
    const subscription = stream.subscribe(observer);
    return () => subscription.dispose();
  });
}

export interface RevisionDiff {
  name: string;
  commit: string;
  ahead: number;
  behind: number;
}

export class ApiService {
  private readonly connection: signalR.HubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hub")
    .build();
  private readonly connectivity = new Observable<signalR.HubConnection>(
    observer => {
      this.connection
        .start()
        .then(() => observer.next(this.connection), err => observer.error(err));
      return () => {
        this.connection.stop();
      };
    }
  ).pipe(shareReplay(1));

  private graphQl$<T>(
    gql: string,
    select: (d: any) => any,
    cast: (d: any) => T
  ) {
    return this.connectivity
      .pipe(switchMap(connection => adapt(connection.stream("Query", gql))))
      .pipe(
        map(d => JSON.parse(d)),
        map(select),
        map(cast),
        shareReplay(1)
      );
  }

  public readonly reserveTypes$ = this.graphQl$(
    `{ Configuration { Configuration { ReserveTypes } } }`,
    data => data.Configuration.Configuration.ReserveTypes,
    data => data as Record<string, ReserveConfiguration>
  );

  public readonly reserves$ = this.graphQl$(
    `{ Configuration { Structure { BranchReserves } } }`,
    data => data.Configuration.Structure.BranchReserves,
    data => data as Record<string, BranchReserve>
  );

  public readonly branches$ = this.graphQl$(
    `{ Target { Branches } }`,
    data => data.Target.Branches,
    data => data as Record<string, string>
  );

  public readonly flowTypes$ = of(["Automatic"]);

  public submitAction(action: {
    action: string;
    payload: Record<string, any>;
  }) {
    return ajax.post("/api/action", action, {
      "Content-Type": "application/json",
    });
  }

  public revisionDiff(commit: string) {
    return ajax.get(`/api/git/revision-diffs/${commit}`).pipe(
      map(
        resp =>
          resp.response as {
            Reserves: RevisionDiff[];
            Branches: RevisionDiff[];
          }
      )
    );
  }
}
