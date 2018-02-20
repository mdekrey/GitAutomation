import { IRxBindProps } from "../utils/presentation/d3-binding";
import { branchNameDisplay } from "../branch-name-display";
import { applyStyles } from "../style/style-binding";
import { RoutingNavigate } from "@woosti/rxjs-router";

export const buildBranchCheckListing = (
  styles: Record<string, string>,
  navigate: RoutingNavigate
): IRxBindProps<
  HTMLTableRowElement,
  Pick<GitAutomationGQL.IBranchGroupDetails, "groupName" | "branchType">,
  any,
  any
> => ({
  onCreate: target =>
    target.append<HTMLTableRowElement>("tr").attr("data-static-branch", ""),
  selector: "tr[data-static-branch]",
  onEnter: tr => {
    tr.html(require("./new-branch-check-listing.row.html"));
    applyStyles(styles)(tr);
  },
  onEach: selection => {
    branchNameDisplay(selection.select(`[data-locator="branch"]`), navigate);

    selection
      .select(`[data-locator="upstream-branches"] [data-locator="check"]`)
      .attr("data-direction", "upstream")
      .attr("data-branch", data => data.groupName);

    selection.sort((a, b) => a.groupName.localeCompare(b.groupName));
  }
});

export { checkedData } from "../manage-branch/branch-check-listing";
