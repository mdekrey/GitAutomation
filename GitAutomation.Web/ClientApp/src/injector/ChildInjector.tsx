import React from "react";
import { Scope } from "./Scope";
import { injectorContext } from "./injectorContext";

export const ChildInjector = ({
  beginScopes,
  children,
}: {
  beginScopes: Scope[];
  children?: React.ReactNode;
}) => {
  const injector = React.useContext(injectorContext);
  const childInjector = React.useMemo(() => {
    const result = injector.createChildInjector();
    for (const scope of beginScopes) {
      result.beginScope(scope);
    }
    return result;
    // eslint-disable-next-line
  }, [injector]);
  return (
    <injectorContext.Provider value={childInjector}>
      {children}
    </injectorContext.Provider>
  );
};
