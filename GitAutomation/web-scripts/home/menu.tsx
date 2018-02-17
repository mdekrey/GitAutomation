import * as React from "react";

import { ContextComponent } from "../utils/routing-component";
import { signOut } from "../api/basics";
import { Secured } from "../security/security-binding";

export class StandardMenu extends ContextComponent {
  render() {
    return (
      <>
        <a href={this.context.injector.services.routeHrefBuilder("/")}>Home</a>
        <Secured roleNames={["create", "administrate"]}>
          <a
            href={this.context.injector.services.routeHrefBuilder(
              "/new-branch"
            )}
          >
            New Branch
          </a>
        </Secured>
        <Secured roleNames={["administrate"]}>
          <a href={this.context.injector.services.routeHrefBuilder("/admin")}>
            Admin
          </a>
        </Secured>
        <Secured roleNames={["administrate"]}>
          <a
            href={this.context.injector.services.routeHrefBuilder(
              "/auto-wireup"
            )}
          >
            Auto-Wireup
          </a>
        </Secured>
        <a href={this.context.injector.services.routeHrefBuilder("debug")}>
          System Health
        </a>
        <a onClick={this.signOut}>Log Out</a>
      </>
    );
  }

  signOut = () => {
    signOut().subscribe();
    window.location.href = "/";
  };
}
