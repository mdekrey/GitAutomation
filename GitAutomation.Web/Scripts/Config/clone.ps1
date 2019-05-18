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

function Clone-RepositoryConfiguration
{
	git clone "$repository" "$checkoutPath" -b "$branchName" | Out-Host
	if ($LastExitCode -eq 0)
	{
		return 0
	}
	return 1
}

$gitParams = Create-GitParams -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName" -checkoutPath "$checkoutPath"
Set-GitEnvironment -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName"

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
	return Build-StandardAction "ConfigurationRepository:DirectoryNotAccessible" @{ "startTimestamp" = $startTimestamp }
}

Push-Location "$checkoutPath"
if ($(git rev-parse --git-dir) -ne '.git')
{
	if ($LastExitCode -eq 0)
	{
		# A higher-up directory is a git repository
		return Build-StandardAction "ConfigurationRepository:GitNested" @{ "startTimestamp" = $startTimestamp }
	}

	if ((Clone-RepositoryConfiguration) -eq 0)
	{
		return Build-StandardAction "ConfigurationRepository:ReadyToLoad" @{ "startTimestamp" = $startTimestamp }
	}

	# Couldn't clone; one of the following conditions is true:
	# 1. The working directory is dirty
	# 2. The branch doesn't exist in the remote
	# 3. The password is incorrect

	try {
		if ((Get-ChildItem "$checkoutPath" | Measure-Object).Count -ne 0)
		{
			# The working directory is dirty
			Build-StandardAction "ConfigurationRepository:GitCouldNotClone" @{ "startTimestamp" = $startTimestamp }
			exit
		}

		Start-Git $gitParams
		git init | Out-Host
		if ($LastExitCode -ne 0)
		{
			# No permission to initialize
			Build-StandardAction "ConfigurationRepository:GitCouldNotClone" @{ "startTimestamp" = $startTimestamp }
			return
		}
		git remote add origin "$repository" | Out-Host
		End-Git $gitparams
	}
	catch
	{
		# We probably couldn't create the drive
		return Build-StandardAction "ConfigurationRepository:GitCouldNotClone" @{ "startTimestamp" = $startTimestamp }
	}
}
	Pop-Location


Start-Git $gitParams
git remote remove origin | Out-Host
git remote add origin "$repository" | Out-Host

git fetch origin --prune --no-tags | Out-Host
if ($LastExitCode -ne 0)
{
	return Build-StandardAction "ConfigurationRepository:GitPasswordIncorrect" @{ "startTimestamp" = $startTimestamp }
}

git checkout origin/gitauto-config | Out-Host
if ($LastExitCode -ne 0)
{
	# There can be a lot of noise in these, so we just ignore exit codes
	git checkout $(git rev-parse HEAD) | Out-Host
	git branch -D "$branchName" | Out-Host
	git checkout --orphan "$branchName" | Out-Host
	git reset --hard | Out-Host
	git clean -fxd | Out-Host
	# We clean all refs for better gc
	git for-each-ref --format='%(refname:short)' refs/heads | ForEach-Object -Process {git branch -D $_} | Out-Host

	return Build-StandardAction "ConfigurationRepository:GitNoBranch" @{ "startTimestamp" = $startTimestamp }
}
End-Git $gitparams
	
return Build-StandardAction "ConfigurationRepository:ReadyToLoad" @{ "startTimestamp" = $startTimestamp }
