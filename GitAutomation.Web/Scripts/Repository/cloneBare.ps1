#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[DateTimeOffset] $startTimestamp
)

function Clone-RepositoryConfiguration
{
	git clone "$repository" "$checkoutPath" --bare | Out-Host
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

$gitParams = Create-GitParams -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName" -checkoutPath "$checkoutPath"
Set-GitEnvironment -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName"

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

if (![System.IO.Directory]::Exists($checkoutPath) -or ((Get-GitStatus($checkoutPath)) -ne 0))
{
	if ((Clone-RepositoryConfiguration) -eq 0)
	{
		return Build-StandardAction "TargetReadyToLoad" @{ "startTimestamp" = $startTimestamp }
	}

	# Couldn't clone; one of the following conditions is true:
	# 1. The working directory is dirty
	# 2. The branch doesn't exist in the remote
	# 3. The password is incorrect

	try {
		if ((Get-ChildItem "$checkoutPath" | Measure-Object).Count -ne 0)
		{
			# The working directory is dirty
			Build-StandardAction "TargetRepositoryCouldNotBeCloned" @{ "startTimestamp" = $startTimestamp }
			exit
		}

		Start-Git $gitParams
		git init | Out-Host
		if ($LastExitCode -ne 0)
		{
			# No permission to initialize
			Build-StandardAction "TargetRepositoryCouldNotBeCloned" @{ "startTimestamp" = $startTimestamp }
			return
		}
		git remote add origin "$repository" | Out-Host
		End-Git $gitparams
	}
	catch
	{
		# We probably couldn't create the drive
		return Build-StandardAction "TargetRepositoryCouldNotBeCloned" @{ "startTimestamp" = $startTimestamp }
	}
}


Start-Git $gitParams
git remote remove origin | Out-Host
git remote add origin "$repository" | Out-Host

git fetch origin --prune --no-tags | Out-Host
if ($LastExitCode -ne 0)
{
	return Build-StandardAction "TargetRepositoryPasswordIncorrect" @{ "startTimestamp" = $startTimestamp }
}

End-Git $gitparams
	
return Build-StandardAction "TargetReadyToFetch" @{ "startTimestamp" = $startTimestamp }
