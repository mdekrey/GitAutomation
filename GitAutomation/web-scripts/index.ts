import { Observable } from "rxjs";
import { rxData, rxEvent, d3element } from "./utils/presentation/d3-binding";
import { getLog, remoteBranches, fetch } from "./api/basics";
import { logPresentation } from "./logs/log.presentation";

const domChanged = Observable.of(null);

function watchElements<T extends Element>(query: string) {
  return domChanged
    .map(() => document.querySelector(query) as T | null)
    .filter(Boolean)
    .distinctUntilChanged()
    .map(d3element);
}

fetch(
  rxEvent({
    target: watchElements('[data-locator="fetch-from-remote"]'),
    eventName: "click"
  })
).subscribe();

rxData<string, HTMLUListElement>(
  watchElements<HTMLUListElement>(`[data-locator="remote-branches"]`),
  remoteBranches(
    rxEvent({
      target: watchElements('[data-locator="remote-branches-refresh"]'),
      eventName: "click"
    }).startWith(null)
  )
).bind({
  element: "li",
  selector: "li",
  onEach: selection => {
    selection.text(data => data);
  }
});

rxData(
  watchElements<HTMLUListElement>(`[data-locator="status"]`),
  getLog(
    rxEvent({
      target: watchElements('[data-locator="status-refresh"]'),
      eventName: "click"
    }).startWith(null)
  )
).bind(logPresentation);
