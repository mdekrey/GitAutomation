import { indexBy, values, intersection } from "../utils/ramda";
import { BranchGroupWithDownstream } from "./types";
import { Observable } from "../utils/rxjs";

interface TreeEntry {
  descendants: Set<string>;
  depth: number;
  ancestors: Set<string>;
}
const newTreeEntry = (): TreeEntry => ({
  descendants: new Set<string>(),
  depth: 0,
  ancestors: new Set<string>()
});
const buildDescendantsTree = <T>(
  items: T[],
  getKey: (entry: T) => string,
  getChildrenKeys: (entry: T) => string[]
) => {
  const lookup = indexBy(getKey, items);
  const result: Record<string, TreeEntry> = {};
  const getOrCreate = (key: string) =>
    (result[key] = result[key] || newTreeEntry());
  const enqueued = new Set<string>(items.map(getKey));
  const addTo = (target: Set<string>) => (entry: string) => target.add(entry);
  while (enqueued.size > 0) {
    const next = Array.from(enqueued)[0];
    enqueued.delete(next);
    const target = getOrCreate(next);
    getChildrenKeys(lookup[next]).forEach(childKey => {
      if (!target.descendants.has(childKey)) {
        addTo(enqueued)(childKey);
      }
      target.descendants.add(childKey);
      const child = getOrCreate(childKey);
      child.descendants.forEach(addTo(target.descendants));
      child.ancestors.add(next);
      target.ancestors.forEach(addTo(child.ancestors));
    });
    const originalDepth = target.depth;
    target.ancestors.forEach(ancestor => {
      enqueued.add(ancestor);
      target.depth = Math.max(target.depth, getOrCreate(ancestor).depth + 1);
    });
    if (target.depth != originalDepth) {
      enqueued.add(next);
      target.descendants.forEach(addTo(enqueued));
    }
  }

  return result;
};

export interface BranchGroupInput {
  groupName: BranchGroupWithDownstream["groupName"];
  branchType: BranchGroupWithDownstream["branchType"];
  directDownstream: BranchGroupWithDownstream["directDownstream"];
}

export interface BranchGroupHierarchy {
  downstream: string[];
  directUpstream: string[];
  upstream: string[];
}

export interface BranchGroupHierarchyDepth extends BranchGroupHierarchy {
  hierarchyDepth: number;
}

export function groupsToHierarchy<T extends BranchGroupInput>(
  groups: Observable<T[]>,
  filter?: (group: T & BranchGroupHierarchy) => boolean
): Observable<Record<string, T & BranchGroupHierarchyDepth>> {
  const firstPass = groups.map(branches => {
    const directUpstreamTree = indexBy(
      b => b.groupName,
      branches.map(b => ({
        groupName: b.groupName,
        upstream: branches
          .filter(other =>
            other.directDownstream.find(d => d.groupName === b.groupName)
          )
          .map(other => other.groupName)
      }))
    );

    const downstreamTree = buildDescendantsTree(
      branches,
      b => b.groupName,
      b => (b ? b.directDownstream.map(v => v.groupName) : [])
    );

    return indexBy(
      g => g.groupName,
      branches.map((branch): T & BranchGroupHierarchyDepth => ({
        ...(branch as any),
        downstream: Array.from(downstreamTree[branch.groupName].descendants),
        upstream: Array.from(downstreamTree[branch.groupName].ancestors),
        directUpstream: directUpstreamTree[branch.groupName].upstream,
        hierarchyDepth: downstreamTree[branch.groupName].depth
      }))
    );
  });

  return filter
    ? firstPass.switchMap(result => {
        const filtered = values(result).filter(filter);
        const filteredNames = filtered.map(g => g.groupName);
        const resultFiltered = filtered.map(group => ({
          ...(group as any),
          directDownstream: intersection(
            filteredNames,
            group.directDownstream.map(b => b.groupName)
          ).map(b => ({
            groupName: b
          }))
        }));
        return groupsToHierarchy(Observable.of(resultFiltered));
      })
    : firstPass;
}
