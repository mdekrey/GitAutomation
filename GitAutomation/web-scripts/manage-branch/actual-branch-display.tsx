import * as React from "react";
import { StatelessObservableComponent } from "../utils/rxjs-component";
import { IManageBranch } from "./data";
import { ExternalLink } from "../external-window-link";
import { Secured } from "../security/security-binding";
import { BehaviorSubject } from "../utils/rxjs";
import { detectAllUpstream, deleteBranchByMode } from "../api/basics";
import { handleError } from "../handle-error";
import { confirmJsx } from "../utils/confirmation";

export class ActualBranchDisplay extends StatelessObservableComponent<{
  branches: IManageBranch["branches"];
}> {
  private readonly upToDate = new BehaviorSubject<{
    name: string;
    branches: string[];
  } | null>(null);

  render() {
    return (
      <table>
        <tbody>
          <tr style={{ verticalAlign: "top" }}>
            {this.prop$
              .map(p => p.branches)
              .map(branches => (
                <td>
                  <h3>Actual Branches</h3>
                  <ul>
                    {branches.map(data => (
                      <li key={data.name}>
                        <span>
                          {data.name} ({data.commit.substr(0, 7)})
                        </span>
                        <span>
                          <ExternalLink url={data.url} />
                        </span>{" "}
                        <Secured roleNames={["delete", "administrate"]}>
                          <a onClick={() => this.deleteSingleBranch(data.name)}>
                            Delete this
                          </a>{" "}
                        </Secured>
                        <a onClick={() => this.checkUpToDate(data.name)}>
                          What is up to date?
                        </a>
                      </li>
                    ))}
                  </ul>
                </td>
              ))
              .asComponent()}
            {this.upToDate
              .map(
                upToDateData =>
                  upToDateData ? (
                    <td>
                      <h3>
                        Branches Up-to-date in <span>{upToDateData.name}</span>
                      </h3>
                      <ul>
                        {upToDateData.branches.map(data => (
                          <li key={data}>{data}</li>
                        ))}
                      </ul>
                    </td>
                  ) : null
              )
              .asComponent()}
          </tr>
        </tbody>
      </table>
    );
  }

  deleteSingleBranch = (branchName: string) => {
    confirmJsx(<>Delete {branchName} from git?</>).subscribe(() =>
      deleteBranchByMode(branchName, "ActualBranchOnly")
        .take(1)
        .let(handleError)
        .subscribe()
    );
  };
  checkUpToDate = (branchName: string) => {
    detectAllUpstream(branchName)
      .take(1)
      .map(branches => ({ name: branchName, branches }))
      .multicast(this.upToDate)
      .connect();
  };
}
