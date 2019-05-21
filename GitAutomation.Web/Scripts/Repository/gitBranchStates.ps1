#!/usr/bin/env pwsh

param(
	[string] $checkoutPath,
	[DateTimeOffset] $startTimestamp
)

Push-Location "$checkoutPath"
$allRefs = (git for-each-ref --format='%(refname) {%(objectname)}' refs/heads | ForEach-Object -Process {
	$r = [regex]::match($_, '^refs/heads/(?<name>.+) \{(?<commit>[0-9a-f]{40})\}')
	return @{ "name" = $r.Groups['name'].Value; "commit" = $r.Groups['commit'].Value }
})
Build-StandardAction "TargetRepository:Refs" @{ "startTimestamp" = $startTimestamp; "allRefs" = $allRefs }
Pop-Location
