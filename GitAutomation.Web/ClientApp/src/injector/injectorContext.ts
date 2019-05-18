import { createContext } from "react";
import { injectorBuilder } from "./injectorBuilder";
import { Scope } from "./Scope";

const defaultInjector = injectorBuilder.build();
defaultInjector.beginScope(Scope.Singleton);

export const injectorContext = createContext(defaultInjector);
