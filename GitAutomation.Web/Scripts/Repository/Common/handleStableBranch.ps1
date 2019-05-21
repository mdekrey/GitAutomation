#!/usr/bin/env pwsh

param(
	[string] $name
)

return Build-StandardAction "RepositoryStructure:SetReserveState" @{ "Reserve" = $name; "State" = "OutOfDate" }
