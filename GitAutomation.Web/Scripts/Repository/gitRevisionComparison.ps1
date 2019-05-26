#!/usr/bin/env pwsh

param(
	[string] $checkoutPath,
	[string] $revision
)

Push-Location "$checkoutPath"
$result = @()
(git for-each-ref --format='%(refname) {%(objectname)}' refs/heads | ForEach-Object -Process {
	$r = [regex]::match($_, '^refs/heads/(?<name>.+) \{(?<commit>[0-9a-f]{40})\}')
	$commit = $r.Groups['commit'].Value
	$diff = git rev-list --count --left-right "$($revision)...$($commit)"
	if ($diff -ne $null)
	{
		$r2 = [regex]::match($diff, '^(?<behind>[0-9]+)\s+(?<ahead>[0-9]+)')
		$result += @(@{ "name" = $r.Groups['name'].Value; "commit" = $commit; "behind" = $r2.Groups['behind'].Value -as [int]; "ahead" = $r2.Groups['ahead'].Value -as [int] })
	}
})
ConvertTo-Json -InputObject $result
Pop-Location
