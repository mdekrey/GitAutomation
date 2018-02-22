import * as React from "react";
import produce from "immer";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { BranchType } from "../api/basic-branch";
import { SelectObservable, InputObservable } from "../utils/subject-forms";
import { manageStyle } from "./form-styles";

export type IBranchSettingsData = Pick<
  GitAutomationGQL.IBranchGroupDetails,
  "groupName" | "branchType" | "upstreamMergePolicy"
>;

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
          .map(({ isNewBranch }) => isNewBranch)
          .distinctUntilChanged()
          .map(
            isNewBranch =>
              isNewBranch ? (
                <section>
                  <label>
                    <span className={manageStyle.fieldLabel}>Branch Name</span>
                    <InputObservable
                      observable={this.prop$.map(
                        p => p.currentSettings.groupName
                      )}
                      observer={{ next: this.updateBranchName }}
                    />
                  </label>
                </section>
              ) : null
          )
          .asComponent()}
        <label className={manageStyle.fieldSection}>
          <span className={manageStyle.fieldLabel}>Branch Type</span>
          <SelectObservable
            observable={this.prop$.map(p => p.currentSettings.branchType)}
            observer={{ next: this.updateBranchType }}
          >
            <option value="Feature">Feature</option>
            <option value="ReleaseCandidate">Release Candidate</option>
            <option value="ServiceLine">Service Line</option>
            <option value="Infrastructure">Infrastructure</option>
            <option value="Integration">Integration</option>
            <option value="Hotfix">Hotfix</option>
          </SelectObservable>
        </label>
        <label className={manageStyle.fieldSection}>
          <span className={manageStyle.fieldLabel}>Upstream Policy</span>
          <SelectObservable
            observable={this.prop$.map(
              p => p.currentSettings.upstreamMergePolicy
            )}
            observer={{ next: this.updateMergePolicy }}
          >
            <option value="None">None (merge normally)</option>
            <option value="MergeNextIteration">
              Create new branch Iteration
            </option>
            <option value="ForceFresh">Force update base branch</option>
          </SelectObservable>
        </label>
        <p className={manageStyle.hint}>
          Used only with "Release Candidates"; will make new branches that
          contain only upstream commits
        </p>
      </section>
    );
  }

  updateBranchName = (branchName: string) =>
    this.props.updateSettings(
      produce(this.props.currentSettings, draft => {
        draft.groupName = branchName;
      })
    );

  updateBranchType = (branchType: BranchType) =>
    this.props.updateSettings(
      produce(this.props.currentSettings, draft => {
        draft.branchType = branchType;
      })
    );
  updateMergePolicy = (
    mergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum
  ) =>
    this.props.updateSettings(
      produce(this.props.currentSettings, draft => {
        draft.upstreamMergePolicy = mergePolicy;
      })
    );
}
