import { Observable, Subject, Subscription } from "../utils/rxjs";
import { any, equals, flatten, values } from "../utils/ramda";
import { Selection, event as d3event, mouse as d3mouse } from "d3-selection";
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
  rxEvent,
  rxData,
  rxDatum,
  fnSelect
} from "../utils/presentation/d3-binding";

import { BranchGroupWithHierarchy } from "../api/basic-branch";
import { branchTypeColors } from "../style/branch-colors";
import { RoutingNavigate } from "../routing/index";
import { BranchType } from "../api/basic-branch";

import * as branchHierarchyHtml from "./branch-hierarchy.html";

interface NodeDatum extends BranchGroupWithHierarchy, SimulationNodeDatum {
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

export function branchHierarchy({
  target,
  navigate,
  data: hierarchyData
}: {
  target: Observable<Selection<SVGSVGElement, any, any, any>>;
  navigate: RoutingNavigate;
  data: Observable<Record<string, BranchGroupWithHierarchy>>;
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
              branch.directDownstream.map(downstream => ({
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
      .publishReplay(1)
      .refCount();

    const linkForce = forceLink<NodeDatum, NodeLink>([])
      .distance(({ source, target }) => {
        return (target.hierarchyDepth - source.hierarchyDepth) * 30;
      })
      .strength(link => link.linkIntensity);
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
      .force(
        "y",
        forceY<NodeDatum>(branch => branch.hierarchyDepth * 0).strength(0.1)
      );

    subscription.add(
      data.subscribe(({ nodes, links }) => {
        simulation.nodes(nodes);
        linkForce.links(links as NodeLink[]);
        simulation.alpha(0.3).restart();
      })
    );

    const svgSize = target.map(target => target.node()!.getClientRects()[0]);

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

        let currentHover: NodeDatum | undefined = undefined;
        hitbox
          .on("pointermove", function({ width, height }) {
            const x = d3mouse(this)[0] - xOffset,
              y = d3mouse(this)[1] - height / 2;
            const newHover = simulation.find(x, y, 10);
            if (currentHover !== newHover) {
              if (currentHover) {
                currentHover.showLabel = false;
              }
              currentHover = newHover;
              if (currentHover) {
                currentHover.showLabel = true;
              }
              updateDraw.next(null);
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

    const redraw = rxEvent(
      {
        target: Observable.of(simulation as any),
        eventName: "tick"
      },
      () => null
    )
      .merge(updateDraw)
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
          selector: `circle`,
          onCreate: target => target.append<SVGCircleElement>("circle"),
          onEnter: target => {
            target.transition().attr("r", 5);
          },
          onExit: target =>
            target
              .transition()
              .attr("r", 0)
              .remove(),
          onEach: target => {
            target
              .attr("transform", node => `translate(${node.x}, ${node.y})`)
              .attr("fill", node => node.branchColor);
          }
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
              .attr("fill", "white");
            const text = target
              .append<SVGTextElement>("text")
              .attr("data-locator", "foreground")
              .attr("stroke-width", 0)
              .attr("dy", -6)
              .attr("dx", 3)
              .text(node => node.groupName);
            const textNode = text.node();
            if (textNode) {
              const textSize = textNode.getClientRects()[0];
              rect
                .attr("y", -textSize.height - 6)
                .attr("height", textSize.height + 6);
            }
          },
          onEach: target => {
            target.attr("transform", node => `translate(${node.x}, ${node.y})`);
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
            target
              .transition()
              .style("opacity", node => (node.showLabel ? 0.95 : 0));
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

    return subscription;
  });
}
