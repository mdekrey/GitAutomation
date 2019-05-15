#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[string] $branchName,
	[DateTimeOffset] $startTimestamp
)

$gitParams = Create-GitParams -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName" -checkoutPath "$checkoutPath"

Start-Git $gitParams
	git add . | Out-Host
	git commit -m "Update configuration" | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepositoryCouldNotCommit" @{ "startTimestamp" = $startTimestamp }
	}
	git push origin HEAD:"$branchName" | Out-Host
	if ($LastExitCode -ne 0)
	{
		git checkout "origin/$branchName"
		return Build-StandardAction "ConfigurationRepositoryCouldNotPush" @{ "startTimestamp" = $startTimestamp }
	}
	git checkout "origin/$branchName"
End-Git $gitParams

return Build-StandardAction "ConfigurationPushSuccess" @{ "startTimestamp" = $startTimestamp }
