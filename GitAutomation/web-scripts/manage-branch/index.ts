import { Observable, Subscription, Subject } from "rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  rxEvent,
  fnSelect,
  rxDatum,
  rxData
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { buildBranchCheckListing } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";
import { promoteServiceLine, deleteBranch } from "../api/basics";

export const manage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="home">Home</a>
  <h1 data-locator="branch-name"></h1>
  <h3>Settings</h3>
  <label>
      <input type="checkbox" data-locator="recreate-from-upstream" />
      Recreate from Upstream
  </label>
  <label>
      <input type="checkbox" data-locator="is-service-line" />
      Is Service Line?
  </label>
  <label>
      Conflict Mode
      <select data-locator="conflict-mode">
        <option value="PullRequest">Pull Request</option>
        <option value="IntegrationBranch">Integration Branch</option>
      </select>
  </label>
  <h3>Downstream Branches</h3>
  <ul data-locator="downstream-branches"></ul>
  <h3>Upstream Branches</h3>
  <ul data-locator="upstream-branches"></ul>
  <button type="button" data-locator="reset">Reset</button>
  <button type="button" data-locator="home">Cancel</button>
  <button type="button" data-locator="save">Save</button>

  <h3>Release to Service Line</h3>
  <label>
    <span>Service Line Branch</span>
    <input type="text" data-locator="service-line-branch" />
  </label>
  <label>
    <span>Release Tag</span>
    <input type="text" data-locator="release-tag" />
  </label>
  <button type="button" data-locator="promote-service-line">Release to Service Line</button>

    <h3>Delete Branch</h3>
    <p>This action cannot be undone.</p>
    <button type="button" data-locator="delete-branch">Delete</button>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();
        const branchName = state.state.remainingPath!;
        const reload = new Subject<null>();

        // go home
        subscription.add(
          rxEvent({
            target: container.map(body =>
              body.selectAll('[data-locator="home"]')
            ),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/", replaceCurentHistory: false })
          )
        );

        // reset
        const reset = rxEvent({
          target: container.map(body =>
            body.selectAll('[data-locator="reset"]')
          ),
          eventName: "click"
        })
          .map(() => null)
          .publish()
          .refCount()
          .startWith(null);

        const branchData = runBranchData(branchName!, reload);
        subscription.add(branchData.subscription);

        subscription.add(
          bindSaveButton(
            branchName,
            '[data-locator="save"]',
            container,
            branchData.state,
            () => reload.next(null)
          )
        );

        // display branch name
        subscription.add(
          container
            .map(fnSelect(`[data-locator="branch-name"]`))
            .let(rxDatum(Observable.of(branchName)))
            .subscribe(target => target.text(data => data))
        );

        const branchList = branchData.state
          .map(state =>
            state.branches.filter(({ branch }) => branch !== branchName)
          )
          .combineLatest(reset, _ => _);

        subscription.add(
          container
            .map(e => e.select(`[data-locator="recreate-from-upstream"]`))
            .switchMap(e =>
              branchData.state
                .map(d => d.recreateFromUpstream)
                .map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("checked", value => value);
            })
        );

        subscription.add(
          container
            .map(e => e.select(`[data-locator="is-service-line"]`))
            .switchMap(e =>
              branchData.state.map(d => d.isServiceLine).map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("checked", value => value);
            })
        );

        subscription.add(
          container
            .map(e => e.select(`[data-locator="conflict-mode"]`))
            .switchMap(e =>
              branchData.state
                .map(d => d.conflictResolutionMode)
                .map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("value", value => value);
            })
        );

        // display downstream branches
        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="downstream-branches"]`)),
            branchList,
            data => data.branch
          )
            .bind<HTMLLIElement>(
              buildBranchCheckListing(
                b => b.isDownstream,
                b => !b.isDownstreamAllowed && !b.isDownstream
              )
            )
            .subscribe()
        );

        // display upstream branches
        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="upstream-branches"]`)),
            branchList,
            data => data.branch
          )
            .bind<HTMLLIElement>(
              buildBranchCheckListing(
                b => b.isUpstream,
                b => !b.isUpstreamAllowed && !b.isUpstream
              )
            )
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: container.map(
              fnSelect(`[data-locator="promote-service-line"]`)
            ),
            eventName: "click"
          })
            .switchMap(_ =>
              Observable.combineLatest(
                container
                  .map(fnSelect(`[data-locator="service-line-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="release-tag"]`))
                  .map(sl => sl.property("value") as string)
              ).map(([serviceLine, tagName]) => ({
                releaseCandidate: branchName,
                serviceLine,
                tagName
              }))
            )
            .take(1)
            .switchMap(promoteServiceLine)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="delete-branch"]`)),
            eventName: "click"
          })
            .map(() => branchName)
            .take(1)
            .switchMap(deleteBranch)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        return subscription;
      })
    );
