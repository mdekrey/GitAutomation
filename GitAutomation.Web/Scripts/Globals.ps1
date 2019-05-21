

function Create-GitParams (
    [Parameter(Mandatory=$true)] [string] $password,
	[Parameter(Mandatory=$true)] [string] $authorEmail,
	[Parameter(Mandatory=$true)] [string] $authorName,
	[Parameter(Mandatory=$true)] [string] $committerEmail,
	[Parameter(Mandatory=$true)] [string] $committerName,
	[Parameter(Mandatory=$true)] [string] $checkoutPath)
{
	return @{
		"password" = $password;
		"authorEmail" = $authorEmail;
		"authorName" = $authorName;
		"committerEmail" = $committerEmail;
		"committerName" = $committerName;
		"checkoutPath" = $checkoutPath
	}
}


function Set-GitAuthor ([string] $authorEmail, [string] $authorName)
{
	Set-Item -Path Env:GIT_AUTHOR_NAME -Value "$authorName"
	Set-Item -Path Env:GIT_AUTHOR_EMAIL -Value "$authorEmail"
}
function Set-GitCommitter ([string] $committerEmail, [string] $committerName)
{
	Set-Item -Path Env:GIT_COMMITTER_NAME -Value "$committerName"
	Set-Item -Path Env:GIT_COMMITTER_EMAIL -Value "$committerEmail"
}

function Set-GitPassword ([string] $password)
{
	# TODO - password
}

function Set-GitEnvironment ([string] $password, [string] $committerEmail, [string] $committerName, [string] $authorEmail, [string] $authorName)
{
	Set-GitAuthor -authorEmail $authorEmail -authorName $authorName
	Set-GitCommitter -committerEmail $committerEmail -committerName $committerName
	Set-GitPassword $password
}

function Build-StandardAction ([string] $action, [hashtable] $payload = @{})
{
	@{ 
		"action" = $action;
		"payload" = $payload
	} | ConvertTo-Json -Depth 10
}

function Start-Git ([hashtable] $gitparams)
{
	Set-GitEnvironment -password "$($gitparams.password)" -authorEmail "$($gitparams.authorEmail)" -authorName "$($gitparams.authorName)" -committerEmail "$($gitparams.committerEmail)" -committerName "$($gitparams.committerName)"
	Push-Location $checkoutPath
	if ((pwd).Path -ne (Get-Item $checkoutPath).FullName) {
		throw "Could not cd to $checkoutPath"
	}
}

function End-Git ([hashtable] $gitparams)
{
	Pop-Location
}

$verbosepreference='continue'
