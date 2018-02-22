import * as React from "react";
import { claims } from "../security/app-access";
import { style } from "typestyle";

const container = style({
  padding: "0.5em"
});

export class Login extends React.PureComponent<{}, never> {
  render() {
    return (
      <div className={container}>
        {claims
          .map(
            claim =>
              claim.claims.length ? (
                <>
                  <h1>Your current claims</h1>
                  <p>
                    If you're seeing this screen, share the below values with
                    your administrator so they can give you access.
                  </p>
                  <ul>
                    {claim.claims.map(claim => (
                      <li>
                        <strong>{claim.type}</strong> &mdash;{" "}
                        <span>{claim.value}</span>
                      </li>
                    ))}
                  </ul>
                </>
              ) : (
                <>
                  <h1>Log In</h1>
                  <p>You aren't currently logged in.</p>
                  <a href="/api/authentication/sign-in">Log In</a>
                </>
              )
          )
          .asComponent()}
      </div>
    );
  }
}
