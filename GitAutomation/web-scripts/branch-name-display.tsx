import * as React from "react";
import { Selection, event as d3event } from "d3-selection";
import { bind } from "./utils/presentation/d3-binding";
import { RoutingNavigate } from "./routing";
import { branchTypeColors } from "./style/branch-colors";
import { applyExternalLink, ExternalLink } from "./external-window-link";
import { BehaviorSubject } from "./utils/rxjs";
import { ContextComponent } from "./utils/routing-component";

export interface DisplayableBranch {
  groupName: string;
  branchType?: GitAutomationGQL.IBranchGroupTypeEnum;
  branches?: Array<Partial<GitAutomationGQL.IGitRef>>;
  latestBranch?: ({ name: string } & Partial<GitAutomationGQL.IGitRef>) | null;
}

export const hoveredGroupName = new BehaviorSubject<string | null>(null);

function findActualBranch<P extends keyof GitAutomationGQL.IGitRef>(
  data: DisplayableBranch,
  targetKey: P
) {
  if (data.latestBranch) {
    const latest: Partial<GitAutomationGQL.IGitRef> = data.latestBranch;
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

export interface IBranchNameDisplayProps {
  branch: DisplayableBranch;
}

export class BranchNameDisplay extends ContextComponent<
  IBranchNameDisplayProps
> {
  render() {
    const { branch } = this.props;
    return (
      <span onMouseEnter={this.beginHover} onMouseLeave={this.endHover}>
        <a
          data-locator="name"
          style={{
            color: branch.branchType
              ? branchTypeColors[branch.branchType][0].toHexString()
              : null,
            cursor: "pointer"
          }}
          href={this.context.injector.services.routeHrefBuilder(
            "/manage/" + branch.groupName
          )}
        >
          {branch.groupName}
        </a>
        <span data-locator="external-window">
          <ExternalLink url={findActualBranch(branch, "url")} />
        </span>
        <span data-locator="status" />
      </span>
    );
  }

  beginHover = () => hoveredGroupName.next(this.props.branch.groupName);
  endHover = () => hoveredGroupName.next(null);
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
      selection.on("mouseenter.globalGroupName", b => {
        hoveredGroupName.next(b.groupName);
      });
      selection.on("mouseleave.globalGroupName", b => {
        hoveredGroupName.next(null);
      });

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
      applyExternalLink(
        selection
          .select(`[data-locator="external-window"]`)
          .datum(data => findActualBranch(data, "url"))
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
            .classed("normal", true)
            .attr("target", "_blank")
            .on("click", stopPropagation),
        onEach: e =>
          e
            .html(
              s =>
                s.state === "Success"
                  ? `<img alt="Build Successful" class="text-image" src="${require("./images/green-check.svg")}" />`
                  : s.state === "Error"
                    ? `<img alt="Build Error" class="text-image" src="${require("./images/red-x.svg")}" />`
                    : `<img alt="Build Pending" class="text-image" src="${require("./images/question-mark.svg")}" />`
            )
            .attr("href", s => s.url)
            .attr("title", s => `${s.key} - ${s.description}`)
      });
    }
  });
