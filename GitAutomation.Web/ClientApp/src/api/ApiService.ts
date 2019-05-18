import { ajax } from "rxjs/ajax";
import { map } from "rxjs/operators";

export class ApiService {
  get reserveTypes$() {
    return ajax
      .post(
        "/api/graphql",
        { query: `{ Configuration { Configuration { ReserveTypes } } }` },
        {
          "Content-Type": "application/json",
          Accept: "application/json",
        }
      )
      .pipe(
        map(
          response =>
            response.response.data.Configuration.Configuration
              .ReserveTypes as Record<string, { Description: string }>
        )
      );
  }
}
