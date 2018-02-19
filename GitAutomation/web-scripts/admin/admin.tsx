import * as React from "react";
import { Subject, BehaviorSubject, Observable } from "../utils/rxjs";

import {
  allUsers,
  allRoles,
  updateUser,
  forceRefreshUsers
} from "../api/basics";
import { Secured } from "../security/security-binding";

interface UserRoleChange {
  username: string;
  permission: string;
  checked: boolean;
}

export class Admin extends React.PureComponent<{}, never> {
  private readonly permissions = allRoles
    .map(roles => roles.map(({ role }) => role))
    .publishReplay(1)
    .refCount();
  private readonly newBranchName = new BehaviorSubject("");
  private readonly changeUserRole = new Subject<UserRoleChange>();
  private userRoleChanges: Observable<UserRoleChange[]>;

  componentDidMount() {
    this.userRoleChanges = this.changeUserRole
      .scan(
        (current, next) =>
          current
            .filter(
              v =>
                v.username !== next.username || v.permission !== next.permission
            )
            .concat(next),
        []
      )
      .publishReplay(1)
      .refCount();
  }

  render() {
    return (
      <>
        <h1>Manage Users</h1>

        <table data-locator="users">
          <thead>
            <tr>
              <th>User name</th>
              {this.permissions
                .map(roles => (
                  <>
                    {roles.map(permission => (
                      <th key={permission}>{permission}</th>
                    ))}
                  </>
                ))
                .asComponent()}
              <Secured roleNames={["administrate"]}>
                <th data-locator="actions">Actions</th>
              </Secured>
            </tr>
          </thead>
          <tbody>
            {allUsers
              .map(users => (
                <>
                  {users.map(user => (
                    <tr key={user.username}>
                      <th>{user.username}</th>
                      <this.renderPermissionCheckboxes
                        username={user.username}
                        roles={user.roles}
                      />
                      <Secured roleNames={["administrate"]}>
                        <td>
                          <button
                            data-locator="save-user"
                            data-username={user.username}
                            onClick={this.saveUser}
                          >
                            Save User
                          </button>
                        </td>
                      </Secured>
                    </tr>
                  ))}
                </>
              ))
              .asComponent()}
            <Secured roleNames={["administrate"]}>
              <tr>
                <td>
                  {this.newBranchName
                    .map(name => (
                      <input
                        type="text"
                        data-locator="user-name-value"
                        value={name}
                        onChange={this.updateNewBranchName}
                      />
                    ))
                    .asComponent()}
                </td>
                <this.renderPermissionCheckboxes username={""} roles={[]} />
                <td>
                  <button
                    data-locator="save-user"
                    data-username={""}
                    onClick={this.saveUser}
                  >
                    Save User
                  </button>
                </td>
              </tr>
            </Secured>
          </tbody>
        </table>
      </>
    );
  }

  private renderPermissionCheckboxes = (user: {
    username: string;
    roles: string[];
  }) => {
    return this.permissions
      .map(roles => (
        <>
          {roles.map(permission => (
            <td key={permission}>
              {this.userRoleChanges
                .map(values =>
                  values.filter(
                    v =>
                      v.username === user.username &&
                      v.permission === permission
                  )
                )
                .filter(values => Boolean(values.length))
                .map(values => values[0].checked)
                .startWith(user.roles.indexOf(permission) !== -1)
                .map(checked => (
                  <input
                    type="checkbox"
                    checked={checked}
                    data-username={user.username}
                    data-permission={permission}
                    onChange={this.toggleCheckbox}
                  />
                ))
                .asComponent()}
            </td>
          ))}
        </>
      ))
      .asComponent();
  };

  updateNewBranchName = (event: React.ChangeEvent<HTMLInputElement>) =>
    this.newBranchName.next(event.currentTarget.value);
  toggleCheckbox = (event: React.ChangeEvent<HTMLInputElement>) =>
    this.changeUserRole.next({
      username: event.currentTarget.getAttribute("data-username")!,
      permission: event.currentTarget.getAttribute("data-permission")!,
      checked: event.currentTarget.checked
    });
  saveUser = (event: React.MouseEvent<HTMLButtonElement>) => {
    const username = event.currentTarget.getAttribute("data-username")!;
    this.userRoleChanges
      .take(1)
      .map(roleChanges => roleChanges.filter(c => c.username === username))
      .subscribe(roleChanges =>
        updateUser(username || this.newBranchName.value, {
          addRoles: roleChanges.filter(c => c.checked).map(c => c.permission),
          removeRoles: roleChanges
            .filter(c => !c.checked)
            .map(c => c.permission)
        }).subscribe(() => {
          forceRefreshUsers.next(null);
          this.newBranchName.next("");
        })
      );
  };
}
