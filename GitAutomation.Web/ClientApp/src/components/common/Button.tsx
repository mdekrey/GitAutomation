import React from "react";
import "./Button.css";
import { Chainable } from "./Chain";

type Classed = { className?: string };
type Clickable = Pick<React.HTMLAttributes<any>, "onClick">;

export const ButtonStyle = Chainable<Classed>(
  "ButtonStyle",
  ({ className = "" }) => ({
    className: `button ${className}`,
  })
);

export const DisabledStyle = Chainable<Classed & Clickable>(
  "DisabledStyle",
  ({ className = "", onClick }) => ({
    className: `disabled ${className}`,
    onClick: onClick || (e => e.preventDefault()),
  })
);

export function Button(props: React.HTMLAttributes<HTMLButtonElement>) {
  return <ButtonStyle Component={"button"} {...props} />;
}

export function DisabledButton(props: React.HTMLAttributes<HTMLButtonElement>) {
  return <ButtonStyle Component={["button", DisabledStyle]} {...props} />;
}
