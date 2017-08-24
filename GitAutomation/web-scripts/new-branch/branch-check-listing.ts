import { IRxBindProps } from "../utils/presentation/d3-binding";

export const buildBranchCheckListing = (): IRxBindProps<
  HTMLLIElement,
  string,
  any,
  any
> => ({
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
    selection.select(`[data-locator="branch"]`).text(data => data);
    selection
      .select(`[data-locator="check"]`)
      .attr("data-branch", data => data);
  }
});