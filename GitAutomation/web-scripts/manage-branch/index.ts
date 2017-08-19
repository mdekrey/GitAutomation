import { difference, intersection } from "ramda";
import { BehaviorSubject, Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  rxEvent,
  selectChildren,
  rxDatum,
  rxData,
  d3element
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { updateBranch } from "../api/basics";

export const manage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="home">Home</a>
  <h1 data-locator="branch-name"></h1>
  <h3>Downstream Branches</h3>
  <ul data-locator="downstream-branches"></ul>
  <h3>Upstream Branches</h3>
  <ul data-locator="upstream-branches"></ul>
  <button type="button" data-locator="reset">Reset</button>
  <button type="button" data-locator="home">Cancel</button>
  <button type="button" data-locator="save">Save</button>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();
        const reset = new BehaviorSubject<null>(null);
        const branchName = state.state.remainingPath!;

        // go home
        subscription.add(
          rxEvent({
            target: body.map(body => body.selectAll('[data-locator="home"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/", replaceCurentHistory: false })
          )
        );

        // reset
        subscription.add(
          rxEvent({
            target: body.map(body => body.selectAll('[data-locator="reset"]')),
            eventName: "click"
          })
            .map(() => null)
            .subscribe(reset)
        );

        const branchData = runBranchData(branchName!);
        subscription.add(branchData.subscription);

        const checkedBranches = (branchType: string) =>
          body
            .map(body =>
              body.selectAll(
                `[data-locator="${branchType}"] [data-locator="check"]:checked`
              )
            )
            .map(selection => selection.nodes())
            .map(checkboxes =>
              checkboxes
                .map(d3element)
                .map(checkbox => checkbox.attr("data-branch"))
            );

        // save
        subscription.add(
          rxEvent({
            target: body.map(body => body.selectAll('[data-locator="save"]')),
            eventName: "click"
          })
            .switchMap(_ =>
              Observable.combineLatest(
                checkedBranches("upstream-branches"),
                checkedBranches("downstream-branches")
              )
                .map(([upstream, downstream]) => ({
                  upstream,
                  downstream
                }))
                .take(1)
                // TODO - warn in this case, but we can't allow saving with
                // upstream and downstream being the same.
                .filter(
                  ({ upstream, downstream }) =>
                    !intersection(upstream, downstream).length
                )
                .withLatestFrom(
                  branchData.state.map(d => d.branches),
                  ({ upstream, downstream }, branches) => {
                    const oldUpstream = branches
                      .filter(b => b.isUpstream)
                      .map(b => b.branch);
                    const oldDownstream = branches
                      .filter(b => b.isDownstream)
                      .map(b => b.branch);
                    return {
                      addUpstream: difference(upstream, oldUpstream),
                      removeUpstream: difference(oldUpstream, upstream),
                      addDownstream: difference(downstream, oldDownstream),
                      removeDownstream: difference(oldDownstream, downstream)
                    };
                  }
                )
            )
            .switchMap(requestBody => updateBranch(branchName, requestBody))
            // TODO - success/error message
            .subscribe()
        );

        // display branch name
        subscription.add(
          rxDatum(
            body.let(selectChildren(`[data-locator="branch-name"]`)),
            Observable.of(branchName)
          ).subscribe(target => target.text(data => data))
        );

        // display downstream branches
        subscription.add(
          rxData(
            body.let(selectChildren(`[data-locator="downstream-branches"]`)),
            branchData.state
              .map(state => state.branches)
              .combineLatest(reset, _ => _),
            data => data.branch
          )
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              selector: "li",
              onEnter: li => {
                li.html(`
                  <label>
                    <input type="checkbox" data-locator="check"/>
                    <span data-locator="branch"></span>
                  </label>
                `);
              },
              onEach: selection => {
                selection
                  .select(`[data-locator="branch"]`)
                  .text(data => data.branch);
                selection
                  .select(`[data-locator="check"]`)
                  .attr("data-branch", data => data.branch)
                  .property("checked", data => data.isDownstream)
                  .property("disabled", data => data.isUpstream);
              }
            })
            .subscribe()
        );

        // display upstream branches
        subscription.add(
          rxData(
            body.let(selectChildren(`[data-locator="upstream-branches"]`)),
            branchData.state
              .map(state => state.branches)
              .combineLatest(reset, _ => _),
            data => data.branch
          )
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              selector: "li",
              onEnter: li => {
                li.html(`
                  <label>
                    <input type="checkbox" data-locator="check"/>
                    <span data-locator="branch"></span>
                  </label>
                `);
              },
              onEach: selection => {
                selection
                  .select(`[data-locator="branch"]`)
                  .text(data => data.branch);
                selection
                  .select(`[data-locator="check"]`)
                  .attr("data-branch", data => data.branch)
                  .property("checked", data => data.isUpstream)
                  .property("disabled", data => data.isDownstream);
              }
            })
            .subscribe()
        );

        return subscription;
      })
    );
