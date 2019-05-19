import React from "react";
import "./Label.css";

export function Label({
  label,
  target,
  className = "",
  ...props
}: {
  label: React.ReactNode;
  target: React.ReactNode;
} & React.LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label {...props} className={`Label_label ${className}`}>
      <span className={`Label_contents`}>{label}</span>
      <span className={`Label_target`}>{target}</span>
    </label>
  );
}
