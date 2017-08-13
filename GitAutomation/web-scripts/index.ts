import { Observable } from "rxjs";
import { rxData } from "./utils/presentation/d3-binding";
import { select as d3select } from "d3-selection";

rxData<string, HTMLUListElement>(
  Observable.of(
    d3select<HTMLUListElement, null>(`[data-locator="remote-branches"]`).node()!
  ),
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
