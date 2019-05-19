import React from "react";
import { Link } from "react-router-dom";
import "./Button";

export function LinkButton({ className = "", ...props }: PropsOf<Link>) {
  return <Link {...props} className={`button ${className}`} />;
}

export function DisabledLinkButton({
  className = "",
  ...props
}: PropsOf<Link>) {
  return (
    <Link
      onClick={e => e.preventDefault()}
      {...props}
      className={`button disabled ${className}`}
    />
  );
}
