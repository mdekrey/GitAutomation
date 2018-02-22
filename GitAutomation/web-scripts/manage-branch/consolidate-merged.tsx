import * as React from "react";
import { Secured } from "../security/security-binding";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { IManageBranch } from "./data";
import { BehaviorSubject } from "../utils/rxjs";
import { SelectSubject } from "../utils/subject-forms";
import { consolidateMerged } from "../api/basics";
import { handleError } from "../handle-error";
import { RoutingNavigate } from "@woosti/rxjs-router";

export class ConsolidateMerged extends StatelessObservableComponent<{
  branchName: string;
  otherBranches: IManageBranch["otherBranches"];
  navigate: RoutingNavigate;
}> {
  private targetBranch = new BehaviorSubject("");

  componentDidMount() {
    this.unmounting.add(
      this.prop$
        .filter(p => Boolean(p.otherBranches[0]))
        .subscribe(p => this.targetBranch.next(p.otherBranches[0].groupName))
    );
  }

  render() {
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
      originalBranch: this.props.branchName
    })
      .let(handleError)
      .subscribe(response => {
        this.props.navigate({ url: "/", replaceCurentHistory: false });
      });
  };
}
