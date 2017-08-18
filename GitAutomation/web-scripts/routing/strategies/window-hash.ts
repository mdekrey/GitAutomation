import { windowUrlChanged } from "./window-base";
import { buildDefaultState } from "../operations";
import { buildStrategy } from "../strategy";

const windowHash = windowUrlChanged.map(
  () => window.location.hash.slice(1) || "/"
);

function navigateHash({
  url,
  replaceCurentHistory
}: {
  url: string;
  replaceCurentHistory: boolean;
}) {
  console.log(url, replaceCurentHistory);
  if (typeof history !== "undefined") {
    if (!replaceCurentHistory) {
      history.pushState({}, "", "#" + url);
    } else {
      history.replaceState({}, "", "#" + url);
    }
  }
  windowUrlChanged.next(null);
}

export const windowHashStrategy = buildStrategy(
  windowHash.map(buildDefaultState),
  navigateHash
);
