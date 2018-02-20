import * as React from "react";
import { Observable, BehaviorSubject } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import { allBranchGroups, createBranch } from "../api/basics";
import {
  BranchCheckListing,
  CheckboxState,
  SelectableBranch,
  defaultState
} from "./branch-check-listing";
import { style } from "typestyle";
import { branchHierarchy } from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";
import { BranchType } from "../api/basic-branch";
import { RxD3 } from "../utils/rxjs-d3-component";
import { handleError } from "../handle-error";

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

export class NewBranch extends ContextComponent {
  private readonly newBranchName = new BehaviorSubject("");
  private readonly checkboxes = new BehaviorSubject<
    Record<string, CheckboxState>
  >({});
  private readonly branchType = new BehaviorSubject<BranchType>(
    BranchType.Feature
  );
  private readonly mergePolicy = new BehaviorSubject<
    GitAutomationGQL.IUpstreamMergePolicyEnum
  >("None");

  render() {
    const fullBranchData = Observable.combineLatest(
      this.newBranchName.map(e => e || "New Branch"),
      this.mergePolicy,
      this.branchType,
      this.checkboxes
    )
      .map(([branchName, upstreamMergePolicy, branchType, checkedData]) => ({
        branchName,
        upstreamMergePolicy,
        branchType,
        downstream: Object.keys(checkedData).filter(
          groupName => checkedData[groupName] && false
        ),
        upstream: Object.keys(checkedData).filter(
          groupName =>
            checkedData[groupName] && checkedData[groupName].upstreamChecked
        )
      }))
      .publishReplay(1)
      .refCount();

    const hierarchyData = fullBranchData
      .combineLatest(allBranchGroups, (newStatus, groups) => ({
        groups: groups
          .map(
            group =>
              group.groupName === newStatus.branchName
                ? {
                    ...group,
                    directDownstream: newStatus.downstream.map(groupName => ({
                      groupName
                    }))
                  }
                : {
                    ...group,
                    directDownstream: group.directDownstream
                      .filter(g => g.groupName !== newStatus.branchName)
                      .concat(
                        newStatus.upstream.find(up => up === group.groupName)
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
                    directDownstream: newStatus.downstream.map(groupName => ({
                      groupName
                    })),
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
            Boolean(group.upstream.find(v => v === groupsData.branchName)) ||
            Boolean(group.downstream.find(v => v === groupsData.branchName))
        )
      );

    return (
      <>
        <h1>New Branch</h1>
        <h3>Settings</h3>
        <section>
          <section>
            <label>
              Branch Name
              {this.newBranchName
                .map(name => (
                  <input
                    type="text"
                    value={name}
                    onChange={this.updateNewBranchName}
                  />
                ))
                .asComponent()}
            </label>
          </section>
          <section className={manageStyle.fieldSection}>
            <label>
              Branch Type
              {this.branchType
                .map(currentBranchType => (
                  <select
                    value={currentBranchType}
                    onChange={ev =>
                      this.branchType.next(ev.currentTarget.value as BranchType)
                    }
                  >
                    <option value="Feature">Feature</option>
                    <option value="ReleaseCandidate">Release Candidate</option>
                    <option value="ServiceLine">Service Line</option>
                    <option value="Infrastructure">Infrastructure</option>
                    <option value="Integration">Integration</option>
                    <option value="Hotfix">Hotfix</option>
                  </select>
                ))
                .asComponent()}
            </label>
          </section>
          <section className={manageStyle.fieldSection}>
            <label>
              Upstream Policy
              {this.mergePolicy
                .map(currentMergePolicy => (
                  <select
                    value={currentMergePolicy}
                    onChange={ev =>
                      this.mergePolicy.next(ev.currentTarget
                        .value as GitAutomationGQL.IUpstreamMergePolicyEnum)
                    }
                  >
                    <option value="None">None (merge normally)</option>
                    <option value="MergeNextIteration">
                      Create new branch Iteration
                    </option>
                    <option value="ForceFresh">Force update base branch</option>
                  </select>
                ))
                .asComponent()}
            </label>
            <p className={manageStyle.hint}>
              Used only with "Release Candidates"; will make new branches that
              contain only upstream commits
            </p>
          </section>
        </section>
        <h3>Other Branches</h3>
        <table className={manageStyle.otherBranchTable}>
          <thead>
            <tr>
              <td />
              <th className={manageStyle.rotateHeader}>
                <div>
                  <span>Upstream</span>
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            <BranchCheckListing
              styles={manageStyle}
              branches={allBranchGroups
                .take(1)
                .map(b => b as SelectableBranch[])}
              checkboxes={this.checkboxes.asObservable()}
              onUpstreamToggled={this.toggleUpstream}
            />
          </tbody>
        </table>

        <section>
          <h5>Preview graph</h5>

          <RxD3
            do={target => () =>
              branchHierarchy({
                target: target as Observable<any>,
                navigate: this.handleNavigate,
                data: hierarchyData
              })}
          >
            <svg width="800" height="100" style={{ maxHeight: "70vh" }} />
          </RxD3>
        </section>
        <button type="button" onClick={this.goHome}>
          Cancel
        </button>
        <button type="button" onClick={this.save}>
          Save
        </button>
      </>
    );
  }

  handleNavigate = this.context.injector.services.routeNavigate;
  goHome = () =>
    this.context.injector.services.routeNavigate({
      url: "/",
      replaceCurentHistory: false
    });
  updateNewBranchName = (event: React.ChangeEvent<HTMLInputElement>) =>
    this.newBranchName.next(event.currentTarget.value);
  toggleUpstream = (branchName: string, checked: boolean) => {
    // TODO - this would be MUCH cleaner with immer
    let state = this.checkboxes.value[branchName] || defaultState;
    state = Object.assign({}, state, { upstreamChecked: checked });
    const next = Object.assign({}, this.checkboxes.value, {
      [branchName]: state
    });
    this.checkboxes.next(next);
  };
  save = () => {
    const newBranchName = this.newBranchName.value;
    createBranch(newBranchName, {
      upstreamMergePolicy: this.mergePolicy.value,
      branchType: this.branchType.value,
      addUpstream: Object.keys(this.checkboxes.value).filter(
        groupName =>
          this.checkboxes.value[groupName] &&
          this.checkboxes.value[groupName].upstreamChecked
      )
    })
      .let(handleError)
      .subscribe(() => {
        this.context.injector.services.routeNavigate({
          url: "/manage/" + newBranchName,
          replaceCurentHistory: false
        });
      });
  };
}
