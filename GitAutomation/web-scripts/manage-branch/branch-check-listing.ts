import { IRxBindProps } from "../utils/presentation/d3-binding";
import { IBranchData } from "./data";

export type BranchPredicate = (data: IBranchData) => boolean;

export const buildBranchCheckListing = (
  checked: BranchPredicate,
  disabled: BranchPredicate
): IRxBindProps<HTMLLIElement, IBranchData, any, any> => ({
  onCreate: target =>
    target.append<HTMLLIElement>("li").attr("data-static-branch", ""),
  selector: "li[data-static-branch]",
  onEnter: li => {
    li.html(`
      <label>
        <input type="checkbox" data-locator="check"/>
        <span data-locator="branch"></span>
      </label>
    `);
  },
  onEach: selection => {
    selection.select(`[data-locator="branch"]`).text(data => data.branch);
    selection
      .select(`[data-locator="check"]`)
      .attr("data-branch", data => data.branch)
      .property("checked", checked)
      .property("disabled", disabled);
  }
});
