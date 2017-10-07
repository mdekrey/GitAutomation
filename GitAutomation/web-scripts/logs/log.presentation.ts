import { IRxBindProps } from "../utils/presentation/d3-binding";
import { OutputMessage, OutputMessageChannel } from "../api/output-message";

export const logPresentation: IRxBindProps<
  HTMLLIElement,
  OutputMessage,
  HTMLUListElement,
  {}
> = {
  onCreate: target => target.append<HTMLLIElement>("li"),
  selector: "li",
  onEach: selection => {
    selection
      .style(
        "font-weight",
        data =>
          data.channel === OutputMessageChannel.Error ? "bold" : "normal"
      )
      .style(
        "font-style",
        data =>
          data.channel === OutputMessageChannel.StartInfo ? "italic" : "normal"
      )
      .style(
        "color",
        data =>
          data.channel === OutputMessageChannel.ExitCode && data.exitCode
            ? "red"
            : "initial"
      )
      .style(
        "margin-bottom",
        data => (data.channel === OutputMessageChannel.ExitCode ? `10px` : null)
      )
      .text(
        data =>
          data.channel === OutputMessageChannel.ExitCode
            ? `Exit code: ${data.exitCode}`
            : data.message
      );
  }
};
