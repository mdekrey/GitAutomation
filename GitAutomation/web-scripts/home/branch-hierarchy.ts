import { Observable, Subject, Subscription } from "rxjs";
import { Selection, event as d3event, mouse as d3mouse } from "d3-selection";
import {
  forceLink,
  forceSimulation,
  forceManyBody,
  forceCenter,
  forceX,
  forceY,
  SimulationNodeDatum,
  SimulationLinkDatum
} from "d3-force";
import { drag, SubjectPosition } from "d3-drag";
import "d3-transition";
import { equals, flatten } from "ramda";
import {
  rxEvent,
  rxData,
  rxDatum,
  fnSelect
} from "../utils/presentation/d3-binding";

import { allBranchesHierarchy } from "../api/basics";
import { BranchGroup } from "../api/basic-branch";
import { branchTypeColors } from "../style/branch-colors";
import { ICascadingRoutingStrategy } from "../routing/index";
import { BranchType } from "../api/basic-branch";

interface NodeDatum extends BranchGroup, SimulationNodeDatum {
  branchColor: string;
  showLabel?: boolean;
}

export function branchHierarchy({
  target,
  state
}: {
  target: Observable<Selection<SVGSVGElement, any, any, any>>;
  state: ICascadingRoutingStrategy<any>;
}) {
  return Observable.create(() => {
    const subscription = new Subscription();
    const updateDraw = new Subject<null>();

    const branchCounter: Partial<Record<BranchType, number>> = {};

    function getBranchColor(branchType: BranchType) {
      const counter = branchCounter[branchType] || 0;
      branchCounter[branchType] =
        (counter + 1) % branchTypeColors[branchType].length;
      return branchTypeColors[branchType][counter].toString();
    }

    subscription.add(
      target.distinctUntilChanged().subscribe(svg =>
        svg.html(`
        <g data-locator="viewport">
          <g data-locator="links"/>
          <g data-locator="nodes"/>
          <g data-locator="labels"/>
        </g>
        <rect data-locator="hitbox" fill="transparent" />
      `)
      )
    );

    const data = allBranchesHierarchy()
      .map(allBranches => {
        const nodes = allBranches.map((branch, index): NodeDatum => ({
          ...branch,
          branchColor: getBranchColor(branch.branchType)
        }));

        const links = flatten<SimulationLinkDatum<NodeDatum>>(
          allBranches.map((branch, source) =>
            branch.downstreamBranchGroups.map(downstream => ({
              source,
              target: nodes.find(branch => branch.groupName === downstream)!
            }))
          )
        );

        return { nodes, links };
      })
      .delay(2000)
      .publishReplay(1)
      .refCount();

    const linkForce = forceLink<NodeDatum, SimulationLinkDatum<NodeDatum>>([])
      .distance(40)
      .strength(1);
    let hierarchyForceOffset = 0;
    subscription.add(
      data
        .map(({ nodes }) => nodes.map(node => node.hierarchyDepth))
        .map(hierarchyDepths =>
          hierarchyDepths.reduce((prev, next) => Math.max(prev, next), 0)
        )
        .subscribe(maxDepth => (hierarchyForceOffset = -maxDepth / 2))
    );
    const simulation = forceSimulation<NodeDatum>([])
      .force("link", linkForce)
      .force(
        "charge",
        forceManyBody()
          .distanceMax(80)
          .strength(-100)
      )
      .force("center", forceCenter())
      .force(
        "x",
        forceX<NodeDatum>(
          branch => (branch.hierarchyDepth + hierarchyForceOffset) * 40
        ).strength(1)
      )
      .force(
        "y",
        forceY<NodeDatum>(
          branch => (branch.hierarchyDepth + hierarchyForceOffset) * 0
        ).strength(0.1)
      );

    subscription.add(
      data.subscribe(({ nodes, links }) => {
        simulation.nodes(nodes);
        linkForce.links(links);
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
                d3event.x - width / 2,
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
            const x = d3mouse(this)[0] - width / 2,
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
            const x = d3mouse(this)[0] - width / 2,
              y = d3mouse(this)[1] - height / 2;
            const clicked = simulation.find(x, y, 10);
            if (clicked) {
              state.navigate({
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
          data => `translate(${data.width / 2}, ${data.height / 2})`
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
            target
              .transition()
              .attr("r", 5)
              .attr("fill", node => node.branchColor);
          },
          onExit: target =>
            target
              .transition()
              .attr("r", 0)
              .remove(),
          onEach: target => {
            target.attr("transform", node => `translate(${node.x}, ${node.y})`);
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
              .attr("stroke", node => node.branchColor)
              .attr("fill", "white");
            const text = target
              .append<SVGTextElement>("text")
              .attr("data-locator", "foreground")
              .attr("fill", node => node.branchColor)
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
              .attr("width", function() {
                return (
                  this.parentElement!.querySelector("text")!.getClientRects()[0]
                    .width + 6
                );
              });
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
              .attr("fill", "rgba(0,0,0,0)")
              .transition()
              .attr("stroke", "rgba(0,0,0,1)")
              .attr("fill", "rgba(0,0,0,1)");
            target.append(`line`);
            target.append(`path`).attr("d", "M0,0 l-10,3 l0,-6 l10,3");
          },
          onExit: target =>
            target
              .transition()
              .attr("stroke", "rgba(0,0,0,0)")
              .attr("fill", "rgba(0,0,0,0)")
              .remove(),
          onEach: target => {
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
