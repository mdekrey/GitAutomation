import { BehaviorSubject, Observable } from "rxjs";
import { Selection } from "d3-selection";

import { rxData } from "../utils/presentation/d3-binding";

export const newBranch = (
  container: Observable<Selection<HTMLElement, any, any, any>>
) => {
  const label = new BehaviorSubject<string>("");
  return rxData(container, label.map(v => [v])).bind({
    selector: `li[data-locator="new-branch"]`,
    onCreate: elem => elem.append("li").attr("data-locator", "new-branch"),
    onEnter: li =>
      li.html(`
        <input type="checkbox" data-locator="check"/>
        <input type="text" data-locator="branch-text" />
      `),
    onEach: target => {
      target
        .select(`input[data-locator="check"]`)
        .property("disabled", true)
        .property("checked", d => Boolean(d))
        .attr("data-branch", d => d);
      target
        .select<HTMLInputElement>(`input[data-locator="branch-text"]`)
        .property("value", d => d)
        .on("input", function(_, index) {
          label.next(this.value);
        });
    }
  });
};
