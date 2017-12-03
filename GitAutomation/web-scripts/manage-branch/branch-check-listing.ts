import { IRxBindProps, bind } from "../utils/presentation/d3-binding";
import { IBranchData } from "./data";
import { branchNameDisplay } from "../branch-name-display";
import { applyStyles } from "../style/style-binding";
import { Observable } from "../utils/rxjs";
import { take } from "../utils/ramda";
import { Selection } from "d3-selection";
import { RoutingNavigate } from "../routing";
import { checkboxChecked } from "../utils/inputs";
import { style } from "typestyle";

const prListing = style({
  margin: 0,
  padding: 0,
  listStyle: "none"
});
const separator = style({
  $nest: {
    "&::after": {
      content: JSON.stringify(": ")
    }
  }
});

type BranchPredicate = (data: IBranchData) => boolean;
interface BranchTypeRules {
  checked: BranchPredicate;
  disabled: BranchPredicate;
}

const downstreamRules: BranchTypeRules = {
  checked: b => b.isDownstream,
  disabled: b => !b.isDownstreamAllowed && !b.isDownstream
};

const upstreamRules: BranchTypeRules = {
  checked: b => b.isUpstream,
  disabled: b => !b.isUpstreamAllowed && !b.isUpstream
};

export const buildBranchCheckListing = (
  styles: Record<string, string>,
  navigate: RoutingNavigate
): IRxBindProps<HTMLTableRowElement, IBranchData, any, any> => ({
  onCreate: target =>
    target.append<HTMLTableRowElement>("tr").attr("data-static-branch", ""),
  selector: "tr[data-static-branch]",
  onEnter: tr => {
    tr.html(require("./branch-check-listing.row.html"));
    applyStyles(styles)(tr);
  },
  onEach: selection => {
    branchNameDisplay(selection.select(`[data-locator="branch"]`), navigate);
    selection
      .select(`[data-locator="downstream-branches"] [data-locator="check"]`)
      .attr("data-direction", "downstream")
      .attr("data-branch", data => data.groupName)
      .property("checked", downstreamRules.checked)
      .property("disabled", downstreamRules.disabled);

    selection
      .select(`[data-locator="upstream-branches"] [data-locator="check"]`)
      .attr("data-direction", "upstream")
      .attr("data-branch", data => data.groupName)
      .property("checked", upstreamRules.checked)
      .property("disabled", upstreamRules.disabled);

    bind({
      target: selection
        .select(`[data-locator="pr-status"]`)
        .classed(prListing, true)
        .selectAll(`li[data-locator="pr"]`)
        .data(data => take(1, data.pullRequests)),
      onCreate: t => t.append("li").attr("data-locator", "pr"),
      onEnter: t => t.html(require("./pr-display.html")),
      onEach: t => {
        t
          .select(`[data-locator="link"]`)
          .attr("href", pr => pr.url)
          .text(pr => `${pr.state} PR #${pr.id}`);
        t
          .select(`[data-locator="status"]`)
          .classed(separator, pr => Boolean(pr.reviews.length));

        bind({
          target: t
            .select(`[data-locator="reviews"]`)
            .selectAll("a")
            .data(pr => pr.reviews || []),
          onCreate: e =>
            e
              .append("a")
              .attr("target", "_blank")
              .classed("normal", true),
          onEach: e =>
            e
              .attr("href", review => review.url)
              .html(
                review =>
                  review.state === "Approved"
                    ? `<img alt="Approved" class="text-image" src="${require("../images/green-check.svg")}" />`
                    : review.state === "ChangesRequested"
                      ? `<img alt="Rejected" class="text-image" src="${require("../images/red-x.svg")}" />`
                      : `<img alt="Comment Only" class="text-image" src="${require("../images/question-mark.svg")}" />`
              )
              .attr("title", review => review.author)
        });
      }
    });

    selection.sort((a, b) => a.groupName.localeCompare(b.groupName));
  }
});

export const checkedData = (
  table: Observable<Selection<HTMLTableRowElement, any, any, any>>,
  onlyOnChanged = false
) => {
  return table
    .map(e =>
      e.selectAll<HTMLInputElement, any>(
        `[data-locator="other-branches"] input`
      )
    )
    .let(target =>
      target
        .let(checkboxChecked({ includeInitial: !onlyOnChanged }))
        .withLatestFrom(target, (_, inputs) => inputs)
    )
    .map(inputs => {
      return {
        downstream: inputs
          .filter(`[data-direction="downstream"]:checked`)
          .nodes()
          .map(i => i.getAttribute("data-branch")!),
        upstream: inputs
          .filter(`[data-direction="upstream"]:checked`)
          .nodes()
          .map(i => i.getAttribute("data-branch")!)
      };
    });
};
