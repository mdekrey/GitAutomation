import { Observable } from "rxjs";
import { rxData } from "./utils/presentation/d3-binding";

const domChanged = Observable.of(null);

function watchElements<T extends Element>(query: string): Observable<T> {
  return domChanged
    .map(() => document.querySelector(query) as T | null)
    .filter(Boolean)
    .distinctUntilChanged();
}

rxData<string, HTMLUListElement>(
  watchElements<HTMLUListElement>(`[data-locator="remote-branches"]`),
  Observable.ajax("/api/management/remote-branches").map(
    response => response.response as string[]
  )
).bind({
  element: "li",
  selector: "li",
  onEach: selection => {
    selection.text(data => data);
  }
});

rxData<{}, HTMLUListElement>(
  watchElements<HTMLUListElement>(`[data-locator="status"]`),
  Observable.ajax("/api/management/log").map(
    response => response.response as {}[]
  )
).bind({
  element: "li",
  selector: "li",
  onEach: selection => {
    selection.text(data => JSON.stringify(data));
  }
});
