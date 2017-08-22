import { difference, intersection } from "ramda";
import { Observable } from "rxjs";
import { Selection } from "d3-selection";

import { d3element, rxEvent } from "../utils/presentation/d3-binding";
import { IManageBranch } from "./data";
import { updateBranch } from "../api/basics";

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
        .map(e => e.select(`[data-locator="is-service-line"]`))
        .map(e => e.property(`checked`) as boolean),
      container
        .map(e => e.select(`[data-locator="conflict-mode"]`))
        .map(e => e.property(`value`) as string)
    )
      .map(
        (
          [
            upstream,
            downstream,
            recreateFromUpstream,
            isServiceLine,
            conflictResolutionMode
          ]
        ) => ({
          upstream,
          downstream,
          recreateFromUpstream,
          isServiceLine,
          conflictResolutionMode
        })
      )
      .take(1)
      // TODO - warn in this case, but we can't allow saving with
      // upstream and downstream having the same branch.
      .filter(
        ({ upstream, downstream }) => !intersection(upstream, downstream).length
      )
      .withLatestFrom(
        branchData.map(d => d.branches),
        (
          {
            upstream,
            downstream,
            recreateFromUpstream,
            isServiceLine,
            conflictResolutionMode
          },
          branches
        ) => {
          const oldUpstream = branches
            .filter(b => b.isUpstream)
            .map(b => b.branch);
          const oldDownstream = branches
            .filter(b => b.isDownstream)
            .map(b => b.branch);
          return {
            recreateFromUpstream,
            isServiceLine,
            conflictResolutionMode,
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
      .switchMap(requestBody => updateBranch(branchName, requestBody))
      .do(_ => onSaved())
      // TODO - success/error message
      .subscribe({
        error: _ => console.log(_)
      })
  );
};
