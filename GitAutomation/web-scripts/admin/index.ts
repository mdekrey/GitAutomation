import { Observable, Subject, Subscription } from "rxjs";
import { Selection, select as d3select } from "d3-selection";

import {
  rxData,
  rxEvent,
  fnSelect,
  bind
} from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { allUsers, updateUser } from "../api/basics";
import { IUpdateUserRequestBody } from "../api/update-user";

const updateUserData = new Subject<
  { userName: string } & IUpdateUserRequestBody
>();
const freshUserData = new Subject<Record<string, string[]>>();
const userData = allUsers().merge(freshUserData);
const permissions = [
  "read",
  "create",
  "delete",
  "update",
  "approve",
  "administrate"
];

export const admin = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="home">Home</a>

  <h1>Manage Users</h1>

  <table data-locator="users">
    <thead>
      <tr>
        <th>User name</th>
        <th data-locator="actions">Actions</th>
      </tr>
    </thead>
    <tbody>
    </tbody>
  </table>

`)
    )
    .publishReplay(1)
    .refCount()
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        subscription.add(
          updateUserData
            .switchMap(({ userName, addRoles, removeRoles }) =>
              updateUser(userName, { addRoles, removeRoles })
            )
            .subscribe(result => freshUserData.next(result))
        );

        subscription.add(
          rxData(
            body.map(fnSelect(`[data-locator="users"] thead tr`)),
            Observable.of(permissions)
          )
            .bind<HTMLTableHeaderCellElement>({
              onCreate: target =>
                target
                  .insert<HTMLTableHeaderCellElement>(
                    "th",
                    `[data-locator="actions"]`
                  )
                  .attr("data-locator", "permission"),
              selector: `th[data-locator="permission"]`,
              onEach: th => th.text(permission => permission)
            })
            .subscribe()
        );

        subscription.add(
          rxData(
            body.map(fnSelect(`[data-locator="users"] tbody`)),
            userData.map(record =>
              Object.keys(record)
                .map(key => ({
                  userName: key,
                  roles: record[key]
                }))
                .concat([{ userName: "", roles: [] }])
            ),
            data => data.userName
          )
            .bind<HTMLTableRowElement>({
              onCreate: target => target.append<HTMLTableRowElement>("tr"),
              selector: "tr",
              onEnter: tr =>
                tr.html(`
                <th data-locator="user-name"></th>
                <td data-locator="actions">
                  <button data-locator="save-user">Save User</button>
                </td>`),
              onEach: tr => {
                tr
                  .select(`[data-locator="user-name"]`)
                  .attr(
                    "data-locator",
                    row => (row.userName ? "user-name-value" : null)
                  )
                  .html(
                    row =>
                      row.userName
                        ? row.userName
                        : `<input type="text" data-locator="user-name-value" />`
                  );
                tr
                  .selectAll(`[data-locator="save-user"]`)
                  .data(function({ roles }) {
                    return [{ row: d3select(this), roles }];
                  })
                  .on("click", ({ row, roles }) => {
                    const userNameValue = row.select(
                      `[data-locator="user-name-value"]`
                    );
                    const userName =
                      userNameValue.text() || userNameValue.property("value");
                    const roleCheckboxes = row
                      .selectAll<HTMLInputElement, any>(`[type="checkbox"]`)
                      .nodes()
                      .map(checkbox => ({
                        role: checkbox.getAttribute("data-role") as string,
                        hasRole: checkbox.checked
                      }));
                    const addRoles = roleCheckboxes
                      .filter(({ hasRole }) => hasRole)
                      .map(({ role }) => role)
                      .filter(role => roles.indexOf(role) === -1);
                    const removeRoles = roleCheckboxes
                      .filter(({ hasRole }) => !hasRole)
                      .map(({ role }) => role)
                      .filter(role => roles.indexOf(role) !== -1);
                    updateUserData.next({ userName, addRoles, removeRoles });
                  });
              }
            })
            .do(selection =>
              bind({
                target: selection
                  .selectAll<HTMLTableCellElement, any>(
                    `td[data-locator="user-permission"]`
                  )
                  .data(({ userName, roles }) =>
                    permissions.map(permission => ({
                      permission,
                      userName,
                      hasPermission: roles.indexOf(permission) !== -1
                    }))
                  ),
                onCreate: target =>
                  target
                    .insert<HTMLTableCellElement>(
                      "td",
                      `[data-locator="actions"]`
                    )
                    .attr("data-locator", "user-permission"),
                onEnter: target => target.html(`<input type="checkbox"/>`),
                onEach: target => {
                  target
                    .select("input")
                    .property("checked", data => data.hasPermission)
                    .attr("data-role", ({ permission }) => permission)
                    .attr(
                      "data-locator",
                      ({ userName, permission }) =>
                        `${userName}-has-${permission}`
                    );
                }
              })
            )
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="home"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/", replaceCurentHistory: false })
          )
        );

        return subscription;
      })
    );
