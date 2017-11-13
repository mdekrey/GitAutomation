import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import { rxEvent, fnSelect, rxData } from "../utils/presentation/d3-binding";
import { allBranchGroups } from "../api/basics";
import { buildBranchCheckListing, checkedData } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";
import { style } from "typestyle";
import { classed } from "../style/style-binding";
import { branchHierarchy } from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";

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

export const newBranch = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./new-branch.layout.html")))
    .let(classed(manageStyle))
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
        const checkboxes = rxData(
          container.map(fnSelect(`[data-locator="other-branches"]`)),
          allBranchGroups
            .take(1)
            .map(b => b as Partial<GitAutomationGQL.IBranchGroupDetails>[])
        )
          .bind<HTMLTableRowElement>(
            buildBranchCheckListing(manageStyle, state.navigate)
          )
          .publishReplay(1)
          .refCount();
        subscription.add(checkboxes.subscribe());

        // display preview
        const data = Observable.combineLatest(
          rxEvent({
            target: container.map(e =>
              e.select<HTMLInputElement>(`[data-locator="branch-name"]`)
            ),
            eventName: "change"
          })
            .map(e => e.target.value)
            .startWith("")
            .map(e => e || "New Branch"),
          rxEvent({
            target: container.map(e =>
              e.select<HTMLInputElement>(
                `[data-locator="recreate-from-upstream"]`
              )
            ),
            eventName: "change"
          })
            .map(e => e.target.checked)
            .startWith(false),
          rxEvent({
            target: container.map(e =>
              e.select<HTMLInputElement>(`[data-locator="branch-type"]`)
            ),
            eventName: "change"
          })
            .map(e => e.target.value)
            .startWith("Feature")
        ).map(([branchName, recreateFromUpstream, branchType]) => ({
          branchName,
          recreateFromUpstream,
          branchType
        }));

        const hierarchy$ = checkedData(checkboxes)
          .combineLatest(
            allBranchGroups,
            data,
            (newStatus, groups, { branchName, branchType }) => ({
              groups: groups
                .map(
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
                .concat(
                  groups.find(group => group.groupName === branchName)
                    ? []
                    : [
                        {
                          groupName: branchName,
                          branchType: branchType as GitAutomationGQL.IBranchGroupTypeEnum,
                          directDownstream: newStatus.downstream.map(
                            groupName => ({ groupName })
                          ),
                          latestBranch: null,
                          branches: []
                        }
                      ]
                ),
              branchName,
              branchType: branchType as GitAutomationGQL.IBranchGroupTypeEnum
            })
          )
          .switchMap(groupsData =>
            groupsToHierarchy(
              Observable.of(groupsData.groups),
              group =>
                group.groupName === groupsData.branchName ||
                Boolean(
                  group.upstream.find(v => v === groupsData.branchName)
                ) ||
                Boolean(group.downstream.find(v => v === groupsData.branchName))
            )
          );

        subscription.add(
          branchHierarchy({
            target: container.map(
              fnSelect<SVGSVGElement>(
                `svg[data-locator="hierarchy-container-preview"]`
              )
            ),
            navigate: state.navigate,
            data: hierarchy$
          }).subscribe()
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
