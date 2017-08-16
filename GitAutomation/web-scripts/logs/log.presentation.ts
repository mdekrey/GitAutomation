import { IRxBindProps } from "../utils/presentation/d3-binding";
import { OutputMessage } from "../api/output-message";

export const logPresentation: IRxBindProps<
  HTMLLIElement,
  OutputMessage,
  HTMLUListElement,
  {}
> = {
  onCreate: target => target.append<HTMLLIElement>("li"),
  selector: "li",
  onEach: selection => {
    selection.text(data => JSON.stringify(data));
  }
};
