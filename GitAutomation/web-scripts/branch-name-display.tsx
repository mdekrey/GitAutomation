import * as React from "react";
import { branchTypeColors } from "./style/branch-colors";
import { ExternalLink } from "./external-window-link";
import { BehaviorSubject } from "./utils/rxjs";
import { ContextComponent } from "./utils/routing-component";

export interface DisplayableBranch {
  groupName: string;
  branchType?: GitAutomationGQL.IBranchGroupTypeEnum;
  branches?: Array<Partial<GitAutomationGQL.IGitRef>>;
  latestBranch?: ({ name: string } & Partial<GitAutomationGQL.IGitRef>) | null;
}

export const hoveredGroupName = new BehaviorSubject<string | null>(null);

function findActualBranch<P extends keyof GitAutomationGQL.IGitRef>(
  data: DisplayableBranch,
  targetKey: P
) {
  if (data.latestBranch) {
    const latest: Partial<GitAutomationGQL.IGitRef> = data.latestBranch;
    if (latest[targetKey]) {
      return latest[targetKey];
    }

    if (data.branches) {
      const found = data.branches.find(b => b.name === latest.name);
      if (found && found[targetKey]) {
        return found[targetKey];
      }
    }
  }
  return undefined;
}

function findStatuses(data: DisplayableBranch) {
  return findActualBranch(data, "statuses") || [];
}

export interface IBranchNameDisplayProps {
  branch: DisplayableBranch;
}

export class BranchNameDisplay extends ContextComponent<
  IBranchNameDisplayProps
> {
  render() {
    const { branch } = this.props;
    return (
      <span onMouseEnter={this.beginHover} onMouseLeave={this.endHover}>
        <a
          style={{
            color: branch.branchType
              ? branchTypeColors[branch.branchType][0].toHexString()
              : null,
            cursor: "pointer"
          }}
          href={this.context.injector.services.routeHrefBuilder(
            "/manage/" + branch.groupName
          )}
        >
          {branch.groupName}
        </a>
        <ExternalLink url={findActualBranch(branch, "url")} />
        <span data-locator="status">
          {findStatuses(branch).map(status => (
            <a
              target="_blank"
              href={status.url}
              key={status.key}
              title={`${status.key} - ${status.description}`}
            >
              {status.state === "Success" ? (
                <img
                  alt="Build Successful"
                  className="text-image"
                  src={require("./images/green-check.svg")}
                />
              ) : status.state === "Error" ? (
                <img
                  alt="Build Error"
                  className="text-image"
                  src={require("./images/red-x.svg")}
                />
              ) : (
                <img
                  alt="Build Pending"
                  className="text-image"
                  src={require("./images/question-mark.svg")}
                />
              )}
            </a>
          ))}
        </span>
      </span>
    );
  }

  beginHover = () => hoveredGroupName.next(this.props.branch.groupName);
  endHover = () => hoveredGroupName.next(null);
}
