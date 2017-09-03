import { windowUrlChanged } from "./window-base";
import { buildDefaultState } from "../operations";
import { buildStrategy } from "../strategy";

const windowPath = windowUrlChanged.map(() => window.location.pathname);

function navigatePath({
  url,
  replaceCurentHistory
}: {
  url: string;
  replaceCurentHistory: boolean;
}) {
  if (typeof history !== "undefined") {
    if (!replaceCurentHistory) {
      history.pushState({}, "", url);
    } else {
      history.replaceState({}, "", url);
    }
  }
  windowUrlChanged.next(null);
}

export const windowPathStrategy = buildStrategy(
  windowPath.map(buildDefaultState),
  navigatePath
);
