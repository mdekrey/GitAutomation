import React from "react";
import "./TextLine.css";

export function TextLine() {
  return <span className="TextLine_line" />;
}

export function TextParagraph() {
  return (
    <p>
      <span className="TextLine_paragraph" />
      <span className="TextLine_paragraph" />
      <span className="TextLine_paragraph" />
    </p>
  );
}
