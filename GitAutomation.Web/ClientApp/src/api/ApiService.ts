import { ajax } from "rxjs/ajax";
import { map, switchMap, shareReplay, tap } from "rxjs/operators";
import * as signalR from "@aspnet/signalr";
import { Observable } from "rxjs";

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
      console.log("starting...");
      connection
        .start()
        .then(() => observer.next(connection), err => observer.error(err));
      return () => {
        console.log("stopped...");
        connection.stop();
      };
    }).pipe(
      tap(v => console.log(v), err => console.warn(err)),
      shareReplay(1)
    );
  }

  get reserveTypes$() {
    return this.connection
      .pipe(
        switchMap(connection =>
          adapt(
            connection.stream(
              "Query",
              `{ Configuration { Configuration { ReserveTypes } } }`
            )
          )
        )
      )
      .pipe(
        map(
          data =>
            JSON.parse(data).Configuration.Configuration.ReserveTypes as Record<
              string,
              { Description: string }
            >
        )
      );
  }
}
