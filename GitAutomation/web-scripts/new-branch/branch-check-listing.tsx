import * as React from "react";
import { BranchNameDisplay } from "../branch-name-display";
import { Observable } from "../utils/rxjs";

export type SelectableBranch = Pick<
  GitAutomationGQL.IBranchGroupDetails,
  "groupName" | "branchType"
>;

export interface CheckboxState {
  allowUpstream: boolean;
  upstreamChecked: boolean;
}

export const defaultState: CheckboxState = {
  allowUpstream: true,
  upstreamChecked: false
};

export interface IBranchCheckListingProps {
  styles: { branchName: string; checkboxCell: string };
  branches: Observable<SelectableBranch[]>;
  checkboxes: Observable<Record<string, CheckboxState>>;

  onUpstreamToggled: (branchName: string, checked: boolean) => void;
}

export const BranchCheckListing = ({
  branches,
  styles,
  checkboxes,
  onUpstreamToggled
}: IBranchCheckListingProps) => (
  <>
    {branches
      .map(all => (
        <>
          {all.map(data => (
            <tr key={data.groupName}>
              <td className={styles.branchName}>
                <BranchNameDisplay branch={data} />
              </td>
              <td
                data-locator="upstream-branches"
                className={styles.checkboxCell}
              >
                {checkboxes
                  .map(records => records[data.groupName])
                  .map(state => state || defaultState)
                  .map(state => (
                    <input
                      type="checkbox"
                      checked={state.upstreamChecked}
                      disabled={!state.allowUpstream}
                      onChange={() =>
                        onUpstreamToggled(
                          data.groupName,
                          !state.upstreamChecked
                        )
                      }
                    />
                  ))
                  .asComponent()}
              </td>
            </tr>
          ))}
        </>
      ))
      .asComponent()}
  </>
);
