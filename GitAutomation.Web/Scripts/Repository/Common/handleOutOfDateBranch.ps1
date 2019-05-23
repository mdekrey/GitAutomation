#!/usr/bin/env pwsh

param(
	[string] $name,
	[GitAutomation.DomainModels.BranchReserve] $reserve,
	[HashTable] $branchDetails,
	[HashTable] $upstreamReserves
)

# $reserve:
# {
#   "ReserveType": "line",
#   "FlowType": "Automatic",
#   "Status": "OutOfDate",
#   "Upstream": {
#     "line/0.7": {
#       "LastOutput": "f0426dc920641aac551756561c87be34836657d2",
#       "Role": "Source",
#       "Meta": {}
#     }
#   },
#   "IncludedBranches": {
#     "origin/line/0.8": {
#       "LastCommit": "41f99443bbe337ded8f78ca24ad0f6fd05330844",
#       "Meta": {
#         "Role": "Output"
#       }
#     }
#   },
#   "OutputCommit": "0000000000000000000000000000000000000000",
#   "Meta": {}
# }
# 
# $upstreamReserves:
# {
#   "line/0.7": {
#     "ReserveType": "line",
#     "FlowType": "Automatic",
#     "Status": "Stable",
#     "Upstream": {},
#     "IncludedBranches": {
#       "origin/line/0.7": {
#         "LastCommit": "f0426dc920641aac551756561c87be34836657d2",
#         "Meta": {
#           "Role": "Output"
#         }
#       }
#     },
#     "OutputCommit": "f0426dc920641aac551756561c87be34836657d2",
#     "Meta": {}
#   }
# }

# 0. If any upstream is not Stable, exit.
# 1. get Output branch
# 2. get a clone of the repo
# 3. if Output branch is behind one of the Upstream branches...
#   3.a. If flow is automatic, attempt a merge. If it succeeds, go to 4.a, otherwise mark as conflicted and go to 4.b.
#   3.b. If flow is not automatic, mark as needs update, and go to 4.b.
# 4. See previous step for whether to run a or b
#   4.a. Make a conflict branch
#   4.b. Push output and then mark as stable

$unstableBranches = $upstreamReserves.Keys | ? { $upstreamReserves[$_].Status -ne "Stable" }
if ($unstableBranches.Count -ne 0)
{
	"Had upstream reserves in non-stable state. Deferring." | Write-Error
	$unstableBranches | Write-Error
	return
}

$outputBranch = $reserve.IncludedBranches.Keys | ? { $reserve.IncludedBranches[$_].Meta.Role -eq "Output" }
if ($outputBranch.Count -ne 1)
{
	"Exactly one output branch expected. The following branches were marked as Output" | Write-Error
	$outputBranches | Write-Error
	return
}
$outputBranch | Write-Error
