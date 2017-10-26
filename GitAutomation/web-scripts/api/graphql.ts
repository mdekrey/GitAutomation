export interface IGraphQLResponse {
  data: any;
  errors: {
    message: string;
    locations: { line: number; column: number }[];
  }[];
}

import { Observable } from "../utils/rxjs";

export class GraphQLError extends Error {
  constructor(public readonly response: IGraphQLResponse) {
    super(
      "GraphQL response contained errors. See full body in `response` on this object."
    );
  }
}

const jsonMimeType = {
  "Content-Type": "application/json"
};

// TODO - provide "dataloader" like functionality on this side to combine queries
export const graphQl = <TResult>(
  query: string,
  variables: Record<string, any> | null = null
) =>
  Observable.ajax
    .post(
      "/api/graphql",
      {
        query,
        variables: variables ? JSON.stringify(variables) : null
      },
      jsonMimeType
    )
    .map(response => {
      if (!response.response.errors) {
        return response.response.data as TResult;
      }
      throw new GraphQLError(response.response);
    });
