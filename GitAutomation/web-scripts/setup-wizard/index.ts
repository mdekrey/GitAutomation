import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import { fnEvent, fnSelect, rxData } from "../utils/presentation/d3-binding";
import { recommendGroups, updateBranch, detectUpstream } from "../api/basics";
import { handleErrorOnce } from "../handle-error";

export const setupWizard = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./setup-wizard.layout.html")))
    .publishReplay(1)
    .refCount()
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();

        // go home
        subscription.add(
          container
            .map(body => body.selectAll('[data-locator="home"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({ url: "/", replaceCurentHistory: false })
            )
        );

        const groupsList = container.map(
          fnSelect(`[data-locator="groups-list"]`)
        );
        subscription.add(
          rxData(groupsList, recommendGroups())
            .bind({
              selector: "li",
              onCreate: target => target.append<HTMLLIElement>("li"),
              onEnter: target =>
                target.html(require("./setup-wizard.group-row.html")),
              onEach: target => {
                target.select(`[data-locator="group-name"]`).text(data => data);
                target.select(`input`).attr("data-group", data => data);
              }
            })
            .subscribe()
        );

        subscription.add(
          container
            .map(fnSelect('[data-locator="save"]'))
            .let(fnEvent("click"))
            .do(() => {
              container
                .map(fnSelect('[data-locator="save"]'))
                .take(1)
                .subscribe(target => target.property("disabled", true));
              groupsList
                .map(groups => groups.selectAll("input"))
                .take(1)
                .subscribe(target => target.property("disabled", true));
            })
            .map(() =>
              groupsList.map(c =>
                c
                  .selectAll<HTMLInputElement, any>("input")
                  .nodes()
                  .filter(input => input.checked)
                  .map(input => input.getAttribute("data-group")!)
                  .filter(Boolean)
              )
            )
            .switch()
            .map(groups =>
              Observable.concat(
                ...groups.map(group =>
                  updateBranch(group, {
                    recreateFromUpstream: false,
                    branchType: "Feature",
                    addUpstream: [],
                    addDownstream: [],
                    removeDownstream: [],
                    removeUpstream: []
                  })
                )
              )
                .toArray()
                .map(() => groups)
            )
            .switch()
            .map(groups =>
              Observable.concat(
                ...groups.map(group =>
                  detectUpstream(group, true).map(upstream => ({
                    group,
                    settings: {
                      recreateFromUpstream: false,
                      branchType: "Feature",
                      addUpstream: upstream,
                      addDownstream: [],
                      removeDownstream: [],
                      removeUpstream: []
                    }
                  }))
                )
              ).toArray()
            )
            .switch()
            .map(groups =>
              Observable.concat(
                ...groups.map(({ group, settings }) =>
                  updateBranch(group, settings)
                )
              )
                .toArray()
                .map(() => groups)
            )
            .switch()
            .subscribe(groups => {
              state.navigate({
                url: "/",
                replaceCurentHistory: false
              });
            }, handleErrorOnce)
        );

        return subscription;
      })
    );
