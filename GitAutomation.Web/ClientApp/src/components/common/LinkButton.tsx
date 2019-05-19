import React from "react";
import { Link } from "react-router-dom";
import { ButtonStyle, DisabledStyle } from "./Button";

export function LinkButton(props: PropsOf<Link>) {
  return <ButtonStyle Component={Link} {...props} />;
}

export function DisabledLinkButton({ ...props }: PropsOf<Link>) {
  return <ButtonStyle Component={[Link, DisabledStyle]} {...props} />;
}
