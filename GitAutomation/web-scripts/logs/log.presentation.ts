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
      .filter(data => data.channel === OutputMessageChannel.Error)
      .style("font-weight", "bold")
      .text(data => data.message);

    selection
      .filter(data => data.channel === OutputMessageChannel.Out)
      .text(data => data.message);

    selection
      .filter(data => data.channel === OutputMessageChannel.StartInfo)
      .style("font-style", "italic")
      .text(data => data.message);

    selection
      .filter(data => data.channel === OutputMessageChannel.ExitCode)
      .style("color", data => (data.exitCode ? "red" : "initial"))
      .style("margin-bottom", `10px`)
      .text(data => `Exit code: ${data.exitCode}`);
  }
};
