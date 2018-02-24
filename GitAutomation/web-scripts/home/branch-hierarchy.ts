import { Observable, Subject, Subscription } from "../utils/rxjs";
import { any, equals, flatten, values } from "../utils/ramda";
import {
  BaseType,
  ValueFn,
  Selection,
  event as d3event,
  mouse as d3mouse,
  select as d3select
} from "d3-selection";
import {
  forceLink,
  forceSimulation,
  forceManyBody,
  forceX,
  forceY,
  SimulationNodeDatum,
  SimulationLinkDatum
} from "d3-force";
import { drag, SubjectPosition } from "d3-drag";
import "d3-transition";
import {
  fnEvent,
  rxData,
  rxDatum,
  fnSelect,
  IBindBaseProps,
  bind
} from "../utils/presentation/d3-binding";
import { v4 as uuid } from "uuid";

import { branchTypeColors } from "../style/branch-colors";
import { RoutingNavigate } from "@woosti/rxjs-router";
import { BranchType } from "../api/basic-branch";

import * as branchHierarchyHtml from "./branch-hierarchy.html";
import { BranchGroupInput, BranchGroupHierarchyDepth } from "../api/hierarchy";
import { hoveredGroupName } from "../branch-name-display";

interface NodeDatum
  extends BranchGroupInput,
    BranchGroupHierarchyDepth,
    SimulationNodeDatum {
  branchColor: string;
  showLabel?: boolean;
}

interface AdditionalLinkData {
  linkIntensity: number;
}

interface NodeLink extends AdditionalLinkData {
  source: NodeDatum;
  target: NodeDatum;
}

const xOffset = 40;

/** Centers the graph vertically. Code adapted from `forceCenter`, which centers
 * horizontally and vertically */
function centerY(y: number = 0) {
  interface Force {
    (): void;
    y(newY: number): this;
    y(): number;
    initialize: (newNodes: SimulationNodeDatum[]) => void;
  }
  let nodes: SimulationNodeDatum[];

  const force: Partial<Force> & (() => void) = function() {
    let i,
      n = nodes.length,
      sY = 0;

    for (i = 0; i < n; ++i) {
      sY += nodes[i].y!;
    }

    sY = sY / n - y;
    for (i = 0; i < n; ++i) {
      nodes[i].y! -= sY;
    }
  };
  force.initialize = (_: NodeDatum[]) => (nodes = _);
  force.y = ((_?: number) =>
    _ !== undefined ? ((y = +_), force) : y) as Force["y"];

  return force as Force;
}

export interface HierarchyStyleEntry {
  bind: IBindBaseProps<any, NodeDatum, any, any>;
  selection: string;
  filter: ValueFn<BaseType, NodeDatum, boolean>;
}

const defaultHierarchyStyle: HierarchyStyleEntry = {
  bind: {
    onCreate: target => target.append<SVGCircleElement>("circle"),
    onEnter: target => target.transition("resize").attr("r", 5),
    onExit: target => {
      target = target.filter(`:not([data-locator="dead-node"])`);
      if (target.nodes().length) {
        target
          .attr("data-locator", "dead-node")
          .transition("resize")
          .duration(500)
          .attr("r", 0)
          .remove();
      }
    },
    onEach: target => target.attr("fill", node => node.branchColor)
  },
  selection: "circle",
  filter: _ => true
};

export const svgFilterHierarchyStyle = (
  filter: string
): Pick<HierarchyStyleEntry, "bind" | "selection"> => ({
  bind: {
    onCreate: target =>
      target
        .append<SVGCircleElement>("circle")
        .attr("data-filter-type", filter),
    onEnter: target => {
      const node = target.node() as Element | null;
      if (node) {
        const svg = node.closest("svg");
        const ids = getIds(d3select(svg!));

        target
          .transition("resize")
          .attr("r", 5)
          .attr("filter", `url(${ids[filter].selector})`);
      }
    },
    onExit: target => {
      target = target.filter(`:not([data-locator="dead-node"])`);
      if (target.nodes().length) {
        target
          .attr("data-locator", "dead-node")
          .transition("resize")
          .duration(500)
          .attr("r", 0)
          .remove();
      }
    },
    onEach: target => target.attr("fill", node => node.branchColor)
  },
  selection: `circle[data-filter-type="${filter}"]`
});

export const highlightedHierarchyStyle = svgFilterHierarchyStyle("innerGlow");

