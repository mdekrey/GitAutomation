import { Selection } from "d3-selection";
import { bind } from "./utils/presentation/d3-binding";

export const branchNameDisplay = (
  target: Selection<
    any,
    Pick<GitAutomationGQL.IBranchGroupDetails, "groupName">,
    any,
    any
  >
) =>
  bind({
    target: target
      .selectAll<HTMLSpanElement, any>(
        `span[data-locator="branch-name-display"]`
      )
      .data(p => [p]),
    onCreate: target =>
      target
        .insert<HTMLSpanElement>("span", `*`)
        .attr("data-locator", "branch-name-display"),
    onEnter: span => span.html(require("./branch-name-display.html")),
    onEach: selection => {
      selection
        .select(`span[data-locator="name"]`)
        .text(data => data.groupName);
      // TODO - restore status
      // selection
      //   .select(`span[data-locator="status"]`)
      //   // TODO - styling around this
      //   .text(
      //     data =>
      //       (data.statuses || []).length
      //         ? `(${data.statuses[0].state})`
      //         : "(No status)"
      //   );
    }
  });
