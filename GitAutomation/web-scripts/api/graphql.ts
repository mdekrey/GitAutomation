import { Observable, Observer } from "../utils/rxjs";
import { ApolloClient, HttpLink, InMemoryCache } from "apollo-client-preset";
import { ApolloQueryResult, WatchQueryOptions } from "apollo-client";
import { DocumentNode } from "graphql";

const client = new ApolloClient({
  link: new HttpLink({
    uri: "/api/graphql",
    fetchOptions: {
      credentials: "same-origin"
    }
  }),
  cache: new InMemoryCache().restore({}),
  defaultOptions: {
    watchQuery: {
      fetchPolicy: "cache-and-network"
    }
  }
});

export class GraphQLError<T> extends Error {
  constructor(public readonly response: ApolloQueryResult<T>) {
    super(
      "GraphQL response contained errors. See full body in `response` on this object."
    );
  }
}

// TODO - provide "dataloader" like functionality on this side to combine queries
export const graphQl = <TResult>(
  options: WatchQueryOptions,
  { excludeErrors }: { excludeErrors: boolean }
) =>
  (Observable.create((observer: Observer<TResult>) =>
    Observable.from(
      client.watchQuery<TResult>({
        pollInterval: 60000,
        ...options
      })
    )
      .filter(response => !excludeErrors || !response.errors)
      .map(response => {
        if (!response.errors) {
          return response.data;
        }
        throw new GraphQLError(response);
      })
      .subscribe(observer)
  ) as Observable<TResult>)
    .publishReplay(1)
    .refCount();

export const invalidateQuery = (query: DocumentNode) =>
  client.query({ query, fetchPolicy: "network-only" });
