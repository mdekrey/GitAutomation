#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath,
	[string] $branchName
)

$gitParams = Create-GitParams -password "$password" -userEmail "$userEmail" -userName "$userName" -checkoutPath "$checkoutPath"

$result = With-Git $gitParams {
	git add . | Out-Host
	git commit -m "Update configuration" | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepositoryCouldNotCommit"
	}
	git push origin HEAD:"$branchName" | Out-Host
	if ($LastExitCode -ne 0)
	{
		return Build-StandardAction "ConfigurationRepositoryCouldNotPush"
	}
	git checkout "origin/$branchName"
}
if ($result)
{
	return $result
}

return Build-StandardAction "ConfigurationPushSuccess"
