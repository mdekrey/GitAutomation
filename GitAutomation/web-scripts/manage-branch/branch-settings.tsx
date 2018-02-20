import * as React from "react";
import { style } from "typestyle";
import produce from "immer";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { BranchType } from "../api/basic-branch";
import { equals } from "../utils/ramda";

const manageStyle = {
  fieldSection: style({
    marginTop: "0.5em"
  }),
  hint: style({
    margin: 0,
    padding: 0,
    fontSize: "0.75rem"
  })
};

export interface IBranchSettingsData {
  branchName: string;
  branchType: BranchType;
  upstreamMergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum;
}

export interface IBranchSettingsProps {
  isNewBranch: boolean;
  currentSettings: IBranchSettingsData;
  updateSettings: (newSettings: IBranchSettingsData) => void;
}

export class BranchSettings extends StatelessObservableComponent<
  IBranchSettingsProps
> {
  render() {
    return (
      <section>
        {this.prop$
          .map(({ isNewBranch, currentSettings: { branchName } }) => ({
            isNewBranch,
            branchName
          }))
          .distinctUntilChanged(equals)
          .map(
            ({ isNewBranch, branchName }) =>
              isNewBranch ? (
                <section>
                  <label>
                    Branch Name
                    <input
                      type="text"
                      value={branchName}
                      onChange={ev =>
                        this.updateBranchName(ev.currentTarget.value)
                      }
                    />
                  </label>
                </section>
              ) : null
          )
          .asComponent()}
        <section className={manageStyle.fieldSection}>
          <label>
            Branch Type
            {this.prop$
              .map(p => p.currentSettings.branchType)
              .distinctUntilChanged()
              .map(currentBranchType => (
                <select
                  value={currentBranchType}
                  onChange={ev =>
                    this.updateBranchType(ev.currentTarget.value as BranchType)
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
            {this.prop$
              .map(p => p.currentSettings.upstreamMergePolicy)
              .distinctUntilChanged()
              .map(currentMergePolicy => (
                <select
                  value={currentMergePolicy}
                  onChange={ev =>
                    this.updateMergePolicy(ev.currentTarget
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
    );
  }

  updateBranchName = (branchName: string) =>
    this.props.updateSettings(
      produce(
        this.props.currentSettings,
        draft => (draft.branchName = branchName)
      )
    );

  updateBranchType = (branchType: BranchType) =>
    this.props.updateSettings(
      produce(
        this.props.currentSettings,
        draft => (draft.branchType = branchType)
      )
    );
  updateMergePolicy = (
    mergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum
  ) =>
    this.props.updateSettings(
      produce(
        this.props.currentSettings,
        draft => (draft.upstreamMergePolicy = mergePolicy)
      )
    );
}
