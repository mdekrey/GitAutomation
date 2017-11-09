import { IRxBindProps } from "../utils/presentation/d3-binding";
import { IBranchData } from "./data";
import { branchNameDisplay } from "../branch-name-display";

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

export const buildBranchCheckListing = (): IRxBindProps<
  HTMLTableRowElement,
  IBranchData,
  any,
  any
> => ({
  onCreate: target =>
    target.append<HTMLTableRowElement>("tr").attr("data-static-branch", ""),
  selector: "tr[data-static-branch]",
  onEnter: tr => {
    tr.html(require("./branch-check-listing.row.html"));
  },
  onEach: selection => {
    branchNameDisplay(selection.select(`[data-locator="branch"]`));
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
  }
});
