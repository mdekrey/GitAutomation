import { IRxBindProps } from "../utils/presentation/d3-binding";

export const logPresentation: IRxBindProps<
  HTMLLIElement,
  GitAutomationGQL.IOutputMessage,
  HTMLUListElement,
  {}
> = {
  onCreate: target => target.append<HTMLLIElement>("li"),
  selector: "li",
  onEach: selection => {
    selection
      .style(
        "font-weight",
        data => (data.channel === "Error" ? "bold" : "normal")
      )
      .style(
        "font-style",
        data => (data.channel === "StartInfo" ? "italic" : "normal")
      )
      .style(
        "color",
        data =>
          data.channel === "ExitCode" && data.exitCode ? "red" : "initial"
      )
      .style(
        "margin-bottom",
        data => (data.channel === "ExitCode" ? `10px` : null)
      )
      .text(
        data =>
          data.channel === "ExitCode"
            ? `Exit code: ${data.exitCode}`
            : data.message
      );
  }
};
