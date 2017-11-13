import { Observable, Subscription, Subject } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  rxEvent,
  fnSelect,
  rxDatum,
  rxData,
  d3element
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { buildBranchCheckListing, checkedData } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";
import {
  checkPullRequests,
  consolidateMerged,
  promoteServiceLine,
  deleteBranch,
  deleteBranchByMode,
  detectUpstream,
  detectAllUpstream,
  allBranchGroups,
  forceRefreshBranchGroups
} from "../api/basics";
import { branchHierarchy } from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";
import { classed } from "../style/style-binding";
import { style } from "typestyle";

const manageStyle = {
  fieldSection: style({
    marginTop: "0.5em"
  }),
  hint: style({
    margin: 0,
    padding: 0,
    fontSize: "0.75em"
  }),
  rotateHeader: style({
    height: "100px",
    whiteSpace: "nowrap",
    width: "25px",
    verticalAlign: "bottom",
    padding: "0",
    $nest: {
      "> div": {
        transformOrigin: "bottom left",
        transform: "translate(26px, 0px) rotate(-60deg)",
        width: "25px",
        $nest: {
          "> span": {
            borderBottom: "1px solid #ccc",
            padding: "0"
          }
        }
      }
    }
  }),
  otherBranchTable: style({
    borderCollapse: "collapse"
  }),
  checkboxCell: style({
    borderRight: "1px solid #ccc",
    textAlign: "center"
  }),
  branchName: style({
    textAlign: "right",
    fontWeight: "bold"
  })
};

