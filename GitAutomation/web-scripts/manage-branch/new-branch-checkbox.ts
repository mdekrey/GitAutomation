import { Observable } from "rxjs";
import { Selection } from "d3-selection";

import { rxData } from "../utils/presentation/d3-binding";

export const newBranch = (
  container: Observable<Selection<HTMLElement, any, any, any>>
) =>
  rxData(container, Observable.of([{}])).bind({
    selector: `li[data-locator="new-branch"]`,
    onCreate: elem => elem.append("li").attr("data-locator", "new-branch"),
    onEnter: li =>
      li.html(`
        <input type="checkbox" data-locator="check"/>
        <input type="text" data-locator="branch-text" />
      `),
    onEach: target => {
      const input = target.select<HTMLInputElement>(
        `input[data-locator="branch-text"]`
      );
      const checkbox = target.select(`input[data-locator="check"]`);

      checkbox.property("disabled", true);
      if (input.nodes().length) {
        checkbox.property("checked", Boolean(input.property("value")));
      }

      input.on("input", function() {
        checkbox
          .property("checked", Boolean(this.value))
          .attr("data-branch", this.value);
      });
    }
  });
