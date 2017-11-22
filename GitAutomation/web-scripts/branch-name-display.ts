import { Selection } from "d3-selection";
import { bind } from "./utils/presentation/d3-binding";
import { RoutingNavigate } from "./routing";
import { branchTypeColors } from "./style/branch-colors";

export interface DisplayableBranch {
  groupName: string;
  branchType?: GitAutomationGQL.IBranchGroupTypeEnum;
  branches?: Array<Partial<GitAutomationGQL.IGitRef>>;
  latestBranch?: ({ name: string } & Partial<GitAutomationGQL.IGitRef>) | null;
}

function findStatuses(data: DisplayableBranch) {
  if (data.latestBranch) {
    const latest = data.latestBranch;
    if (latest.statuses) {
      return latest.statuses;
    }

    if (data.branches) {
      const found = data.branches.find(b => b.name === latest.name);
      if (found && found.statuses) {
        return found.statuses;
      }
    }
  }
  return [];
}

export const branchNameDisplay = (
  target: Selection<any, DisplayableBranch, any, any>,
  navigate?: RoutingNavigate
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
        .text(data => data.groupName)
        .style(
          "color",
          data =>
            data.branchType
              ? branchTypeColors[data.branchType][0].toHexString()
              : null
        )
        .style("cursor", "pointer")
        .on(
          "click",
          data =>
            navigate &&
            navigate({
              url: "/manage/" + data.groupName,
              replaceCurentHistory: false
            })
        );
      bind({
        target: selection
          .select(`span[data-locator="status"]`)
          .selectAll(`a[data-locator="status-entry"]`)
          .data(data => findStatuses(data)),
        onCreate: e =>
          e
            .append("a")
            .attr("data-locator", "status-entry")
            .attr("target", "_blank"),
        onEach: e =>
          e
            .html(
              s =>
                s.state === "Success" ? "✔️" : s.state === "Error" ? "❌" : "❔"
            )
            .attr("href", s => s.url)
            .attr("title", s => `${s.key} - ${s.description}`)
      });
    }
  });
