﻿#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[string] $branchName,
	[DateTimeOffset] $startTimestamp
)

$gitParams = Create-GitParams -password "$password" -userEmail "$userEmail" -userName "$userName" -checkoutPath "$checkoutPath"

Start-Git $gitParams

# There can be a lot of noise in these, so we just ignore exit codes
git checkout $(git rev-parse HEAD) | Out-Host
git branch -D "$branchName" | Out-Host
git checkout --orphan "$branchName" | Out-Host
git reset --hard | Out-Host
git clean -fxd | Out-Host
git for-each-ref --format='%(refname:short)' refs/heads | ForEach-Object -Process {git branch -D $_} | Out-Host

End-Git $gitParams

# TODO - can/should we do this from here?
# Build-StandardAction "ConfigurationLocalModified" @{ "startTimestamp" = $startTimestamp }