export const manage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./manage-branch.layout.html")))
    .let(classed(manageStyle))
    .publishReplay(1)
    .refCount()
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();
        const branchName = state.state.remainingPath!;
        const reload = new Subject<null>();
        let dataConnection: Subscription | null = null;

        subscription.add(
          reload.subscribe(() => forceRefreshBranchGroups.next(null))
        );

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

        const branchDataState = branchData.state.publishReplay(1);
        subscription.add(
          reload
            .merge(reset)
            .startWith(null)
            .subscribe(() => {
              if (dataConnection === null) {
                subscription.add((dataConnection = branchDataState.connect()));
              }
            })
        );

        subscription.add(
          bindSaveButton(
            branchName,
            '[data-locator="save"]',
            container,
            branchDataState,
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

        const branchList = branchDataState
          .map(state =>
            state.branches.filter(({ groupName }) => groupName !== branchName)
          )
          .combineLatest(reset, _ => _);

        subscription.add(
          container
            .map(e => e.select(`[data-locator="recreate-from-upstream"]`))
            .switchMap(e =>
              branchDataState
                .map(d => d.recreateFromUpstream)
                .map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("checked", value => value);
            })
        );

        subscription.add(
          container
            .map(e => e.select(`[data-locator="branch-type"]`))
            .switchMap(e =>
              branchDataState.map(d => d.branchType).map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("value", value => value);
            })
        );

        subscription.add(
          branchHierarchy({
            target: container.map(
              fnSelect<SVGSVGElement>(`svg[data-locator="hierarchy-container"]`)
            ),
            navigate: state.navigate,
            data: groupsToHierarchy(
              allBranchGroups,
              group =>
                group.groupName === branchName ||
                Boolean(group.upstream.find(v => v === branchName)) ||
                Boolean(group.downstream.find(v => v === branchName))
            )
          }).subscribe()
        );

        const checkboxes = rxData(
          container.map(fnSelect(`[data-locator="other-branches"]`)),
          branchList,
          data => data.groupName
        )
          .bind(buildBranchCheckListing(manageStyle))
          .publishReplay(1)
          .refCount();

        // display downstream branches
        subscription.add(checkboxes.subscribe());

        subscription.add(
          branchHierarchy({
            target: container.map(
              fnSelect<SVGSVGElement>(
                `svg[data-locator="hierarchy-container-preview"]`
              )
            ),
            navigate: state.navigate,
            data: groupsToHierarchy(
              checkedData(checkboxes).combineLatest(
                allBranchGroups,
                (newStatus, groups) =>
                  groups.map(
                    group =>
                      group.groupName === branchName
                        ? {
                            ...group,
                            directDownstream: newStatus.downstream.map(
                              groupName => ({ groupName })
                            )
                          }
                        : {
                            ...group,
                            directDownstream: group.directDownstream
                              .filter(g => g.groupName !== branchName)
                              .concat(
                                newStatus.upstream.find(
                                  up => up === group.groupName
                                )
                                  ? [{ groupName: branchName }]
                                  : []
                              )
                          }
                  )
              ),
              group =>
                group.groupName === branchName ||
                Boolean(group.upstream.find(v => v === branchName)) ||
                Boolean(group.downstream.find(v => v === branchName))
            )
          }).subscribe()
        );

        const actualBranchDisplay = rxData(
          container.map(fnSelect(`[data-locator="grouped-branches"]`)),
          branchDataState.map(branch => branch.actualBranches)
        ).bind({
          selector: "li",
          onCreate: selection => selection.append<HTMLLIElement>("li"),
          onEnter: selection =>
            selection.html(require("./manage-branch.branch-row.html")),
          onEach: selection =>
            selection
              .select("span")
              .text(data => `${data.name} (${data.commit.substr(0, 7)})`)
        });

        subscription.add(
          rxEvent({
            target: actualBranchDisplay.map(
              fnSelect(`a[data-locator="delete"]`)
            ),
            eventName: "click"
          })
            .switchMap(event =>
              deleteBranchByMode(event.datum.name, "ActualBranchOnly")
            )
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: actualBranchDisplay.map(
              fnSelect(`a[data-locator="check-up-to-date"]`)
            ),
            eventName: "click"
          })
            .switchMap(event =>
              rxData(
                container
                  .map(fnSelect(`[data-locator="up-to-date"]`))
                  .do(elem =>
                    elem
                      .html(require("./manage-branch.up-to-date-report.html"))
                      .select("span")
                      .text(event.datum.name)
                  )
                  .map(fnSelect("ul")),
                detectAllUpstream(event.datum.name).take(1)
              ).bind({
                selector: "li",
                onCreate: selection => selection.append<HTMLLIElement>("li"),
                onEach: selection => selection.text(data => data)
              })
            )
            .subscribe()
        );

        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="approved-branch"]`)),
            branchDataState.map(branch => branch.actualBranches)
          )
            .bind({
              selector: "option",
              onCreate: selection =>
                selection.append<HTMLOptionElement>("option"),
              onEach: selection =>
                selection
                  .text(data => `${data.name} (${data.commit.substr(0, 7)})`)
                  .attr("value", data => data.name)
            })
            .subscribe()
        );

        subscription.add(
          rxDatum(branchDataState.map(branch => branch.latestBranchName))(
            container.map(fnSelect(`[data-locator="approved-branch"]`))
          ).subscribe(selection => selection.property("value", d => d))
        );

        subscription.add(
          rxData(
            container.map(
              fnSelect(`[data-locator="consolidate-target-branch"]`)
            ),
            branchList.map(branches =>
              branches.filter(branch => !branch.isUpstream)
            ),
            data => data.groupName
          )
            .bind({
              selector: "option",
              onCreate: selection =>
                selection.append<HTMLOptionElement>("option"),
              onEach: selection =>
                selection
                  .text(data => data.groupName)
                  .attr("value", data => data.groupName)
            })
            .subscribe()
        );
        subscription.add(
          rxData(
            container.map(
              fnSelect(`[data-locator="consolidate-original-branches"]`)
            ),
            branchList.map(branches =>
              [branchName].concat(
                branches
                  .filter(branch => branch.isSomewhereUpstream)
                  .map(branch => branch.groupName)
              )
            ),
            data => data
          )
            .bind({
              selector: "li",
              onCreate: selection => selection.append<HTMLLIElement>("li"),
              onEnter: selection =>
                selection.html(
                  require("./manage-branch.consolidate-entry.html")
                ),
              onEach: selection => {
                selection.select("span").text(b => b);
                selection.select("input").attr("data-branch", b => b);
              }
            })
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="detect-upstream"]`)),
            eventName: "click"
          })
            .withLatestFrom(
              container.map(fnSelect(`[data-locator="other-branches"]`)),
              (_, elem) => elem
            )
            .subscribe(elements => {
              detectUpstream(branchName, true).subscribe(branchNames =>
                branchNames.forEach(upstreamBranchName =>
                  elements
                    .select(
                      `[data-locator="upstream-branches"] [data-locator="check"][data-branch="${upstreamBranchName}"]`
                    )
                    .property("checked", true)
                )
              );
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="check-prs"]`)),
            eventName: "click"
          })
            .withLatestFrom(
              container.map(fnSelect(`[data-locator="other-branches"]`)),
              (_, elem) => elem
            )
            .subscribe(elements => {
              checkPullRequests(branchName).subscribe(pullRequests =>
                pullRequests.forEach(pr =>
                  elements
                    .select(
                      `[data-locator="upstream-branches"] [data-locator="pr-status"][data-branch="${pr.sourceBranch}"]`
                    )
                    .text(
                      `Has PR: ${pr.state} (${pr.reviews!
                        .map(review => `${review.username}: ${review.state}`)
                        .join(", ")})`
                    )
                )
              );
            })
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
                  .map(fnSelect(`[data-locator="approved-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="service-line-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="release-tag"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="auto-consolidate"]`))
                  .map(sl => sl.property("checked") as boolean)
              ).map(
                (
                  [releaseCandidate, serviceLine, tagName, autoConsolidate]
                ) => ({
                  releaseCandidate,
                  serviceLine,
                  tagName,
                  autoConsolidate
                })
              )
            )
            .take(1)
            .switchMap(promoteServiceLine)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(
              fnSelect(`[data-locator="consolidate-branch"]`)
            ),
            eventName: "click"
          })
            .switchMap(_ =>
              Observable.combineLatest(
                container
                  .map(fnSelect(`[data-locator="consolidate-target-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(elem =>
                    elem.selectAll<HTMLInputElement, any>(
                      `[data-locator="consolidate-original-branches"] [data-locator="consolidate-original-branch"]:checked`
                    )
                  )
                  .map(sl => sl.nodes())
                  .map(checkboxes =>
                    checkboxes
                      .map(d3element)
                      .map(checkbox => checkbox.attr("data-branch"))
                  )
              ).map(([targetBranch, originalBranches]) => ({
                targetBranch,
                originalBranches
              }))
            )
            .take(1)
            .switchMap(consolidateMerged)
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

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="delete-group"]`)),
            eventName: "click"
          })
            .map(() => branchName)
            .take(1)
            .switchMap(branchName =>
              deleteBranchByMode(branchName, "GroupOnly")
            )
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        return subscription;
      })
    );
