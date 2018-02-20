import * as React from "react";
import { Observable, BehaviorSubject } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import { allBranchGroups, createBranch } from "../api/basics";
import { IBranchData, BranchCheckTable } from "./branch-check-listing";
import { branchHierarchy } from "../home/branch-hierarchy";
import { groupsToHierarchy } from "../api/hierarchy";
import { BranchType } from "../api/basic-branch";
import { RxD3 } from "../utils/rxjs-d3-component";
import { handleError } from "../handle-error";
import { BranchSettings, IBranchSettingsData } from "./branch-settings";

export class NewBranch extends ContextComponent {
  private readonly branchData = new BehaviorSubject<IBranchData[]>([]);
  private readonly branchSettings = new BehaviorSubject<IBranchSettingsData>({
    branchName: "",
    branchType: BranchType.Feature,
    upstreamMergePolicy: "None"
  });
  private readonly fullBranchData = Observable.combineLatest(
    this.branchSettings,
    this.branchData
  )
    .map(([{ branchName, branchType, upstreamMergePolicy }, branchData]) => ({
      branchName: branchName || "New Branch",
      upstreamMergePolicy,
      branchType,
      downstream: branchData
        .filter(branch => branch.isDownstream)
        .map(branch => branch.groupName),
      upstream: branchData
        .filter(branch => branch.isUpstream)
        .map(branch => branch.groupName)
    }))
    .publishReplay(1)
    .refCount();
  private readonly hierarchyData = this.fullBranchData
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

  componentDidMount() {
    allBranchGroups
      .take(1)
      .map(groups =>
        groups.map(group => ({
          ...group,
          isDownstream: false,
          isUpstream: false,
          isSomewhereUpstream: false,
          isDownstreamAllowed: true,
          isUpstreamAllowed: true,
          pullRequests: []
        }))
      )
      .subscribe(groups => this.branchData.next(groups));
  }

  render() {
    return (
      <>
        <h1>New Branch</h1>

        <h3>Settings</h3>
        {this.branchSettings
          .map(current => (
            <BranchSettings
              currentSettings={current}
              isNewBranch={true}
              updateSettings={v => this.branchSettings.next(v)}
            />
          ))
          .asComponent()}
        <h3>Other Branches</h3>
        {this.branchData
          .map(currentData => (
            <BranchCheckTable
              branches={currentData}
              updateBranches={branchData => this.branchData.next(branchData)}
              showDownstream={false}
            />
          ))
          .asComponent()}

        <section>
          <h5>Preview graph</h5>

          <RxD3
            do={target => () =>
              branchHierarchy({
                target: target as Observable<any>,
                navigate: this.handleNavigate,
                data: this.hierarchyData
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
  save = () => {
    const settings = this.branchSettings.value;
    createBranch(settings.branchName, {
      upstreamMergePolicy: settings.upstreamMergePolicy,
      branchType: settings.branchType,
      addUpstream: this.branchData.value
        .filter(branch => branch.isUpstream)
        .map(branch => branch.groupName)
    })
      .let(handleError)
      .subscribe(() => {
        this.context.injector.services.routeNavigate({
          url: "/manage/" + settings.branchName,
          replaceCurentHistory: false
        });
      });
  };
}
