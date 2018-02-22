import * as React from "react";
import { style } from "typestyle";
import { getLog } from "../api/basics";
import { neverEver } from "../utils/never";

const logPresentationStyles = {
  exitCodeBlock: style({
    marginBottom: "10px",
    fontStyle: "italic"
  }),
  startInfo: style({
    fontStyle: "italic"
  })
};

export const LogPresentation = () => (
  <>
    {getLog
      .map(logs => (
        <>
          {logs.map(
            (data, index) =>
              data.__typename === "StaticRepositoryActionEntry" ? (
                <li
                  style={{ fontWeight: data.isError ? "bold" : "normal" }}
                  key={index}
                >
                  {data.message}
                </li>
              ) : data.__typename === "ProcessRepositoryActionEntry" ? (
                <li key={index}>
                  <span
                    data-locator="start-info"
                    className={logPresentationStyles.startInfo}
                  >
                    {data.startInfo}
                  </span>
                  <ul data-locator="output">
                    {data.output.map((output, index) => (
                      <li
                        key={index}
                        style={{
                          fontWeight:
                            output.channel === "Error" ? "bold" : "normal"
                        }}
                      >
                        {output.message}
                      </li>
                    ))}
                  </ul>
                  <div
                    style={{
                      display: data.exitCode !== null ? "block" : "none"
                    }}
                    className={logPresentationStyles.exitCodeBlock}
                  >
                    Exit code:
                    <span style={{ color: data.exitCode !== 0 ? "red" : null }}>
                      {data.exitCode}
                    </span>
                  </div>
                </li>
              ) : (
                neverEver(data)
              )
          )}
        </>
      ))
      .asComponent()}
  </>
);
