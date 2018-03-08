import * as React from "react";

import { overlayButtonBar } from "../utils/overlay";
import { Modal, isNotCancellation } from "../utils/modal";

export const confirmation = new Modal<JSX.Element, boolean>(
  (props, complete, cancel) => (
    <>
      <p>{props}</p>
      <div className={overlayButtonBar}>
        <button onClick={() => complete(true)}>Yes</button>
        <button onClick={cancel} className="secondary">
          No
        </button>
      </div>
    </>
  )
);

export function confirmJsx(message: JSX.Element) {
  return this.confirmation
    .launch(message)
    .filter(isNotCancellation)
    .filter(v => v);
}
