#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[string] $branchName
)

function Clone-RepositoryConfiguration
{
	git clone "$repository" "$checkoutPath" -b "$branchName"
	if ($LastExitCode -eq 0)
	{
		cd "$checkoutPath"
		git config user.name "$userName"
		git config user.email "$userEmail"
		return 0
	}
	return 1
}

# potentially global functions
function Set-GitEnvironment ([string] $password, [string] $userEmail, [string] $userName)
{
	Set-Item -Path Env:GIT_AUTHOR_NAME -Value "$userName"
	Set-Item -Path Env:GIT_AUTHOR_EMAIL -Value "$userEmail"
	# TODO - password
}

function Get-GitStatus ([string] $checkoutPath)
{
	Push-Location
	cd "$checkoutPath"
	git status
	$LastExitCode
	Pop-Location
}


Set-GitEnvironment -password "$password" -userEmail "$userEmail" -userName "$userName"

# Could be in the following states:
# 1. Already cloned with correct branch
# 2. Already cloned but wrong branch and dirty working directory
# 3. Directory exists with no checkout with permissions
# 4. Directory exists with no checkout without permissions
# 5. No checkout

$hasCheckout = [System.IO.File]::Exists($checkoutPath) -and (Get-GitStatus($checkoutPath) -eq 0)

if (!$hasCheckout)
{
	$cloned = Clone-RepositoryConfiguration
	if ($cloned -eq 0)
	{
		${ "action": "ConfigurationReady" }
		return
	}

	# Couldn't clone; one of the following conditions is true:
	# 1. The working directory is dirty
	# 2. The branch doesn't exist in the remote
	# 3. The password is incorrect

	if ((Get-ChildItem "$checkoutPath" | Measure-Object).Count -ne 0)
	{
		# The working directory is dirty
		${ "action": "ConfigurationRepositoryCouldNotBeCloned" }
		return
	}

	New-Item -ItemType Directory -Force -Path "$checkoutPath"
	if ($LastExitCode -ne 0)
	{
		${ "action": "ConfigurationRepositoryCouldNotBeCloned" }
		return
	}
	Push-Location
	cd "$checkoutPath"
	git init
	if ($LastExitCode -ne 0)
	{
		${ "action": "ConfigurationRepositoryCouldNotBeCloned" }
		return
	}
	git add remote origin "$repository"
	git push origin HEAD:"$branchName" -u
	if ($LastExitCode -ne 0)
	{
		${ "action": "ConfigurationRepositoryPasswordIncorrect" }
		return
	}
	Pop-Location
	
	${ "action": "ConfigurationReady" }
	return
}
