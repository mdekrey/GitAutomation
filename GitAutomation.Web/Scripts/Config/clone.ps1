﻿#!/usr/bin/env pwsh

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
	git clone "$repository" "$checkoutPath" -b "$branchName" | Out-Host
	if ($LastExitCode -eq 0)
	{
		return 0
	}
	return 1
}

function Get-GitStatus ([string] $checkoutPath)
{
	Push-Location
	cd "$checkoutPath"
	git status | Out-Host
	$status = $LastExitCode
	Pop-Location
	return $status
}

$gitParams = Create-GitParams -password "$password" -userEmail "$userEmail" -userName "$userName" -checkoutPath "$checkoutPath"
Set-GitEnvironment -password "$password" -userEmail "$userEmail" -userName "$userName"

# Could be in the following states:
# 1. Already cloned with correct branch
# 2. Already cloned but wrong branch and dirty working directory
# 3. Directory exists with no checkout with permissions
# 4. Directory exists with no checkout without permissions
# 5. No checkout

if ((New-Item -ItemType Directory -Force -Path "$checkoutPath").FullName -ne (Get-Item $checkoutPath).FullName)
{
	$checkoutPath | Write-Verbose
	New-Item -ItemType Directory -Force -Path "$checkoutPath" | Write-Verbose
	# The target directory could not be cloned
	return Build-StandardAction "ConfigurationDirectoryNotAccessible"
}

if (![System.IO.Directory]::Exists($checkoutPath) -or ((Get-GitStatus($checkoutPath)) -ne 0))
{
	if ((Clone-RepositoryConfiguration) -eq 0)
	{
		return Build-StandardAction "ConfigurationReady"
	}

	# Couldn't clone; one of the following conditions is true:
	# 1. The working directory is dirty
	# 2. The branch doesn't exist in the remote
	# 3. The password is incorrect

	try {
		if ((Get-ChildItem "$checkoutPath" | Measure-Object).Count -ne 0)
		{
			# The working directory is dirty
			Build-StandardAction "ConfigurationRepositoryCouldNotBeCloned"
			exit
		}

		$result = With-Git $gitParams {
			git init | Out-Host
			if ($LastExitCode -ne 0)
			{
				# No permission to initialize
				Build-StandardAction "ConfigurationRepositoryCouldNotBeCloned"
				return
			}
			git remote add origin "$repository" | Out-Host
		}
		if ($result)
		{
			return $result
		}
	}
	catch
	{
		# We probably couldn't create the drive
		return Build-StandardAction "ConfigurationRepositoryCouldNotBeCloned"
	}
}


$result = With-Git $gitParams {
	git remote remove origin | Out-Host
	git remote add origin "$repository" | Out-Host

	git fetch origin --prune --no-tags | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepositoryPasswordIncorrect"
	}

	git checkout origin/gitauto-config | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepositoryNoBranch"
	}
}
if ($result)
{
	return $result
}
	
return Build-StandardAction "ConfigurationReadyToLoad"
