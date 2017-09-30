import { Observable } from "rxjs";
import { Selection } from "d3-selection";

import { d3element, rxEvent } from "../utils/presentation/d3-binding";
import { checkDownstreamMerges, updateBranch } from "../api/basics";

export const bindSaveButton = (
  selector: string,
  container: Observable<Selection<HTMLElement, any, any, any>>,
  onSaved: (branchName: string) => void
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
      container
        .map(e => e.select(`[data-locator="branch-name"]`))
        .map(e => e.property(`value`) as string),
      checkedBranches("upstream-branches"),
      container
        .map(e => e.select(`[data-locator="recreate-from-upstream"]`))
        .map(e => e.property(`checked`) as boolean),
      container
        .map(e => e.select(`[data-locator="branch-type"]`))
        .map(e => e.property(`value`) as string)
    )
      .map(([branchName, upstream, recreateFromUpstream, branchType]) => ({
        branchName,
        upstream,
        recreateFromUpstream,
        branchType
      }))
      .take(1)
      .map(({ branchName, upstream, recreateFromUpstream, branchType }) => {
        return {
          branchName,
          requestBody: {
            recreateFromUpstream,
            branchType,
            addUpstream: upstream,
            removeUpstream: [],
            addDownstream: [],
            removeDownstream: []
          }
        };
      });

  // save
  return (
    rxEvent({
      target: container.map(container => container.selectAll(selector)),
      eventName: "click"
    })
      .switchMap(_ => getUpdateRequest())
      .switchMap(({ branchName, requestBody }) =>
        updateBranch(branchName, requestBody).map(_ => branchName)
      )
      .switchMap(branchName =>
        checkDownstreamMerges(branchName).map(_ => branchName)
      )
      .do(branchName => onSaved(branchName))
      // TODO - success/error message
      .subscribe({
        error: _ => console.log(_)
      })
  );
};
