import * as React from "react";
import { Subject } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import { actionQueue, forceRefreshLog } from "../api/basics";
import { LogPresentation } from "./log.presentation";

import { handleError } from "../handle-error";

export class SystemHealth extends ContextComponent {
  private actionQueueRefresh = new Subject<null>();

  render() {
    return (
      <section>
        <h1>Action Queue</h1>
        <a onClick={() => this.actionQueueRefresh.next(null)}>Refresh</a>
        <ul data-locator="action-queue">
          {this.actionQueueRefresh
            .startWith(null)
            .switchMap(() => actionQueue)
            .let(handleError)
            .map(entries => (
              <>
                {entries.map((entry, index) => (
                  <li key={index}>{JSON.stringify(entry)}</li>
                ))}
              </>
            ))
            .asComponent()}
        </ul>

        <h1>Process Loop Logs</h1>
        <a onClick={() => forceRefreshLog.next(null)}>Refresh</a>
        <ul>
          <LogPresentation />
        </ul>
      </section>
    );
  }
}
