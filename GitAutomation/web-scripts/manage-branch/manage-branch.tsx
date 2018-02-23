import * as React from "react";
import { Observable, BehaviorSubject } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import { IBranchData, BranchCheckTable } from "./branch-check-listing";
import {
  branchHierarchy,
  highlightedHierarchyStyle,
  addDefaultHierarchyStyles
} from "../home/branch-hierarchy";
import { BranchType } from "../api/basic-branch";
import { RxD3 } from "../utils/rxjs-d3-component";
import { handleError, handleErrorOnce } from "../handle-error";
import { IBranchSettingsData, BranchSettings } from "./branch-settings";
import { Subscription, Subject } from "../utils/rxjs";

import { runBranchData, fromBranchDataToGraph, IManageBranch } from "./data";

import { doSave } from "./bind-save-button";
import {
  forceRefreshBranchGroups,
  detectUpstream,
  deleteBranch,
  deleteBranchByMode
} from "../api/basics";
import { BranchNameDisplay } from "../branch-name-display";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { RoutingNavigate } from "@woosti/rxjs-router";
import { ActualBranchDisplay } from "./actual-branch-display";
import { ReleaseToServiceLine } from "./release-to-service-line";
import { ConsolidateMerged } from "./consolidate-merged";
import { equals } from "../utils/ramda";

export class ManageBranch extends ContextComponent {
  render() {
    return this.context.injector.services.routingStrategy
      .map(s => s.state.remainingPath!)
      .map(branchName => (
        <ManageBranchInputed
          branchName={branchName}
          key={branchName}
          routeNavigate={this.context.injector.services.routeNavigate}
        />
      ))
      .asComponent();
  }
}

interface ManageBranchInputedProps {
  branchName: string;
  routeNavigate: RoutingNavigate;
}

class ManageBranchInputed extends StatelessObservableComponent<
  ManageBranchInputedProps
