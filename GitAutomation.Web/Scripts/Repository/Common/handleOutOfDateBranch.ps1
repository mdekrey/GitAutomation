﻿#!/usr/bin/env pwsh

param(
	[string] $name,
	[GitAutomation.DomainModels.BranchReserve] $reserve,
	[HashTable] $branchDetails,
	[HashTable] $upstreamReserves,
	[Hashtable] $remotes,
	[string] $checkoutPath,
	[string] $workingPath,
	[string] $workingRemote,
	[string] $userEmail,
	[string] $userName
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

# 1. If any upstream is not Stable, exit.
# 2. get Output branch
# 3. get a clone of the repo
# 4. if Output branch is behind one of the Upstream branches...
#   4.a. If flow is automatic, attempt a merge. If it succeeds, go to 5.a, otherwise mark as conflicted and go to 5.b.
#   4.b. If flow is not automatic, mark as needs update, and go to 5.b.
# 5. See previous step for whether to run a or b
#   5.a. Make a conflict branch
#   5.b. Push output and then mark as stable

$unstableBranches = $upstreamReserves.Keys | ? { $upstreamReserves[$_].Status -ne "Stable" }
if ($unstableBranches.Count -ne 0)
{
	"Had upstream reserves in non-stable state. Deferring." | Write-Verbose
	$unstableBranches | Write-Verbose
	return
}

$outputBranch = $reserve.IncludedBranches.Keys | ? { $reserve.IncludedBranches[$_].Meta.Role -eq "Output" }
if ($outputBranch.Count -ne 1)
{
	"Exactly one output branch expected. The following branches were marked as Output" | Write-Verbose
	$outputBranch | Write-Verbose
	return
}

Push-Location "$workingPath"
Set-GitAuthor -authorEmail $userEmail -authorName $userName
Set-GitCommitter -committerEmail $userEmail -committerName $userName

git clone --shared --origin $workingRemote --no-checkout $checkoutPath . | Write-Verbose
git checkout "$workingRemote/$outputBranch" | Write-Verbose

$finalState = "Stable"
$upstreamNeeded = @()
$push = $False
foreach ($_ in $upstreamReserves.Keys)
{
	$commit = $upstreamReserves[$_].OutputCommit
	$commitsBehind = git rev-list --right-only --count HEAD...$commit
	if ($commitsBehind -ne 0)
	{
		if ($reserve.FlowType -ne "Automatic")
		{
			$finalState = "NeedsUpdate"
			$upstreamNeeded += $_
		}
		else 
		{
			$message = "Auto-merge $($_)"
			git merge -m "$message" $commit | Write-Verbose
			if ($LastExitCode -ne 0)
			{
				git merge --abort
				$finalState = "Conflicted"
				$upstreamNeeded += $_
			}
			else
			{
				$push = $true
			}
		}
	}
}

$branchParts = $outputBranch.Split("/", 2)
$baseRemote = $branchParts[0]
$outputBranchRemoteName = $branchParts[1]

if ($push) {
	Set-GitPassword "$($remotes[$baseRemote].password)"
	git remote add $remote "$($remotes[$remote].repository)" | Write-Verbose
	# git push $remote "HEAD:$outputBranchRemoteName" | Write-Verbose
}

@{ "Conflicts" = $upstreamNeeded; "State" = $finalState; "Push" = $push } | ConvertTo-Json | Write-Verbose

Pop-Location

