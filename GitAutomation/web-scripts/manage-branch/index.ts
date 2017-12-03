import { Observable, Subscription, Subject } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  fnEvent,
  fnSelect,
  rxDatum,
  rxData,
  d3element
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { buildBranchCheckListing, checkedData } from "./branch-check-listing";
import { doSave } from "./bind-save-button";
import {
  consolidateMerged,
  promoteServiceLine,
  deleteBranch,
  deleteBranchByMode,
  detectUpstream,
  detectAllUpstream,
  allBranchGroups,
  forceRefreshBranchGroups
} from "../api/basics";
import {
  branchHierarchy,
  highlightedHierarchyStyle,
  defaultHierarchyStyles
} from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";
import { classed } from "../style/style-binding";
import { style } from "typestyle";
import { secured } from "../security/security-binding";
import { inputValue } from "../utils/inputs";
import { handleError, handleErrorOnce } from "../handle-error";
import { branchNameDisplay } from "../branch-name-display";
import { merge } from "../utils/ramda";

const manageStyle = {
  fieldSection: style({
    marginTop: "0.5em"
  }),
  hint: style({
    margin: 0,
    padding: 0,
    fontSize: "0.75rem"
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
    .let(secured)
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();
        const branchName = state.state.remainingPath!;
        const reload = new Subject<null>();
        let dataConnection: Subscription | null = null;
        const disconnect = () => {
          if (dataConnection) {
            dataConnection.unsubscribe();
            dataConnection = null;
          }
        };

        subscription.add(
          reload.subscribe(() => forceRefreshBranchGroups.next(null))
        );

        // go home
        subscription.add(
          container
            .map(body => body.selectAll('[data-locator="home"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({ url: "/", replaceCurentHistory: false })
            )
        );

        // reset
        const reset = container
          .map(body => body.selectAll('[data-locator="reset"]'))
          .let(fnEvent("click"))
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

        // display branch name
        subscription.add(
          container
            .map(fnSelect(`[data-locator="branch-name"]`))
            .let(
              rxDatum(
                branchDataState
                  .map(d => merge(d, { groupName: branchName }))
                  .do(v => console.log(v))
              )
            )
            .subscribe(branchNameDisplay)
        );

        const branchList = branchDataState
          .map(state =>
            state.otherBranches.filter(
              ({ groupName }) => groupName !== branchName
            )
          )
          .combineLatest(reset, _ => _);

        const upstreamMergePolicyData = container
          .map(fnSelect(`[data-locator="upstream-merge-policy"]`))
          .let(rxDatum(branchDataState.map(d => d.upstreamMergePolicy)))
          .do(target => {
            target.property("value", value => value);
          })
          .publishReplay(1)
          .refCount()
          .let(inputValue({ includeInitial: true }))
          .map(v => v as GitAutomationGQL.IUpstreamMergePolicyEnum);

        subscription.add(upstreamMergePolicyData.subscribe());

        const branchTypeData = container
          .map(fnSelect(`[data-locator="branch-type"]`))
          .let(rxDatum(branchDataState.map(d => d.branchType)))
          .do(target => {
            target.property("value", value => value);
          })
          .publishReplay(1)
          .refCount()
          .let(inputValue({ includeInitial: true }))
          .map(v => v as GitAutomationGQL.IBranchGroupTypeEnum);

        subscription.add(branchTypeData.subscribe());

        subscription.add(
          container
            .map(fnSelect(`[data-locator="branch-type"]`))
            .let(inputValue({ includeInitial: false }))
            .subscribe(disconnect)
        );

        subscription.add(
          container
            .map(fnSelect(`[data-locator="upstream-merge-policy"]`))
            .let(inputValue({ includeInitial: false }))
            .subscribe(disconnect)
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
            ),
            style: [
              {
                ...highlightedHierarchyStyle("white"),
                filter: data => data.groupName === branchName
              },
              ...defaultHierarchyStyles
            ]
          }).subscribe()
        );

        const checkboxes = rxData(
          container.map(fnSelect(`[data-locator="other-branches"]`)),
          branchList,
          data => data.groupName
        )
          .bind(buildBranchCheckListing(manageStyle, state.navigate))
          .publishReplay(1)
          .refCount();

        // display downstream branches
        subscription.add(checkboxes.subscribe());

        subscription.add(checkedData(checkboxes, true).subscribe(disconnect));

        const data = checkedData(checkboxes).combineLatest(
          branchTypeData,
          container
            .map(fnSelect(`[data-locator="upstream-merge-policy"]`))
            .let(inputValue({ includeInitial: true }))
            .map(v => v as GitAutomationGQL.IUpstreamMergePolicyEnum),
          (stream, branchType, upstreamMergePolicy) => ({
            ...stream,
            branchType,
            branchName,
            upstreamMergePolicy
          })
        );

        subscription.add(
          container
            .map(fnSelect('[data-locator="save"]'))
            .let(fnEvent("click"))
            .switchMap(() => doSave(data, branchDataState))
            .let(handleError)
            .subscribe({
              next: () => reload.next(null),
              error: err => console.error(err)
            })
        );

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
                branchTypeData,
                allBranchGroups,
                (newStatus, branchType, groups) =>
                  groups.map(
                    group =>
                      group.groupName === branchName
                        ? {
                            ...group,
                            branchType,
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
            ),
            style: [
              {
                ...highlightedHierarchyStyle("white"),
                filter: data => data.groupName === branchName
              },
              ...defaultHierarchyStyles
            ]
          }).subscribe()
        );

        const actualBranchDisplay = rxData(
          container.map(fnSelect(`[data-locator="grouped-branches"]`)),
          branchDataState.map(branch => branch.branches)
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
          actualBranchDisplay
            .map(fnSelect(`a[data-locator="delete"]`))
            .let(fnEvent("click"))
            .switchMap(event =>
              deleteBranchByMode(event.datum.name, "ActualBranchOnly")
            )
            .let(handleError)
            .subscribe()
        );

        subscription.add(
          actualBranchDisplay
            .map(fnSelect(`a[data-locator="check-up-to-date"]`))
            .let(fnEvent("click"))
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
            .let(handleError)
            .subscribe()
        );

        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="approved-branch"]`)),
            branchDataState.map(branch => branch.branches)
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
          rxDatum(
            branchDataState.map(
              branch => branch.latestBranch && branch.latestBranch.name
            )
          )(
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
          container
            .map(fnSelect(`[data-locator="detect-upstream"]`))
            .let(fnEvent("click"))
            .withLatestFrom(
              container.map(fnSelect(`[data-locator="other-branches"]`)),
              (_, elem) => elem
            )
            .subscribe(elements => {
              detectUpstream(branchName, true).subscribe(
                branchNames =>
                  branchNames.forEach(upstreamBranchName =>
                    elements
                      .select(
                        `[data-locator="upstream-branches"] [data-locator="check"][data-branch="${
                          upstreamBranchName
                        }"]`
                      )
                      .property("checked", true)
                      .each(function(this: Element) {
                        const evt = document.createEvent("HTMLEvents");
                        evt.initEvent("change", false, true);
                        this.dispatchEvent(evt);
                      })
                  ),
                handleErrorOnce
              );
            })
        );

        subscription.add(
          container
            .map(fnSelect(`[data-locator="promote-service-line"]`))
            .let(fnEvent("click"))
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
            .let(handleError)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          container
            .map(fnSelect(`[data-locator="consolidate-branch"]`))
            .let(fnEvent("click"))
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
            .let(handleError)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          container
            .map(fnSelect(`[data-locator="delete-branch"]`))
            .let(fnEvent("click"))
            .map(() => branchName)
            .take(1)
            .switchMap(deleteBranch)
            .let(handleError)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          container
            .map(fnSelect(`[data-locator="delete-group"]`))
            .let(fnEvent("click"))
            .map(() => branchName)
            .take(1)
            .switchMap(branchName =>
              deleteBranchByMode(branchName, "GroupOnly")
            )
            .let(handleError)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        return subscription;
      })
    );
