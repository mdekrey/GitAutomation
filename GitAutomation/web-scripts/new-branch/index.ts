import { Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import { rxEvent, fnSelect, rxData } from "../utils/presentation/d3-binding";
import { allBranches } from "../api/basics";
import { buildBranchCheckListing } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";

export const newBranch = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="home">Home</a>
  <h1>New Branch</h1>
  <h3>Settings</h3>
  <label>
      Branch Name
      <input type="text" data-locator="branch-name" />
  </label>
  <label>
      <input type="checkbox" data-locator="recreate-from-upstream" />
      Recreate from Upstream
  </label>
  <label>
      <input type="checkbox" data-locator="is-service-line" />
      Is Service Line?
  </label>
  <h3>Upstream Branches</h3>
  <ul data-locator="upstream-branches"></ul>
  <button type="button" data-locator="home">Cancel</button>
  <button type="button" data-locator="save">Save</button>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();

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

        // display upstream branches
        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="upstream-branches"]`)),
            allBranches()
          )
            .bind<HTMLLIElement>(buildBranchCheckListing())
            .subscribe()
        );

        subscription.add(
          bindSaveButton('[data-locator="save"]', container, branchName => {
            state.navigate({
              url: "/manage/" + branchName,
              replaceCurentHistory: false
            });
          })
        );

        return subscription;
      })
    );
