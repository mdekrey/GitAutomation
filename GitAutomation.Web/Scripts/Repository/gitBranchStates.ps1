#!/usr/bin/env pwsh

param(
	[string] $repository,
	[string] $password,
	[string] $userEmail,
	[string] $userName,
	[string] $checkoutPath
)

$gitParams = Create-GitParams -password "$password" -authorEmail "$userEmail" -authorName "$userName" -committerEmail "$userEmail" -committerName "$userName" -checkoutPath "$checkoutPath"

Start-Git $gitParams
$allRefs = (git for-each-ref --format='%(refname:short) {%(objectname)}' refs/remotes | ForEach-Object -Process {
	$r = [regex]::match($_, '^(?<name>.+) \{(?<commit>[0-9a-f]{40})\}')
	return @{ "name" = $r.Groups['name'].Value; "commit" = $r.Groups['commit'].Value }
})
End-Git
# TODO - send this as an action
