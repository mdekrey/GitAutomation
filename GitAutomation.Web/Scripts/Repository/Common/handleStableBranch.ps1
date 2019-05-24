#!/usr/bin/env pwsh

param(
	[string] $name,
	[GitAutomation.DomainModels.BranchReserve] $reserve,
	[HashTable] $branchDetails,
	[HashTable] $upstreamReserves
)

# reserve:
# {
#   "ReserveType": "line",
#   "FlowType": "Automatic",
#   "Status": "Stable",
#   "Upstream": {},
#   "IncludedBranches": {
#     "origin/line/0.7": {
#       "LastCommit": "0000000000000000000000000000000000000000",
#       "Meta": {
#         "Role": "Output"
#       }
#     }
#   },
#   "OutputCommit": "0000000000000000000000000000000000000000",
#   "Meta": {}
# }

# branchDetails:
# { "origin/line/0.7": "---commit---" }

$changedBranches = $branchDetails.Keys | ? { $branchDetails[$_] -ne $reserve.IncludedBranches[$_].LastCommit }
$changedReserves = $upstreamReserves.Keys | ? { $upstreamReserves[$_].OutputCommit -ne $reserve.Upstream[$_].LastOutput }

if ($changedBranches.Count -or $changedReserves.Count)
{
	$branchChanges = @{}
	$changedBranches | % { $branchChanges[$_] = $branchDetails[$_] }
	
	$reserveChanges = @{}
	$changedReserves | % { $reserveChanges[$_] = $upstreamReserves[$_].OutputCommit }

	if (-not $upstreamReserves.Keys.Count)
	{
		return Build-StandardAction "RepositoryStructure:StabilizeNoUpstream" @{ "Reserve" = $name; "BranchCommits" = $branchChanges }
	} 

	return Build-StandardAction "RepositoryStructure:SetOutOfDate" @{ "Reserve" = $name; "BranchCommits" = $branchChanges; "ReserveOutputCommits" = $reserveChanges }
}
