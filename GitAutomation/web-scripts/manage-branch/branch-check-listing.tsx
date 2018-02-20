import * as React from "react";
import { BranchNameDisplay } from "../branch-name-display";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { style } from "typestyle";
import produce from "immer";

export interface IBranchData {
  groupName: string;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
  latestBranch: {
    name: string;
  } | null;
  branches: GitAutomationGQL.IGitRef[];
  isDownstream: boolean;
  isUpstream: boolean;
  isSomewhereUpstream: boolean;
  isDownstreamAllowed: boolean;
  isUpstreamAllowed: boolean;
  pullRequests: GitAutomationGQL.IPullRequest[];
}

export interface IBranchCheckListingProps {
  branches: IBranchData[];

  onDownstreamToggled?: (branchName: string, checked: boolean) => void;
  onUpstreamToggled: (branchName: string, checked: boolean) => void;
}

const manageStyle = {
  rotateHeader: style({
    height: "100px",
    whiteSpace: "nowrap",
    width: "25px",
    verticalAlign: "bottom",
    padding: "0",
    $nest: {
      "> div": {
        transformOrigin: "bottom left",
        transform: "translate(26px, 0px) rotate(-60deg)",
        width: "25px",
        $nest: {
          "> span": {
            borderBottom: "1px solid #ccc",
            padding: "0"
          }
        }
      }
    }
  }),
  otherBranchTable: style({
    borderCollapse: "collapse"
  }),
  checkboxCell: style({
    borderRight: "1px solid #ccc",
    textAlign: "center"
  }),
  branchName: style({
    textAlign: "right",
    fontWeight: "bold"
  })
};

export const BranchCheckListing = ({
  branches,
  onDownstreamToggled,
  onUpstreamToggled
}: IBranchCheckListingProps) => (
  <>
    {branches.map(data => (
      <tr key={data.groupName}>
        <td className={manageStyle.branchName}>
          <BranchNameDisplay branch={data} />
        </td>
        {onDownstreamToggled ? (
          <td className={manageStyle.checkboxCell}>
            <input
              type="checkbox"
              checked={data.isDownstream}
              disabled={!data.isDownstreamAllowed}
              onChange={() =>
                onDownstreamToggled(data.groupName, !data.isDownstream)
              }
            />
          </td>
        ) : null}
        <td className={manageStyle.checkboxCell}>
          <input
            type="checkbox"
            checked={data.isUpstream}
            disabled={!data.isUpstreamAllowed}
            onChange={() => onUpstreamToggled(data.groupName, !data.isUpstream)}
          />
        </td>
        <td>
          <ul data-locator="pr-status" />
        </td>
      </tr>
    ))}
  </>
);

export class BranchCheckTable extends StatelessObservableComponent<{
  branches: IBranchData[];
  updateBranches: (newData: IBranchData[]) => void;
  showDownstream: boolean;
}> {
  render() {
    return (
      <table className={manageStyle.otherBranchTable}>
        <thead>
          <tr>
            <td />
            {this.prop$
              .map(
                ({ showDownstream }) =>
                  showDownstream ? (
                    <th className={manageStyle.rotateHeader}>
                      <div>
                        <span>Upstream</span>
                      </div>
                    </th>
                  ) : null
              )
              .asComponent()}
            <th className={manageStyle.rotateHeader}>
              <div>
                <span>Upstream</span>
              </div>
            </th>
          </tr>
        </thead>
        <tbody>
          {this.prop$
            .map(props => (
              <BranchCheckListing
                branches={props.branches}
                onUpstreamToggled={this.toggleUpstream}
                onDownstreamToggled={
                  this.props.showDownstream ? this.toggleDownstream : undefined
                }
              />
            ))
            .asComponent()}
        </tbody>
      </table>
    );
  }

  toggleUpstream = (branchName: string, checked: boolean) => {
    this.props.updateBranches(
      produce(this.props.branches, draft => {
        const target = draft.find(b => b.groupName === branchName);
        if (target) {
          target.isUpstream = checked;
        }
        return draft;
      })
    );
  };

  toggleDownstream = (branchName: string, checked: boolean) => {
    this.props.updateBranches(
      produce(this.props.branches, draft => {
        const target = draft.find(b => b.groupName === branchName);
        if (target) {
          target.isDownstream = checked;
        }
        return draft;
      })
    );
  };
}
