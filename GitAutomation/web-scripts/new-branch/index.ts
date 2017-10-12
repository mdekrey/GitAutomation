import { Observable, Subscription } from "../utils/rxjs";
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
    .do(elem => elem.html(require("./new-branch.layout.html")))
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
