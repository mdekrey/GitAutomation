import { difference, intersection } from "ramda";
import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { d3element, rxEvent } from "../utils/presentation/d3-binding";
import { IManageBranch } from "./data";
import { checkDownstreamMerges, updateBranch } from "../api/basics";

export const bindSaveButton = (
  branchName: string,
  selector: string,
  container: Observable<Selection<HTMLElement, any, any, any>>,
  branchData: Observable<IManageBranch>,
  onSaved: () => void
) => {
  /** Runs once and determines currently checked branches */
  const checkedBranches = (branchType: string) =>
    container
      .map(body =>
        body.selectAll(
          `[data-locator="${branchType}"] [data-locator="check"]:checked`
        )
      )
      .map(selection => selection.nodes())
      .map(checkboxes =>
        checkboxes.map(d3element).map(checkbox => checkbox.attr("data-branch"))
      );

  const getUpdateRequest = () =>
    Observable.combineLatest(
      checkedBranches("upstream-branches"),
      checkedBranches("downstream-branches"),
      container
        .map(e => e.select(`[data-locator="recreate-from-upstream"]`))
        .map(e => e.property(`checked`) as boolean),
      container
        .map(e => e.select(`[data-locator="branch-type"]`))
        .map(e => e.property(`value`) as string)
    )
      .map(([upstream, downstream, recreateFromUpstream, branchType]) => ({
        upstream,
        downstream,
        recreateFromUpstream,
        branchType
      }))
      .take(1)
      // TODO - warn in this case, but we can't allow saving with
      // upstream and downstream having the same branch.
      .filter(
        ({ upstream, downstream }) => !intersection(upstream, downstream).length
      )
      .withLatestFrom(
        branchData.map(d => d.branches),
        (
          { upstream, downstream, recreateFromUpstream, branchType },
          branches
        ) => {
          const oldUpstream = branches
            .filter(b => b.isUpstream)
            .map(b => b.groupName);
          const oldDownstream = branches
            .filter(b => b.isDownstream)
            .map(b => b.groupName);
          return {
            recreateFromUpstream,
            branchType,
            addUpstream: difference(upstream, oldUpstream),
            removeUpstream: difference(oldUpstream, upstream),
            addDownstream: difference(downstream, oldDownstream),
            removeDownstream: difference(oldDownstream, downstream)
          };
        }
      );

  // save
  return (
    rxEvent({
      target: container.map(container => container.selectAll(selector)),
      eventName: "click"
    })
      .switchMap(_ => getUpdateRequest())
      .switchMap(requestBody =>
        updateBranch(branchName, requestBody).map(_ => branchName)
      )
      .switchMap(branchName =>
        checkDownstreamMerges(branchName).map(_ => branchName)
      )
      .do(_ => onSaved())
      // TODO - success/error message
      .subscribe({
        error: _ => console.log(_)
      })
  );
};
