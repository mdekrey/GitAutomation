import { Observable } from "../utils/rxjs";
import { ApolloClient, HttpLink, InMemoryCache } from "apollo-client-preset";
import { DocumentNode } from "graphql";
import { ApolloQueryResult } from "apollo-client";

const client = new ApolloClient({
  link: new HttpLink({
    uri: "/api/graphql",
    fetchOptions: {
      credentials: "same-origin"
    }
  }),
  cache: new InMemoryCache().restore({})
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
  query: DocumentNode,
  variables: Record<string, any> | null = null
) =>
  Observable.fromPromise(
    client.query<TResult>({
      query,
      fetchPolicy: "network-only"
    })
  ).map(response => {
    if (!response.errors) {
      return response.data;
    }
    throw new GraphQLError(response);
  });
