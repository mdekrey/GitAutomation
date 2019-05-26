#!/usr/bin/env pwsh

param(
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
		return Build-StandardAction "ConfigurationRepository:GitCouldNotCommit" @{ "startTimestamp" = $startTimestamp }
	}
End-Git $gitParams
