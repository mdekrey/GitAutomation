import { Observable, Subscription } from "rxjs";
import { Selection, event as d3event } from "d3-selection";
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

interface NodeDatum extends BranchHierarchy, SimulationNodeDatum {}

const branchTypeX = {
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

    subscription.add(
      target.distinctUntilChanged().subscribe(svg =>
        svg.html(`
        <g data-locator="viewport">
          <g data-locator="links"/>
          <g data-locator="nodes"/>
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
        console.log(JSON.stringify(nodes));

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
            .on("start", dragstarted)
            .on("drag", dragged)
            .on("end", dragended)
        );

        function dragstarted() {
          if (!d3event.active) simulation.alphaTarget(0.3).restart();
          d3event.subject.fx = d3event.subject.x;
          d3event.subject.fy = d3event.subject.y;
        }

        function dragged() {
          d3event.subject.fx = d3event.x;
          d3event.subject.fy = d3event.y;
        }

        function dragended() {
          if (!d3event.active) simulation.alphaTarget(0);
          d3event.subject.fx = null;
          d3event.subject.fy = null;
        }
      })
    );

    const tick = rxEvent(
      {
        target: Observable.of(simulation as any),
        eventName: "tick"
      },
      () => null
    )
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
        tick.map(d => d.nodes),
        node => node.branchName
      )
        .bind({
          selector: `circle`,
          onCreate: target => target.append<SVGCircleElement>("circle"),
          onEnter: target => target.transition().attr("r", 5),
          onExit: target =>
            target
              .transition()
              .attr("r", 0)
              .remove(),
          onEach: target => {
            target
              .attr("cx", node => node.x || null)
              .attr("cy", node => node.y || null);
          }
        })
        .subscribe()
    );

    subscription.add(
      rxData(
        target.map(fnSelect(`[data-locator="links"]`)),
        tick.map(d => d.links),
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
