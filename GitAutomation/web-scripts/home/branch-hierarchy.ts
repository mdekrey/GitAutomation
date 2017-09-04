import { Observable, Subject, Subscription } from "rxjs";
import { Selection, event as d3event, mouse as d3mouse } from "d3-selection";
import {
  forceCollide,
  forceLink,
  forceSimulation,
  forceManyBody,
  forceCenter,
  forceY,
  SimulationNodeDatum,
  SimulationLinkDatum
} from "d3-force";
import { drag, SubjectPosition } from "d3-drag";
import "d3-transition";
import { flatten } from "ramda";
import {
  rxEvent,
  rxData,
  rxDatum,
  fnSelect
} from "../utils/presentation/d3-binding";

import { allBranchesHierarchy } from "../api/basics";
import { BranchHierarchy } from "../api/branch-hierarchy";
import { branchTypeColors } from "../style/branch-colors";
import { BranchType } from "../api/basic-branch";

interface NodeDatum extends BranchHierarchy, SimulationNodeDatum {
  showLabel?: boolean;
}

const branchTypeX: Record<BranchType, number> = {
  ServiceLine: 0,
  Hotfix: -40,
  Infrastructure: 40,
  Feature: 80,
  Integration: 120,
  ReleaseCandidate: 160
};

export function branchHierarchy({
  target
}: {
  target: Observable<Selection<SVGSVGElement, any, any, any>>;
}) {
  return Observable.create(() => {
    const subscription = new Subscription();
    const updateDraw = new Subject<null>();

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
          x: branchTypeX[branch.branchType],
          y: index * 5
        }));

        const links = flatten<SimulationLinkDatum<NodeDatum>>(
          allBranches.map((branch, source) =>
            branch.downstreamBranches.map(downstream => ({
              source,
              target: nodes.find(branch => branch.branchName === downstream)!
            }))
          )
        );

        return { nodes, links };
      })
      .publish()
      .refCount();

    const linkForce = forceLink<NodeDatum, SimulationLinkDatum<NodeDatum>>([])
      .distance(40)
      .strength(1);
    const simulation = forceSimulation<NodeDatum>([])
      .force("link", linkForce)
      .force(
        "charge",
        forceManyBody()
          .distanceMax(80)
          .strength(-30)
      )
      .force("collide", forceCollide(10))
      .force("center", forceCenter())
      .force("y", forceY().strength(0.1));

    subscription.add(
      data.subscribe(({ nodes, links }) => {
        simulation.nodes(nodes);
        linkForce.links(links);
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
        hitbox.on("pointermove", function({ width, height }) {
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
        node => node.branchName
      )
        .bind({
          selector: `circle`,
          onCreate: target => target.append<SVGCircleElement>("circle"),
          onEnter: target => {
            target
              .transition()
              .attr("r", 5)
              .attr("fill", node =>
                branchTypeColors[node.branchType][0].toString()
              );
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
        node => node.branchName
      )
        .bind({
          selector: `g`,
          onCreate: target => target.append<SVGGElement>("g"),
          onEnter: target => {
            const rect = target
              .append("rect")
              .attr("data-locator", "background")
              .attr("rx", 2)
              .attr("ry", 2)
              .attr("fill", "transparent");
            const text = target
              .append<SVGTextElement>("text")
              .attr("data-locator", "foreground")
              .attr("fill", "transparent")
              .attr("stroke-width", 0)
              .attr("dy", -7)
              .attr("dx", 3)
              .text(node => node.branchName);
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
              .select(`text[data-locator="foreground"]`)
              .attr(
                "fill",
                node =>
                  node.showLabel
                    ? branchTypeColors[node.branchType][0].toString()
                    : "transparent"
              );
            target
              .select<SVGRectElement>(`rect[data-locator="background"]`)
              .attr("fill", node => (node.showLabel ? "white" : "transparent"))
              .attr("width", function() {
                return (
                  this.parentElement!.querySelector("text")!.getClientRects()[0]
                    .width + 6
                );
              })
              .attr(
                "stroke",
                node =>
                  node.showLabel
                    ? branchTypeColors[node.branchType][0].toString()
                    : "transparent"
              );
          }
        })
        .subscribe()
    );

    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="links"]`)),
        redraw.map(d => d.links),
        links =>
          (links.source as NodeDatum).branchName +
          " to " +
          (links.target as NodeDatum).branchName
      )
        .bind({
          selector: `line`,
          onCreate: target => target.append<SVGLineElement>("line"),
          onEnter: target =>
            target
              .attr("stroke", "rgba(0,0,0,0)")
              .transition()
              .attr("stroke", "rgba(0,0,0,1)"),
          onExit: target =>
            target
              .transition()
              .attr("stroke", "rgba(0,0,0,0)")
              .remove(),
          onEach: target => {
            target
              .attr("x1", link => (link.source as NodeDatum).x || null)
              .attr("y1", link => (link.source as NodeDatum).y || null)
              .attr("x2", link => (link.target as NodeDatum).x || null)
              .attr("y2", link => (link.target as NodeDatum).y || null);
          }
        })
        .subscribe()
    );

    return subscription;
  });
}
