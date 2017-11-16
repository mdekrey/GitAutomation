import { Observable, Subject, Subscription } from "../utils/rxjs";
import { Selection, select as d3select } from "d3-selection";

import { rxData, fnSelect, bind } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import {
  allUsers,
  allRoles,
  updateUser,
  forceRefreshUsers
} from "../api/basics";
import { IUpdateUserRequestBody } from "../api/update-user";
import { secured } from "../security/security-binding";

const updateUserData = new Subject<
  { userName: string } & IUpdateUserRequestBody
>();
const permissions = allRoles.map(roles => roles.map(({ role }) => role));

export const admin = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./admin.layout.html")))
    .publishReplay(1)
    .refCount()
    .let(secured)
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        subscription.add(
          updateUserData
            .switchMap(({ userName, addRoles, removeRoles }) =>
              updateUser(userName, { addRoles, removeRoles })
            )
            .subscribe(result => forceRefreshUsers.next(null))
        );

        subscription.add(
          rxData(
            body.map(fnSelect(`[data-locator="users"] thead tr`)),
            permissions
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
            allUsers.map(users => users.concat([{ username: "", roles: [] }])),
            data => data.username
          )
            .bind<HTMLTableRowElement>({
              onCreate: target => target.append<HTMLTableRowElement>("tr"),
              selector: "tr",
              onEnter: tr => tr.html(require("./admin.user-row.html")),
              onEach: tr => {
                tr
                  .select(`[data-locator="user-name"]`)
                  .attr(
                    "data-locator",
                    row => (row.username ? "user-name-value" : null)
                  )
                  .html(
                    row =>
                      row.username
                        ? row.username
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
            .switchMap(selection =>
              permissions.map(permissions =>
                bind({
                  target: selection
                    .selectAll<HTMLTableCellElement, any>(
                      `td[data-locator="user-permission"]`
                    )
                    .data(({ username, roles }) =>
                      permissions.map(permission => ({
                        permission,
                        username,
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
                        ({ username, permission }) =>
                          `${username}-has-${permission}`
                      );
                  }
                })
              )
            )
            .subscribe()
        );

        return subscription;
      })
    );
