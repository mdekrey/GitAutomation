import { Resolver } from "./Resolver"

export type Resolvers<TServices extends {}> = {
  [service in keyof TServices]: Resolver<TServices, TServices[service]>
}
