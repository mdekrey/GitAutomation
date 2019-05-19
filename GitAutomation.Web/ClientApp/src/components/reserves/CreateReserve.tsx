import React from "react";
import { TextLine, TextParagraph } from "../loading";
import { Link } from "react-router-dom";
import "./CreateReserve.css";
import { Card, ActionBar } from "../common";

export function CreateReserve({  }: {}) {
  return (
    <>
      <h1>Create Reserve</h1>
      <div className="CreateReserve_reserves">
        <Card className="CreateReserve_reserveCard">
          <h2>
            <TextLine />
          </h2>
          <TextParagraph />

          <ActionBar>
            <Link to={"/create-reserve"} className="button button-margin">
              Select
            </Link>
          </ActionBar>
        </Card>
        <Card className="CreateReserve_reserveCard">
          <h2>
            <TextLine />
          </h2>
          <TextParagraph />

          <ActionBar>
            <Link to={"/create-reserve"} className="button button-margin">
              Select
            </Link>
          </ActionBar>
        </Card>
        <Card className="CreateReserve_reserveCard">
          <h2>
            <TextLine />
          </h2>
          <TextParagraph />

          <ActionBar>
            <Link to={"/create-reserve"} className="button button-margin">
              Select
            </Link>
          </ActionBar>
        </Card>
      </div>
    </>
  );
}
