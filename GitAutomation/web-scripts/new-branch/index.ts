import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import { fnSelect, rxData, fnEvent } from "../utils/presentation/d3-binding";
import { allBranchGroups } from "../api/basics";
import { buildBranchCheckListing, checkedData } from "./branch-check-listing";
import { doSave } from "./bind-save-button";
import { style } from "typestyle";
import { classed } from "../style/style-binding";
import {
  branchHierarchy,
  defaultHierarchyStyles,
  highlightedHierarchyStyle
} from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";
import { secured } from "../security/security-binding";
import { inputValue, checkboxChecked } from "../utils/inputs";
import { handleError } from "../handle-error";

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
    .let(secured)
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
          container
            .map(fnSelect(`[data-locator="branch-name"]`))
            .let(inputValue({ includeInitial: true }))
            .map(e => e || "New Branch"),
          container
            .map(fnSelect(`[data-locator="recreate-from-upstream"]`))
            .let(checkboxChecked({ includeInitial: true })),
          container
            .map(fnSelect(`[data-locator="branch-type"]`))
            .let(inputValue({ includeInitial: true }))
            .map(v => v as GitAutomationGQL.IBranchGroupTypeEnum),
          checkedData(checkboxes)
        )
          .map(
            ([branchName, recreateFromUpstream, branchType, checkedData]) => ({
              branchName,
              recreateFromUpstream,
              branchType,
              ...checkedData
            })
          )
          .publishReplay(1)
          .refCount();

        const hierarchy$ = data
          .combineLatest(allBranchGroups, (newStatus, groups) => ({
            groups: groups
              .map(
                group =>
                  group.groupName === newStatus.branchName
                    ? {
                        ...group,
                        directDownstream: newStatus.downstream.map(
                          groupName => ({ groupName })
                        )
                      }
                    : {
                        ...group,
                        directDownstream: group.directDownstream
                          .filter(g => g.groupName !== newStatus.branchName)
                          .concat(
                            newStatus.upstream.find(
                              up => up === group.groupName
                            )
                              ? [{ groupName: newStatus.branchName }]
                              : []
                          )
                      }
              )
              .concat(
                groups.find(group => group.groupName === newStatus.branchName)
                  ? []
                  : [
                      {
                        groupName: newStatus.branchName,
                        branchType: newStatus.branchType as GitAutomationGQL.IBranchGroupTypeEnum,
                        directDownstream: newStatus.downstream.map(
                          groupName => ({ groupName })
                        ),
                        latestBranch: null,
                        branches: []
                      }
                    ]
              ),
            branchName: newStatus.branchName,
            branchType: newStatus.branchType as GitAutomationGQL.IBranchGroupTypeEnum
          }))
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

        let currentGroupName: string = "New Branch";
        subscription.add(
          data.map(d => d.branchName).subscribe(r => (currentGroupName = r))
        );

        subscription.add(
          branchHierarchy({
            target: container.map(
              fnSelect<SVGSVGElement>(
                `svg[data-locator="hierarchy-container-preview"]`
              )
            ),
            navigate: state.navigate,
            data: hierarchy$,
            style: [
              {
                ...highlightedHierarchyStyle("white"),
                filter: data => data.groupName === currentGroupName
              },
              ...defaultHierarchyStyles
            ]
          }).subscribe()
        );

        subscription.add(
          container
            .map(fnSelect('[data-locator="save"]'))
            .let(fnEvent("click"))
            .switchMap(() => doSave(data))
            .let(handleError)
            .subscribe({
              next: branchName => {
                state.navigate({
                  url: "/manage/" + branchName,
                  replaceCurentHistory: false
                });
              },
              error: _ => console.log(_)
            })
        );

        return subscription;
      })
    );
