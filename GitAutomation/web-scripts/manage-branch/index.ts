import { Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  rxEvent,
  selectChildren,
  rxDatum,
  rxData
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { buildBranchCheckListing } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";

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
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();
        const branchName = state.state.remainingPath!;

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

        const branchData = runBranchData(branchName!);
        subscription.add(branchData.subscription);

        subscription.add(
          bindSaveButton(
            branchName,
            '[data-locator="save"]',
            container,
            branchData.state
          )
        );

        // display branch name
        subscription.add(
          rxDatum(
            container.let(selectChildren(`[data-locator="branch-name"]`)),
            Observable.of(branchName)
          ).subscribe(target => target.text(data => data))
        );

        const branchList = branchData.state
          .map(state =>
            state.branches.filter(({ branch }) => branch !== branchName)
          )
          .combineLatest(reset, _ => _);

        // display downstream branches
        subscription.add(
          rxData(
            container.let(
              selectChildren(`[data-locator="downstream-branches"]`)
            ),
            branchList,
            data => data.branch
          )
            .bind<HTMLLIElement>(
              buildBranchCheckListing(b => b.isDownstream, b => b.isUpstream)
            )
            .subscribe()
        );

        // display upstream branches
        subscription.add(
          rxData(
            container.let(selectChildren(`[data-locator="upstream-branches"]`)),
            branchList,
            data => data.branch
          )
            .bind<HTMLLIElement>(
              buildBranchCheckListing(b => b.isUpstream, b => b.isDownstream)
            )
            .subscribe()
        );

        return subscription;
      })
    );
