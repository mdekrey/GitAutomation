#!/usr/bin/env pwsh

param(
	[Hashtable] $remotes,
	[string] $checkoutPath,
	[DateTimeOffset] $startTimestamp
)

function Init-BareRepository
{
	git init --bare | Out-Host
	if ($LastExitCode -eq 0)
	{
		return 0
	}
	return 1
}

function Get-GitStatus ([string] $checkoutPath)
{
	Push-Location "$checkoutPath"
	git status | Out-Host
	$status = $LastExitCode
	Pop-Location
	return $status
}

# Could be in the following states:
# 1. Already cloned 
# 3. Directory exists with no clone with permissions
# 4. Directory exists with no clone without permissions
# 5. No clone

if ((New-Item -ItemType Directory -Force -Path "$checkoutPath").FullName -ne (Get-Item $checkoutPath).FullName)
{
	$checkoutPath | Write-Verbose
	New-Item -ItemType Directory -Force -Path "$checkoutPath" | Write-Verbose
	# The target directory could not be cloned
	return Build-StandardAction "TargetDirectoryNotAccessible" @{ "startTimestamp" = $startTimestamp }
}

Push-Location "$checkoutPath"
if ($(git rev-parse --git-dir) -ne '.')
{
	if ($LastExitCode -eq 0)
	{
		# A higher-up directory is a git repository
		return Build-StandardAction "TargetRepositoryNested" @{ "startTimestamp" = $startTimestamp }
	}

	if ((Get-ChildItem "$checkoutPath" | Measure-Object).Count -ne 0)
	{
		# The working directory is dirty
		return Build-StandardAction "TargetRepositoryDirty" @{ "startTimestamp" = $startTimestamp }
	}

	if ((Init-BareRepository) -ne 0)
	{
		# Couldn't init; I believe one of the following conditions is true:
		# 1. No write permissions
		return Build-StandardAction "TargetRepositoryCouldNotBeInitialized" @{ "startTimestamp" = $startTimestamp }
	}
}


foreach ($remote in $remotes.Keys)
{
	Set-GitPassword "$($remotes[$remote].password)"
	if ((git remote get-url $remote) -ne $remotes[$remote].repository) {
		git remote remove $remote | Out-Host
		git remote add $remote "$($remotes[$remote].repository)" | Out-Host
	}

	git fetch $remote --prune --no-tags | Out-Host
	if ($LastExitCode -ne 0)
	{
		# TODO - bad error code, should not halt, and should specify which remote
		return Build-StandardAction "TargetRepositoryPasswordIncorrect" @{ "startTimestamp" = $startTimestamp }
	}
}
Pop-Location
	
return Build-StandardAction "TargetFetched" @{ "startTimestamp" = $startTimestamp }
