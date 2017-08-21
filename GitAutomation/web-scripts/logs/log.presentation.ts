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
      .select(function(data) {
        if (data.channel === OutputMessageChannel.Error) {
          return this;
        }
        return null;
      })
      .style("font-weight", "bold")
      .text(data => data.message);

    selection
      .select(function(data) {
        if (data.channel === OutputMessageChannel.Out) {
          return this;
        }
        return null;
      })
      .text(data => data.message);

    selection
      .select(function(data) {
        if (data.channel === OutputMessageChannel.StartInfo) {
          return this;
        }
        return null;
      })
      .style("font-style", "italic")
      .text(data => data.message);

    selection
      .select(function(data) {
        if (data.channel === OutputMessageChannel.ExitCode) {
          return this;
        }
        return null;
      })
      .style("color", data => (data.exitCode ? "red" : "initial"))
      .style("margin-bottom", `10px`)
      .text(data => `Exit code: ${data.exitCode}`);
  }
};
