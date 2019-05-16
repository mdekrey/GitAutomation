#!/usr/bin/env pwsh

param(
	[string] $checkoutPath,
	[DateTimeOffset] $startTimestamp
)

Push-Location "$checkoutPath"
$allRefs = (git for-each-ref --format='%(refname:short) {%(objectname)}' refs/heads | ForEach-Object -Process {
	$r = [regex]::match($_, '^heads/(?<name>.+) \{(?<commit>[0-9a-f]{40})\}')
	return @{ "name" = $r.Groups['name'].Value; "commit" = $r.Groups['commit'].Value }
})
Build-StandardAction "TargetRefs" @{ "startTimestamp" = $startTimestamp; "allRefs" = $allRefs | ConvertTo-Json }
Pop-Location
