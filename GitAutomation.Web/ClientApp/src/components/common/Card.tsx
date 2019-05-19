import React from "react";
import "./Card.css";

export function Card({
  children,
  className = "",
}: {
  children?: React.ReactNode;
  className?: string;
}) {
  return <div className={`Card_card ${className}`}>{children}</div>;
}

export function CardContents({ children }: { children?: React.ReactNode }) {
  return <div className="Card_contents">{children}</div>;
}

export function CardActionBar({ children }: { children?: React.ReactNode }) {
  return (
    <div className="Card_action-row">
      <div className="Card_filler" />
      {children}
    </div>
  );
}
