import { IRxBindProps, bind } from "../utils/presentation/d3-binding";
import { Selection } from "d3-selection";
import { BaseType } from "d3-selection";
import { applyStyles } from "../style/style-binding";
import { style } from "typestyle";

const filterType = <
  GElement extends BaseType,
  NewDatum,
  PElement extends BaseType,
  PDatum,
  T
>(
  selection: Selection<GElement, NewDatum, PElement, PDatum>,
  filter: (datum: NewDatum) => T | false
) =>
  (selection.filter(d => Boolean(filter(d))) as any) as Selection<
    GElement,
    T,
    PElement,
    PDatum
  >;

const logPresentationStyles = applyStyles({
  exitCodeBlock: style({
    marginBottom: "10px",
    fontStyle: "italic"
  }),
  startInfo: style({
    fontStyle: "italic"
  })
});

export const logPresentation: IRxBindProps<
  HTMLLIElement,
  GitAutomationGQL.RepositoryActionEntryInterface,
  HTMLUListElement,
  {}
> = {
  onCreate: target => target.append<HTMLLIElement>("li"),
  selector: "li",
  onEach: selection => {
    filterType(
      selection,
      d => d.__typename === "StaticRepositoryActionEntry" && d
    )
      .style("font-weight", data => (data.isError ? "bold" : "normal"))
      .text(data => data.message);

    const processEntries = filterType(
      selection,
      d => d.__typename === "ProcessRepositoryActionEntry" && d
    ).html(require("./process.layout.html"));
    processEntries
      .select(`[data-locator="start-info"]`)
      .text(data => data.startInfo);
    processEntries
      .select(`[data-locator="exit-code"]`)
      .text(data => data.exitCode)
      .style(`color`, data => (data.exitCode !== 0 ? "red" : null));
    processEntries
      .select(`[data-locator="exit-code-block"]`)
      .style(`display`, data => (data.exitCode !== null ? "block" : "none"));
    bind({
      target: processEntries
        .select(`[data-locator="output"]`)
        .selectAll(`li`)
        .data(data => data.output),
      onCreate: elem => elem.append<HTMLLIElement>("li"),
      onEach: li =>
        li
          .style(
            "font-weight",
            data => (data.channel === "Error" ? "bold" : "normal")
          )
          .text(data => data.message)
    });
    logPresentationStyles(selection);
  }
};
