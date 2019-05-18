import * as signalR from "@aspnet/signalr";
import { Observable } from "rxjs";
import { map, switchMap, shareReplay } from "rxjs/operators";
import { BranchReserve } from "./BranchReserve";
import { ReserveConfiguration } from "./ReserveConfiguration";

function adapt<T = any>(stream: signalR.IStreamResult<T>): Observable<T> {
  return new Observable(observer => {
    const subscription = stream.subscribe(observer);
    return () => subscription.dispose();
  });
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

  private graphQl$(gql: string) {
    return this.connectivity
      .pipe(switchMap(connection => adapt(connection.stream("Query", gql))))
      .pipe(map(d => JSON.parse(d)));
  }

  public readonly reserveTypes$ = this.graphQl$(
    `{ Configuration { Configuration { ReserveTypes } } }`
  ).pipe(
    map(data => data.Configuration.Configuration.ReserveTypes),
    map(data => data as Record<string, ReserveConfiguration>),
    shareReplay(1)
  );

  public readonly reserves$ = this.graphQl$(
    `{ Configuration { Structure { BranchReserves } } }`
  ).pipe(
    map(data => data.Configuration.Structure.BranchReserves),
    map(data => data as Record<string, BranchReserve>),
    shareReplay(1)
  );

  public readonly branches$ = this.graphQl$(`{ Target { Branches } }`).pipe(
    map(data => data.Target.Branches),
    map(data => data as Record<string, string>),
    shareReplay(1)
  );
}
