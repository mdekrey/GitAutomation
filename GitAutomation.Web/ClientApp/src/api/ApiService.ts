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
  private readonly connection: Observable<signalR.HubConnection>;

  constructor() {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hub")
      .build();
    this.connection = new Observable<signalR.HubConnection>(observer => {
      connection
        .start()
        .then(() => observer.next(connection), err => observer.error(err));
      return () => {
        connection.stop();
      };
    }).pipe(shareReplay(1));
  }

  private graphQl$(gql: string) {
    return this.connection
      .pipe(switchMap(connection => adapt(connection.stream("Query", gql))))
      .pipe(map(d => JSON.parse(d)));
  }

  get reserveTypes$() {
    return this.graphQl$(
      `{ Configuration { Configuration { ReserveTypes } } }`
    ).pipe(
      map(data => data.Configuration.Configuration.ReserveTypes),
      map(data => data as Record<string, ReserveConfiguration>)
    );
  }

  get reserves$() {
    return this.graphQl$(
      `{ Configuration { Structure { BranchReserves } } }`
    ).pipe(
      map(data => data.Configuration.Structure.BranchReserves),
      map(data => data as Record<string, BranchReserve>)
    );
  }

  get branches$() {
    return this.graphQl$(`{ Target { Branches } }`).pipe(
      map(data => data.Target.Branches),
      map(data => data as Record<string, string>)
    );
  }
}
