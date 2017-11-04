import { indexBy } from "../utils/ramda";
import { BranchGroupWithHierarchy } from "./basic-branch";
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

export const groupsToHierarchy = (
  groups: Observable<BranchGroupWithDownstream[]>
): Observable<Record<string, BranchGroupWithHierarchy>> =>
  groups.map(branches => {
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
      branches.map((branch): BranchGroupWithHierarchy => ({
        branchType: branch.branchType,
        groupName: branch.groupName,
        directDownstream: branch.directDownstream.map(
          downstream => downstream.groupName
        ),
        downstream: Array.from(downstreamTree[branch.groupName].descendants),
        upstream: Array.from(downstreamTree[branch.groupName].ancestors),
        directUpstream: directUpstreamTree[branch.groupName].upstream,
        hierarchyDepth: downstreamTree[branch.groupName].depth
      }))
    );
  });
