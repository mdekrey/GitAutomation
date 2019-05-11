

function Create-GitParams (
    [Parameter(Mandatory=$true)] [string] $password,
	[Parameter(Mandatory=$true)] [string] $userEmail,
	[Parameter(Mandatory=$true)] [string] $userName,
	[Parameter(Mandatory=$true)] [string] $checkoutPath)
{
	return @{
		"password" = $password;
		"userEmail" = $userEmail;
		"userName" = $userName;
		"checkoutPath" = $checkoutPath
	}
}

function Set-GitEnvironment ([string] $password, [string] $userEmail, [string] $userName)
{
	Set-Item -Path Env:GIT_AUTHOR_NAME -Value "$userName"
	Set-Item -Path Env:GIT_AUTHOR_EMAIL -Value "$userEmail"
	# TODO - password
}

function Build-StandardAction ([string] $action, [hashtable] $payload = @{})
{
	@{ 
		"action" = $action;
		"payload" = $payload
	}
}

function With-Git ([hashtable] $gitparams, [scriptblock] $script)
{
	Set-GitEnvironment -password "$($gitparams.password)" -userEmail "$($gitparams.userEmail)" -userName "$($gitparams.userName)"
	Push-Location
	cd "$checkoutPath"
	if ((pwd) -ne $checkoutPath) {
		throw "Could not cd to $checkoutPath"
	}

	$script.Invoke()
	
	Pop-Location

}

$verbosepreference='continue'
