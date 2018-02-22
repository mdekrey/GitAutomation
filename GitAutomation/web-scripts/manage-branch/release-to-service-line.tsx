import * as React from "react";
import { Secured } from "../security/security-binding";
import { BehaviorSubject, Observable } from "../utils/rxjs";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { IManageBranch } from "./data";
import {
  InputSubject,
  SelectSubject,
  CheckboxSubject
} from "../utils/subject-forms";
import { promoteServiceLine } from "../api/basics";
import { handleError } from "../handle-error";
import { RoutingNavigate } from "@woosti/rxjs-router";

export class ReleaseToServiceLine extends StatelessObservableComponent<{
  branches: IManageBranch["branches"];
  navigate: RoutingNavigate;
}> {
  private approvedBranch = new BehaviorSubject("");
  private serviceLineBranch = new BehaviorSubject("");
  private releaseTag = new BehaviorSubject("");
  private autoConsolidate = new BehaviorSubject(false);

  componentDidMount() {
    this.unmounting.add(
      this.prop$
        .filter(p => Boolean(p.branches[0]))
        .subscribe(p => this.approvedBranch.next(p.branches[0].name))
    );
  }

  render() {
    return (
      <Secured roleNames={["approve", "administrate"]}>
        <section>
          <h3>Release to Service Line</h3>
          <label>
            <span>Approved Branch</span>
            <SelectSubject subject={this.approvedBranch}>
              {this.prop$
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
            </SelectSubject>
          </label>
          <label>
            <span>Service Line Branch</span>
            <InputSubject type="text" subject={this.serviceLineBranch} />
          </label>
          <label>
            <span>Release Tag</span>
            <InputSubject type="text" subject={this.releaseTag} />
          </label>
          <label>
            <CheckboxSubject subject={this.autoConsolidate} />
            <span>Auto-consolidate</span>
          </label>
          <button type="button" onClick={() => this.promoteServiceLine()}>
            Release to Service Line
          </button>
        </section>
      </Secured>
    );
  }

  promoteServiceLine = () => {
    Observable.combineLatest(
      this.approvedBranch,
      this.serviceLineBranch,
      this.releaseTag,
      this.autoConsolidate
    )
      .map(([releaseCandidate, serviceLine, tagName, autoConsolidate]) => ({
        releaseCandidate,
        serviceLine,
        tagName,
        autoConsolidate
      }))
      .take(1)
      .switchMap(promoteServiceLine)
      .let(handleError)
      .subscribe(response => {
        this.props.navigate({ url: "/", replaceCurentHistory: false });
      });
  };
}
