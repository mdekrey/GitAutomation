import { ApiService } from "./ApiService";
import { BranchReserve } from "./BranchReserve";
import { Observable, combineLatest } from "rxjs";
import { map, shareReplay } from "rxjs/operators";

function determineUnreservedBranches(
  reserves: Record<string, BranchReserve>,
  branches: Record<string, string>
) {
  const reservedBranches = Object.keys(reserves).flatMap(reserve =>
    Object.keys(reserves[reserve].IncludedBranches)
  );
  return Object.keys(branches).filter(b => reservedBranches.indexOf(b) === -1);
}

export class UnreservedBranchesService {
  private readonly api: ApiService;
  public readonly unreservedBranches$: Observable<string[]>;

  constructor(api: ApiService) {
    this.api = api;
    this.unreservedBranches$ = combineLatest(api.reserves$, api.branches$).pipe(
      map(([reserves, branches]) =>
        determineUnreservedBranches(reserves, branches)
      ),
      shareReplay(1)
    );
  }
}
