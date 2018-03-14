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
import { promoteServiceLine, allBranchesHierarchy } from "../api/basics";
import { handleError } from "../handle-error";
import { RoutingNavigate } from "@woosti/rxjs-router";
import { equals } from "ramda";
import { BranchType } from "../api/basic-branch";
import { style } from "typestyle";
import { manageStyle } from "./form-styles";
import { sortBy } from "../utils/ramda";

const formLabel = style({
  display: "inline-block",
  width: "200px"
});

export class ReleaseToServiceLine extends StatelessObservableComponent<{
  latestBranch: IManageBranch["latestBranch"];
  branches: IManageBranch["branches"];
  otherBranches?: IManageBranch["otherBranches"];
  navigate: RoutingNavigate;
}> {
  private approvedBranch = new BehaviorSubject("");
  private serviceLineBranch = new BehaviorSubject("");
  private releaseTag = new BehaviorSubject("");
  private autoConsolidate = new BehaviorSubject(true);

  componentDidMount() {
    this.unmounting.add(
      this.prop$
        .filter(p => Boolean(p.latestBranch))
        .subscribe(p => this.approvedBranch.next(p.latestBranch!.name))
    );
  }

  render() {
    const serviceLineNames = this.prop$
      .map(p =>
        (p.otherBranches || [])
          .filter(
            b =>
              b.branchType === BranchType.ServiceLine && b.isSomewhereUpstream
          )
          .map(b => b.groupName)
      )
      .distinctUntilChanged<string[]>(equals);
    serviceLineNames
      .combineLatest(allBranchesHierarchy)
      .subscribe(([lines, allBranchesHierarchy]) =>
        this.serviceLineBranch.next(
          sortBy(
            line => -allBranchesHierarchy[line].hierarchyDepth,
            lines
          )[0] || ""
        )
      );

    return (
      <Secured roleNames={["approve", "administrate"]}>
        <section>
          <h3>Release to Service Line</h3>
          <label className={manageStyle.fieldSection}>
            <span className={manageStyle.fieldLabel}>Approved Branch</span>
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
          <label className={manageStyle.fieldSection}>
            <span className={formLabel}>Service Line Branch</span>
            <SelectSubject subject={this.serviceLineBranch}>
              <option value="">Other Service Line...</option>
              {serviceLineNames
                .map(branchNames => (
                  <>
                    {branchNames.map(branchName => (
                      <option value={branchName} key={branchName}>
                        {branchName}
                      </option>
                    ))}
                  </>
                ))
                .asComponent()}
            </SelectSubject>
            {serviceLineNames
              .combineLatest(this.serviceLineBranch)
              .map(([lines, selected]) => lines.indexOf(selected) >= 0)
              .map(
                matching =>
                  matching ? null : (
                    <>
                      {" "}
                      <InputSubject
                        type="text"
                        subject={this.serviceLineBranch}
                      />
                    </>
                  )
              )
              .asComponent()}
          </label>
          <label className={manageStyle.fieldSection}>
            <span className={formLabel}>Release Tag (optional)</span>
            <InputSubject type="text" subject={this.releaseTag} />
          </label>
          <label className={manageStyle.fieldSection}>
            <CheckboxSubject subject={this.autoConsolidate} />
            <span>Auto-consolidate</span>
          </label>
          <button
            type="button"
            onClick={() => this.promoteServiceLine()}
            className={manageStyle.fieldSection}
          >
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