> {
  private readonly reload = new Subject<null>();
  private readonly reset = new Subject<null>();
  private readonly stopRefreshing = new Subject<null>();

  private allBranchData: Observable<IManageBranch>;
  private readonly branchSettings = new BehaviorSubject<IBranchSettingsData>({
    groupName: "",
    branchType: BranchType.Feature,
    upstreamMergePolicy: "None"
  });
  private readonly branchData = new BehaviorSubject<IBranchData[]>([]);
  private readonly hierarchyData = () =>
    fromBranchDataToGraph(this.branchData, this.branchSettings);
  private readonly originalHierarchyData = () =>
    fromBranchDataToGraph(
      this.allBranchData.map(({ otherBranches }) => otherBranches),
      this.allBranchData.map(
        ({ groupName, branchType, upstreamMergePolicy }) => ({
          groupName,
          branchType,
          upstreamMergePolicy
        })
      )
    );

  constructor(props: ManageBranchInputedProps) {
    super(props);

    const branchData = runBranchData(
      props.branchName,
      this.reload
    ).publishReplay(1);
    this.allBranchData = branchData;
    // allows us to control when to connect to the server. Send a "stopRefreshing" message to break the data connection
    let dataConnection: Subscription | null = null;
    this.unmounting.add(
      this.stopRefreshing.subscribe(() => {
        if (dataConnection) {
          dataConnection.unsubscribe();
          dataConnection = null;
        }
      })
    );

    this.unmounting.add(
      branchData
        .map(({ groupName, branchType, upstreamMergePolicy }) => ({
          groupName,
          branchType,
          upstreamMergePolicy
        }))
        .multicast(this.branchSettings)
        .connect()
    );

    this.unmounting.add(
      branchData
        .map(({ otherBranches }) => otherBranches)
        .multicast(this.branchData)
        .connect()
    );

    this.unmounting.add(
      this.reload
        .merge(this.reset)
        .startWith(null)
        .subscribe(() => {
          if (dataConnection === null) {
            this.unmounting.add((dataConnection = branchData.connect()));
          }
        })
    );

    this.unmounting.add(
      this.reload.subscribe(() => forceRefreshBranchGroups.next(null))
    );
  }

  render() {
    return (
      <>
        <h1>
          {Observable.combineLatest(this.branchSettings, this.allBranchData)
            .map(([current, fromServer]) => ({ ...fromServer, ...current }))
            .map(b => <BranchNameDisplay branch={b} />)
            .asComponent()}
        </h1>

        <h3>Settings</h3>
        {this.branchSettings
          .map(current => (
            <BranchSettings
              currentSettings={current}
              isNewBranch={false}
              updateSettings={v => {
                this.branchSettings.next({
                  ...this.branchSettings.value,
                  ...v
                });
                this.stopRefreshing.next(null);
              }}
            />
          ))
          .asComponent()}
        <h3>Other Branches</h3>
        <section>
          <h5>Current relevant branches</h5>

          <RxD3
            do={target => () =>
              branchHierarchy({
                target: target as Observable<any>,
                navigate: this.handleNavigate,
                data: this.originalHierarchyData(),
                style: addDefaultHierarchyStyles([
                  {
                    ...highlightedHierarchyStyle,
                    filter: data => data.groupName === this.props.branchName
                  }
                ])
              })}
          >
            <svg width="800" height="100" style={{ maxHeight: "70vh" }} />
          </RxD3>
        </section>
        <a onClick={this.detectUpstream}>Detect Upstream Branches</a>

        {this.branchData
          .map(currentData => (
            <BranchCheckTable
              branches={currentData.filter(
                b => b.groupName !== this.props.branchName
              )}
              updateBranches={branchData => {
                this.branchData.next(branchData);
                this.stopRefreshing.next(null);
              }}
              showDownstream={true}
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
                data: this.hierarchyData(),
                style: addDefaultHierarchyStyles([
                  {
                    ...highlightedHierarchyStyle,
                    filter: data => data.groupName === this.props.branchName
                  }
                ])
              })}
          >
            <svg width="800" height="100" style={{ maxHeight: "70vh" }} />
          </RxD3>
        </section>
        <button type="button" onClick={() => this.reset.next(null)}>
          Reset
        </button>
        <button type="button" onClick={this.goHome}>
          Cancel
        </button>
        <button type="button" onClick={this.save}>
          Save
        </button>

        {this.allBranchData
          .map(b => b.branches)
          .distinctUntilChanged()
          .map(branches => <ActualBranchDisplay branches={branches} />)
          .asComponent()}

        {this.allBranchData
          .map(({ branches }) => branches)
          .distinctUntilChanged<IManageBranch["branches"]>(equals)
          .map(branches => (
            <ReleaseToServiceLine
              branches={branches}
              navigate={this.props.routeNavigate}
            />
          ))
          .asComponent()}

        {this.allBranchData
          .map(b => b.otherBranches)
          .distinctUntilChanged()
          .map(otherBranches => (
            <ConsolidateMerged
              branchName={this.props.branchName}
              otherBranches={otherBranches}
              navigate={this.props.routeNavigate}
            />
          ))
          .asComponent()}

        <section data-role="delete administrate">
          <h3>Delete Branch</h3>
          <p>This action cannot be undone.</p>
          <button type="button" onClick={() => this.delete()}>
            Delete
          </button>
          <button type="button" onClick={() => this.deleteConfiguration()}>
            Delete Group
          </button>
        </section>
      </>
    );
  }

  get handleNavigate() {
    return this.props.routeNavigate;
  }
  goHome = () =>
    this.props.routeNavigate({
      url: "/",
      replaceCurentHistory: false
    });
  save = () => {
    doSave(
      this.allBranchData.map(b => b.otherBranches),
      this.branchSettings,
      this.branchData
    )
      .let(handleError)
      .subscribe(() => this.reload.next(null));
  };
  detectUpstream = () => {
    detectUpstream(this.props.branchName, true)
      .take(1)
      .subscribe(
        branchNames =>
          this.branchData.next(
            this.branchData.value.map(
              branch =>
                branchNames.indexOf(branch.groupName) >= 0
                  ? { ...branch, isUpstream: true }
                  : branch
            )
          ),
        handleErrorOnce
      );
  };
  delete = () => {
    deleteBranch(this.props.branchName)
      .let(handleError)
      .subscribe(this.goHome);
  };
  deleteConfiguration = () => {
    deleteBranchByMode(this.props.branchName, "GroupOnly")
      .let(handleError)
      .subscribe(this.goHome);
  };
}