const conflictedHierarchyStyle: HierarchyStyleEntry = {
  ...svgFilterHierarchyStyle("redGlow"),
  filter: v => {
    // TODO - this typing is correct, but why do I need to go through `any`? I
    // think it's because it's not a deep partial.
    const temp = (v as any) as Partial<GitAutomationGQL.IBranchGroupDetails>;
    if (temp.branches && temp.latestBranch) {
      const latestBranch = temp.branches.find(
        b => b.name === temp.latestBranch!.name
      );
      if (latestBranch && latestBranch.badInfo) {
        return Boolean(latestBranch.badInfo);
      }
    }
    return false;
  }
};

export const hoveredGloballyHierarchyStyle: HierarchyStyleEntry = {
  ...svgFilterHierarchyStyle("glow"),
  filter: v => v.groupName == hoveredGroupName.value
};

const defaultHierarchyStyles = [
  hoveredGloballyHierarchyStyle,
  conflictedHierarchyStyle,
  defaultHierarchyStyle
];

export function addDefaultHierarchyStyles(inserted: HierarchyStyleEntry[]) {
  return [
    hoveredGloballyHierarchyStyle,
    ...inserted,
    conflictedHierarchyStyle,
    defaultHierarchyStyle
  ];
}

export function branchHierarchy({
  target,
  navigate,
  data: hierarchyData,
  style = defaultHierarchyStyles
}: {
  target: Observable<Selection<SVGSVGElement, any, any, any>>;
  navigate: RoutingNavigate;
  data: Observable<
    Record<string, BranchGroupInput & BranchGroupHierarchyDepth>
  >;
  style?: HierarchyStyleEntry[];
}) {
  return Observable.create(() => {
    const subscription = new Subscription();
    const updateDraw = new Subject<null>();

    const branchCounter: Partial<Record<BranchType, number>> = {};

    function getBranchColor(branchType: GitAutomationGQL.IBranchGroupTypeEnum) {
      const counter = branchCounter[branchType] || 0;
      branchCounter[branchType] =
        (counter + 1) % branchTypeColors[branchType].length;
      return branchTypeColors[branchType][counter].toString();
    }

    subscription.add(
      target
        .distinctUntilChanged()
        .subscribe(svg => svg.html(branchHierarchyHtml))
    );

    const data = hierarchyData
      .scan(
        ({ nodes: previousNodes }, allBranches) => {
          const nodes = values(allBranches).map((branch, index): NodeDatum => {
            const previous = previousNodes.find(
              node => node.groupName === branch.groupName
            );
            return {
              ...(previous || {}),
              branchColor:
                previous && previous.branchType === branch.branchType
                  ? previous.branchColor
                  : getBranchColor(branch.branchType),
              ...branch
            };
          });

          const links = flatten<
            AdditionalLinkData & SimulationLinkDatum<NodeDatum>
          >(
            values(allBranches).map((branch, source) =>
              branch.directDownstream.map(d => d.groupName).map(downstream => ({
                source,
                target: nodes.find(branch => branch.groupName === downstream)!,
                linkIntensity: any(
                  downstreamBranch =>
                    allBranches[downstreamBranch]!.downstream.indexOf(
                      downstream
                    ) !== -1,
                  branch.downstream
                )
                  ? 0.2
                  : 1
              }))
            )
          );

          return { nodes, links };
        },
        {
          nodes: [] as NodeDatum[],
          links: [] as (AdditionalLinkData & SimulationLinkDatum<NodeDatum>)[]
        }
      )
      .distinctUntilChanged((a, b) => equals(a, b))
      .publishReplay(1)
      .refCount();

    const linkForce = forceLink<NodeDatum, NodeLink>([])
      .distance(({ source, target }) => {
        return (target.hierarchyDepth - source.hierarchyDepth) * 30;
      })
      .strength(link => {
        return (
          link.linkIntensity /
          Math.max(
            1,
            link.source.directDownstream.length - 1,
            link.target.directUpstream.length - 1
          )
        );
      });
    const yForce = forceY<NodeDatum>().strength(0);
    const simulation = forceSimulation<NodeDatum>([])
      .force("link", linkForce)
      .force(
        "charge",
        forceManyBody()
          .distanceMax(80)
          .strength(-100)
      )
      .force(
        "x",
        forceX<NodeDatum>(branch => branch.hierarchyDepth * 40).strength(1)
      )
      .force("y", centerY(0))
      .force("y2", yForce);

    subscription.add(
      data.subscribe(({ nodes, links }) => {
        simulation.nodes(nodes);
        linkForce.links(links as NodeLink[]);
        simulation.alpha(0.3).restart();
      })
    );

    const svgSize = Observable.interval(100)
      .switchMap(() => target.map(target => target.node()!.getClientRects()[0]))
      .distinctUntilChanged(equals);

    subscription.add(
      rxDatum(svgSize)(
        target.map(fnSelect<SVGRectElement>(`[data-locator="hitbox"]`))
      ).subscribe(hitbox => {
        hitbox
          .attr("width", data => data.width)
          .attr("height", data => data.height);

        hitbox.call(
          drag<SVGRectElement, ClientRect>()
            .container(hitbox.node()!)
            .subject(({ width, height }) => {
              return simulation.find(
                d3event.x - xOffset,
                d3event.y - height / 2
              ) as SubjectPosition;
            })
            .clickDistance(2)
            .on("start", function() {
              if (!d3event.active) simulation.alphaTarget(0.3).restart();
              d3event.subject.fx = d3event.subject.x;
              d3event.subject.fy = d3event.subject.y;
            })
            .on("drag", function() {
              d3event.subject.fx = d3event.x;
              d3event.subject.fy = d3event.y;
            })
            .on("end", function() {
              if (!d3event.active) simulation.alphaTarget(0);
              d3event.subject.fx = null;
              d3event.subject.fy = null;
            })
        );

        let currentHover: NodeDatum | undefined | null = null;
        hitbox
          .on("pointermove", function({ width, height }) {
            const x = d3mouse(this)[0] - xOffset,
              y = d3mouse(this)[1] - height / 2;
            const newHover = simulation.find(x, y, 10);
            simulation
              .nodes()
              .forEach(node => (node.showLabel = node === newHover));
            if (currentHover !== newHover) {
              updateDraw.next(null);
              currentHover = newHover;
            }
          })
          .on("click", function({ width, height }) {
            const x = d3mouse(this)[0] - xOffset,
              y = d3mouse(this)[1] - height / 2;
            const clicked = simulation.find(x, y, 10);
            if (clicked) {
              navigate({
                url: "/manage/" + clicked.groupName,
                replaceCurentHistory: false
              });
            }
          });
      })
    );

    const redraw = Observable.of(simulation)
      .let(fnEvent("tick", { toResult: () => null }))
      .merge(updateDraw)
      .merge(hoveredGroupName)
      .withLatestFrom(data, (_, d) => d)
      .publish()
      .refCount();

    subscription.add(
      rxDatum(svgSize)(
        target.map(fnSelect(`[data-locator="viewport"]`))
      ).subscribe(viewport =>
        viewport.attr(
          "transform",
          data => `translate(${xOffset}, ${data.height / 2})`
        )
      )
    );

    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="nodes"]`)),
        redraw.map(d => d.nodes),
        node => node.groupName
      )
        .bind({
          selector: `g`,
          onCreate: target => {
            return target.append<SVGGElement>("g");
          },
          onEach: target => {
            target.attr(
              "transform",
              node =>
                `translate(${(node.x || 0).toFixed(6)}, ${(node.y || 0).toFixed(
                  6
                )})`
            );
          }
        })
        .do(allTarget => {
          style.forEach((styleEntry, idx, all) => {
            const target = allTarget.filter(function(...args) {
              return (// make sure it's not selected by something esle
                !all
                  .slice(0, idx)
                  .find(other => other.filter.call(this, ...args)) &&
                styleEntry.filter.call(this, ...args) );
            });
            target
              .filter(`[data-style-index]:not([data-style-index="${idx}"])`)
              .html("");
            target.attr("data-style-index", idx);
            bind({
              target: target
                .selectAll(styleEntry.selection)
                .data(d => [d], d => (d as any).groupName),
              ...styleEntry.bind
            });
          });
        })
        .subscribe()
    );

    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="labels"]`)),
        redraw.map(d => d.nodes),
        node => node.groupName
      )
        .bind({
          selector: `g`,
          onCreate: target => target.append<SVGGElement>("g"),
          onEnter: target => {
            target.style("opacity", 0);
            const rect = target
              .append("rect")
              .attr("data-locator", "background")
              .attr("rx", 3)
              .attr("ry", 3)
              .attr("x", 6)
              .attr("fill", "white")
              .attr("fill-opacity", 0.83);
            const text = target
              .append<SVGTextElement>("text")
              .attr("data-locator", "foreground")
              .attr("stroke-width", 0)
              .attr("dy", -3)
              .attr("dx", 3)
              .attr("x", 6)
              .text(node => node.groupName);
            const textNode = text.node();
            if (textNode) {
              const textSize = textNode.getClientRects()[0];
              rect
                .attr("y", -textSize.height / 2 - 3)
                .attr("height", textSize.height + 6);
              text.attr("y", textSize.height / 2);
            }
          },
          onEach: target => {
            target.attr(
              "transform",
              node =>
                `translate(${(node.x || 0).toFixed(6)}, ${(node.y || 0).toFixed(
                  6
                )})`
            );
            target
              .select<SVGRectElement>(`rect[data-locator="background"]`)
              .attr("stroke", node => node.branchColor)
              .attr("width", function() {
                return (
                  this.parentElement!.querySelector("text")!.getClientRects()[0]
                    .width + 6
                );
              });
            target
              .select<SVGRectElement>(`text[data-locator="foreground"]`)
              .attr("fill", node => node.branchColor);
          }
        })
        .subscribe()
    );

    type Picked = Pick<NodeDatum, "groupName" | "showLabel">;
    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="labels"]`)),
        redraw
          .map(d => d.nodes)
          .map(nodes =>
            nodes.map(({ groupName, showLabel }) => ({
              groupName,
              showLabel
            }))
          )
          .distinctUntilChanged<Picked[]>(equals),
        node => node.groupName
      )
        .bind({
          selector: `g`,
          onCreate: target => target.append<SVGGElement>("g"),

          onEach: target => {
            target.each(function(this: SVGGElement, datum) {
              d3select(this)
                .transition(`tooltip-${datum.groupName}`)
                .style("opacity", datum.showLabel ? 0.95 : 0);
            });
          }
        })
        .subscribe()
    );

    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="links"]`)),
        redraw.map(d => d.links),
        links =>
          (links.source as NodeDatum).groupName +
          " to " +
          (links.target as NodeDatum).groupName
      )
        .bind({
          selector: `g`,
          onCreate: target => target.append<SVGGElement>("g"),
          onEnter: target => {
            target
              .attr("stroke", "rgba(0,0,0,0)")
              .attr("fill", "rgba(0,0,0,0)");
            target.append(`line`);
            target.append(`path`).attr("d", "M0,0 l-10,3 l0,-6 l10,3");
          },
          onExit: target =>
            target
              .attr("stroke", "rgba(0,0,0,0)")
              .attr("fill", "rgba(0,0,0,0)")
              .remove(),
          onEach: target => {
            target
              .attr("stroke", link => `rgba(0,0,0,${link.linkIntensity / 2})`)
              .attr("fill", link => `rgba(0,0,0,${link.linkIntensity / 2})`);
            target
              .select(`line`)
              .attr("x1", link => (link.source as NodeDatum).x || null)
              .attr("y1", link => (link.source as NodeDatum).y || null)
              .attr("x2", link => (link.target as NodeDatum).x || null)
              .attr("y2", link => (link.target as NodeDatum).y || null);
            target.select(`path`).attr("transform", link => {
              const source = link.source as NodeDatum;
              const target = link.target as NodeDatum;
              const sourceX = source.x!,
                sourceY = source.y!,
                targetX = target.x!,
                targetY = target.y!;
              const angle = Math.atan2(targetY - sourceY, targetX - sourceX);
              const cos = Math.cos(angle);
              const sin = Math.sin(angle);
              const scale = 0.5;
              const matrix = [
                cos * scale,
                sin * scale,
                -sin * scale,
                cos * scale,
                targetX - 3 * cos,
                targetY - 3 * sin
              ];
              return `matrix(${matrix.join(`, `)})`;
            });
          }
        })
        .subscribe()
    );

    subscription.add(
      redraw.switchMap(v => target).subscribe(svg => {
        const viewport = svg
          .select<SVGGElement>(`[data-locator="viewport"]`)
          .node();
        if (viewport) {
          const outerSize = svg.node()!.getClientRects()[0];
          const size = viewport.getClientRects()[0];
          const topOffset = Math.max(0, outerSize.top - size.top);
          // Makes an exponentially increasing "gravity" to hold everything
          // within view of the SVG
          yForce.strength(d =>
            Math.min(1, (Math.abs(d.y!) / (outerSize.height / 1.5)) ** 5)
          );
          svg
            .attr(
              "height",
              Math.max(
                Number(svg.attr("height")),
                Math.ceil(size.height + topOffset)
              )
            )
            .attr(
              "width",
              Math.max(Number(svg.attr("width")), Math.ceil(size.width))
            );
        }
      })
    );

    return subscription;
  }) as Observable<never>;
}

function getIds(selection: Selection<any, any, any, any>) {
  const result: Record<string, { id: string; selector: string }> = {};

  selection.selectAll(`[data-id-as]`).each(function(this: Element) {
    const key = this.getAttribute("data-id-as")!;
    if (!this.id) {
      this.id = uuid();
    }
    result[key] = {
      id: this.id,
      selector: `#${this.id}`
    };
  });

  return result;
}
