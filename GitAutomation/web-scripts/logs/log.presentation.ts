import { IRxBindProps } from "../utils/presentation/d3-binding";
import { OutputMessage, OutputMessageChannel } from "../api/output-message";

export const logPresentation: IRxBindProps<
  HTMLLIElement,
  OutputMessage,
  HTMLUListElement,
  {}
> = {
  element: "li",
  selector: "li",
  onEach: selection => {
    selection.text(data => JSON.stringify(data));
  }
};
