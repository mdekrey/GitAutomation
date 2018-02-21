import * as React from "react";
import { Secured } from "../security/security-binding";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { IManageBranch } from "./data";
import { BehaviorSubject } from "../utils/rxjs";
import { SelectSubject, CheckboxObservable } from "../utils/subject-forms";
import { consolidateMerged } from "../api/basics";
import { handleError } from "../handle-error";
import { RoutingNavigate } from "@woosti/rxjs-router";

export class ConsolidateMerged extends StatelessObservableComponent<{
  branchName: string;
  otherBranches: IManageBranch["otherBranches"];
  navigate: RoutingNavigate;
}> {
  private targetBranch = new BehaviorSubject("");
  private originalBranches = new BehaviorSubject([] as string[]);

  componentDidMount() {
    this.unmounting.add(
      this.prop$
        .filter(p => Boolean(p.otherBranches[0]))
        .subscribe(p => this.targetBranch.next(p.otherBranches[0].groupName))
    );
  }

  render() {
    this.originalBranches;
    return (
      <Secured roleNames={["approve", "administrate"]}>
        <section>
          <h3>Consolidate Merged</h3>
          <label>
            <span>Consolidate Into</span>
            <SelectSubject subject={this.targetBranch}>
              {this.prop$
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
            </SelectSubject>
          </label>
          <ul data-locator="consolidate-original-branches">
            {this.prop$
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
                        <CheckboxObservable
                          observable={this.originalBranches.map(b =>
                            Boolean(b.indexOf(groupName) !== -1)
                          )}
                          observer={{
                            next: add =>
                              this.originalBranches.next(
                                add
                                  ? [...this.originalBranches.value, groupName]
                                  : this.originalBranches.value.filter(
                                      v => v !== groupName
                                    )
                              )
                          }}
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
    );
  }

  consolidateBranch = () => {
    consolidateMerged({
      targetBranch: this.targetBranch.value,
      originalBranches: this.originalBranches.value
    })
      .let(handleError)
      .subscribe(response => {
        this.props.navigate({ url: "/", replaceCurentHistory: false });
      });
  };
}
