import { IRxBindProps, rxEvent } from "../utils/presentation/d3-binding";
import { IBranchData } from "./data";
import { branchNameDisplay } from "../branch-name-display";
import { applyStyles } from "../style/style-binding";
import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { RoutingNavigate } from "../routing";

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

    selection
      .select(`[data-locator="upstream-branches"] [data-locator="pr-status"]`)
      .attr("data-branch", data => data.groupName);

    selection.sort((a, b) => a.groupName.localeCompare(b.groupName));
  }
});

export const checkedData = (
  target: Observable<Selection<HTMLTableRowElement, any, any, any>>,
  onlyOnChanged = false
) =>
  target
    .map(e =>
      e.selectAll<HTMLInputElement, any>(
        `[data-locator="other-branches"] input`
      )
    )
    .filter(() => !onlyOnChanged)
    .merge(
      rxEvent({
        target: target.map(e =>
          e.selectAll(`[data-locator="other-branches"] input`)
        ),
        eventName: `change.${Math.random()}`
      })
    )
    .withLatestFrom(
      target.map(e =>
        e.selectAll<HTMLInputElement, any>(
          `[data-locator="other-branches"] input`
        )
      ),
      (_, inputs) => inputs
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
