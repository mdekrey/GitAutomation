#!/usr/bin/env pwsh

param(
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[string] $branchName,
	[string] $comment,
	[DateTimeOffset] $startTimestamp
)

if ($comment.Length -eq 0) {
	$comment = "Update configuration"
}

$gitParams = Create-GitParams -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName" -checkoutPath "$checkoutPath"

Start-Git $gitParams
	git add . | Out-Host
	git commit -m "$comment" | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepository:GitCouldNotCommit" @{ "startTimestamp" = $startTimestamp } "Failed to commit changes"
	}
End-Git $gitParams
