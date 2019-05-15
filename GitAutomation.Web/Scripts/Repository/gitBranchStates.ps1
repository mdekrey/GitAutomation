#!/usr/bin/env pwsh

param(
	[string] $checkoutPath
)

Push-Location "$checkoutPath"
$allRefs = (git for-each-ref --format='%(refname:short) {%(objectname)}' refs/remotes | ForEach-Object -Process {
	$r = [regex]::match($_, '^(?<name>.+) \{(?<commit>[0-9a-f]{40})\}')
	return @{ "name" = $r.Groups['name'].Value; "commit" = $r.Groups['commit'].Value }
})
$allRefs
Pop-Location
# TODO - send this as an action
