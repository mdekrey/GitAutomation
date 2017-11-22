import { Selection, event as d3event } from "d3-selection";
import { bind } from "./utils/presentation/d3-binding";
import { RoutingNavigate } from "./routing";
import { branchTypeColors } from "./style/branch-colors";

export interface DisplayableBranch {
  groupName: string;
  branchType?: GitAutomationGQL.IBranchGroupTypeEnum;
  branches?: Array<Partial<GitAutomationGQL.IGitRef>>;
  latestBranch?: ({ name: string } & Partial<GitAutomationGQL.IGitRef>) | null;
}

function findActualBranch<P extends keyof GitAutomationGQL.IGitRef>(
  data: DisplayableBranch,
  targetKey: P
) {
  if (data.latestBranch) {
    const latest = data.latestBranch;
    if (latest[targetKey]) {
      return latest[targetKey];
    }

    if (data.branches) {
      const found = data.branches.find(b => b.name === latest.name);
      if (found && found[targetKey]) {
        return found[targetKey];
      }
    }
  }
  return undefined;
}

function findStatuses(data: DisplayableBranch) {
  return findActualBranch(data, "statuses") || [];
}

function stopPropagation() {
  d3event.stopPropagation();
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
      selection
        .select(`a[data-locator="external-link"]`)
        .datum(data => findActualBranch(data, "url"))
        .style("display", url => (url ? "inline" : "none"))
        .attr("href", url => url || "")
        .on("click", stopPropagation);
      bind({
        target: selection
          .select(`span[data-locator="status"]`)
          .selectAll(`a[data-locator="status-entry"]`)
          .data(data => findStatuses(data)),
        onCreate: e =>
          e
            .append("a")
            .attr("data-locator", "status-entry")
            .classed("normal", true)
            .attr("target", "_blank")
            .on("click", stopPropagation),
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
