import { IRxBindProps } from "../utils/presentation/d3-binding";
import { BranchGroup } from "../api/basic-branch";
import { branchNameDisplay } from "../branch-name-display";

export const buildBranchCheckListing = (): IRxBindProps<
  HTMLLIElement,
  BranchGroup,
  any,
  any
> => ({
  onCreate: target =>
    target.append<HTMLLIElement>("li").attr("data-static-branch", ""),
  selector: "li[data-static-branch]",
  onEnter: li => {
    li.html(require("./new-branch-check-listing.row.html"));
  },
  onEach: selection => {
    branchNameDisplay(selection.select(`[data-locator="branch"]`));
    selection
      .select(`[data-locator="check"]`)
      .attr("data-branch", data => data.groupName);
  }
});
