import * as React from "react";
import { Observable, BehaviorSubject } from "../utils/rxjs";
import { without } from "../utils/ramda";

import { ContextComponent } from "../utils/routing-component";
import { recommendGroups, updateBranch, detectUpstream } from "../api/basics";
import { handleErrorOnce } from "../handle-error";

export class AutoWireup extends ContextComponent {
  private selectedGroups = new BehaviorSubject<string[]>([]);
  private working = new BehaviorSubject(false);

  render() {
    return (
      <>
        <h1>Setup Wizard</h1>
        <h3>New Groups</h3>
        <ul>
          {recommendGroups()
            .multicast(this.selectedGroups)
            .refCount()
            .map(groups => (
              <>
                {groups.map(data => (
                  <li key={data}>
                    <label>
                      {this.selectedGroups
                        .map(g => g.indexOf(data) !== -1)
                        .switchMap(checked =>
                          this.working.map(working => ({ checked, working }))
                        )
                        .map(({ checked, working }) => (
                          <input
                            type="checkbox"
                            checked={checked}
                            disabled={working}
                            data-group-name={data}
                            onChange={this.toggleBranchSelected}
                          />
                        ))
                        .asComponent()}

                      <span>{data}</span>
                    </label>
                  </li>
                ))}
              </>
            ))
            .asComponent()}
        </ul>
        <button type="button" onClick={this.goHome}>
          Cancel
        </button>{" "}
        <button type="button" onClick={this.save}>
          Create New Groups
        </button>
      </>
    );
  }

  goHome = () =>
    this.context.injector.services.routeNavigate({
      url: "/",
      replaceCurentHistory: false
    });

  toggleBranchSelected = (event: React.ChangeEvent<HTMLInputElement>) =>
    this.selectedGroups.next(
      event.currentTarget.checked
        ? this.selectedGroups.value.concat(
            event.currentTarget.getAttribute("data-group-name")!
          )
        : without(
            [event.currentTarget.getAttribute("data-group-name")!],
            this.selectedGroups.value
          )
    );

  save = () => {
    this.working.next(true);
    Observable.of(this.selectedGroups.value)
      .map(groups =>
        Observable.merge(
          ...groups.map(group =>
            updateBranch(group, {
              upstreamMergePolicy: "None",
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
                upstreamMergePolicy: "None" as GitAutomationGQL.IUpstreamMergePolicyEnum,
                branchType: "Feature" as GitAutomationGQL.IBranchGroupTypeEnum,
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
          ...groups.map(({ group, settings }) => updateBranch(group, settings))
        )
          .toArray()
          .map(() => groups)
      )
      .switch()
      .subscribe(groups => {
        this.goHome();
      }, handleErrorOnce);
  };
}
