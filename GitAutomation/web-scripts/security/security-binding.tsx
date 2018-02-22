import * as React from "react";
import { claims } from "./app-access";
import { intersection } from "../utils/ramda";

export const Secured = ({
  roleNames,
  children
}: {
  roleNames: string[];
  children?: React.ReactNode;
}) =>
  claims
    .map(
      currentClaims => intersection(currentClaims.roles, roleNames).length > 0
    )
    .map(allowed => (allowed ? <>{children}</> : null))
    .asComponent();
