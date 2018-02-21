import * as React from "react";
import { Observable, BehaviorSubject } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import { IBranchData, BranchCheckTable } from "./branch-check-listing";
import { branchHierarchy } from "../home/branch-hierarchy";
import { BranchType } from "../api/basic-branch";
import { RxD3 } from "../utils/rxjs-d3-component";
import { handleError } from "../handle-error";
import { IBranchSettingsData, BranchSettings } from "./branch-settings";
import { Subscription, Subject } from "../utils/rxjs";

import { runBranchData, fromBranchDataToGraph, IManageBranch } from "./data";

import { doSave } from "./bind-save-button";
import { forceRefreshBranchGroups } from "../api/basics";
import { BranchNameDisplay } from "../branch-name-display";
import { ExternalLink } from "../external-window-link";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { Secured } from "../security/security-binding";
import { RoutingNavigate } from "@woosti/rxjs-router";

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

  private readonly upToDate = new BehaviorSubject<{
    name: string;
    branches: string[];
  } | null>(null);

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
          {this.branchSettings
            .map(b => <BranchNameDisplay branch={b} />)
            .asComponent()}
        </h1>

        <h3>Settings</h3>
        {this.branchSettings
          .map(current => (
            <BranchSettings
              currentSettings={current}
              isNewBranch={true}
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
                data: this.originalHierarchyData()
              })}
          >
            <svg width="800" height="100" style={{ maxHeight: "70vh" }} />
          </RxD3>
        </section>
        <a onClick={this.detectUpstream}>Detect Upstream Branches</a>

        {this.branchData
          .map(currentData => (
            <BranchCheckTable
              branches={currentData}
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
                data: this.hierarchyData()
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

        <table>
          <tbody>
            <tr style={{ verticalAlign: "top" }}>
              <td>
                <h3>Actual Branches</h3>
                <ul data-locator="grouped-branches">
                  {this.allBranchData
                    .map(b => b.branches)
                    .distinctUntilChanged()
                    .map(branches => (
                      <>
                        {branches.map(data => (
                          <li key={data.name}>
                            <span>
                              {data.name} ({data.commit.substr(0, 7)})
                            </span>
                            <span>
                              <ExternalLink url={data.url} />
                            </span>{" "}
                            <Secured roleNames={["delete", "administrate"]}>
                              <a
                                onClick={() =>
                                  this.deleteSingleBranch(data.name)
                                }
                              >
                                Delete this
                              </a>{" "}
                            </Secured>
                            <a onClick={() => this.checkUpToDate(data.name)}>
                              What is up to date?
                            </a>
                          </li>
                        ))}
                      </>
                    ))
                    .asComponent()}
                </ul>
              </td>
              <td data-locator="up-to-date">
                {this.upToDate
                  .map(
                    upToDateData =>
                      upToDateData ? (
                        <>
                          <h3>
                            Branches Up-to-date in{" "}
                            <span>{upToDateData.name}</span>
                          </h3>
                          <ul>
                            {upToDateData.branches.map(data => (
                              <li key={data}>{data}</li>
                            ))}
                          </ul>
                        </>
                      ) : null
                  )
                  .asComponent()}
              </td>
            </tr>
          </tbody>
        </table>

        <Secured roleNames={["approve", "administrate"]}>
          <section>
            <h3>Release to Service Line</h3>
            <label>
              <span>Approved Branch</span>
              <select data-locator="approved-branch" value="">
                {this.allBranchData
                  .map(b => b.branches)
                  .distinctUntilChanged()
                  .map(branches => (
                    <>
                      {branches.map(data => (
                        <option value={data.name} key={data.name}>
                          {data.name} ({data.commit.substr(0, 7)})
                        </option>
                      ))}
                    </>
                  ))
                  .asComponent()}
              </select>
            </label>
            <label>
              <span>Service Line Branch</span>
              <input type="text" data-locator="service-line-branch" value="" />
            </label>
            <label>
              <span>Release Tag</span>
              <input type="text" data-locator="release-tag" value="" />
            </label>
            <label>
              <input
                type="checkbox"
                data-locator="auto-consolidate"
                checked={false}
              />
              <span>Auto-consolidate</span>
            </label>
            <button type="button" onClick={() => this.promoteServiceLine()}>
              Release to Service Line
            </button>
          </section>

          <section>
            <h3>Consolidate Merged</h3>
            <label>
              <span>Consolidate Into</span>
              <select data-locator="consolidate-target-branch" value="">
                {this.allBranchData
                  .map(b => b.otherBranches)
                  .distinctUntilChanged()
                  .map(branches => (
                    <>
                      {branches.map(data => (
                        <option value={data.groupName} key={data.groupName}>
                          {data.groupName}
                        </option>
                      ))}
                    </>
                  ))
                  .asComponent()}
              </select>
            </label>
            <ul data-locator="consolidate-original-branches">
              {this.allBranchData
                .map(b => b.otherBranches)
                .map(branches =>
                  [this.props.branchName].concat(
                    branches
                      .filter(branch => branch.isSomewhereUpstream)
                      .map(branch => branch.groupName)
                  )
                )
                .map(groupNames => (
                  <>
                    {groupNames.map(groupName => (
                      <li key={groupName}>
                        <label>
                          <input
                            type="checkbox"
                            data-locator="consolidate-original-branch"
                            checked={false}
                          />
                          <span>{groupName}</span>
                        </label>
                      </li>
                    ))}
                  </>
                ))
                .asComponent()}
            </ul>
            <button type="button" onClick={() => this.consolidateBranch()}>
              Consolidate Branch
            </button>
          </section>
        </Secured>

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
      .subscribe();
  };
  detectUpstream = () => {
    // TODO
  };
  deleteSingleBranch = (branchName: string) => {
    // TODO
  };
  delete = () => {
    // TODO
  };
  deleteConfiguration = () => {
    // TODO
  };
  checkUpToDate = (branchName: string) => {
    // TODO
  };
  promoteServiceLine = () => {
    // TODO
  };
  consolidateBranch = () => {
    // TODO
  };
}
