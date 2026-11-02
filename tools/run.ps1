<#
.SYNOPSIS
    运行命令行工具（src\JitterPhysicsTest.Cli）。

.EXAMPLE
    tools\run.ps1 list
    tools\run.ps1 run ragdoll-drop --steps 1200
    tools\run.ps1 compare sphere-cannon
    tools\run.ps1 run box-tower --force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Scenario,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Rest,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$cli = Join-Path $root 'src\JitterPhysicsTest.Cli\JitterPhysicsTest.Cli.csproj'

$cliArgs = @($Command)
if ($Scenario) { $cliArgs += $Scenario }
if ($Rest) { $cliArgs += $Rest }

& dotnet run --project $cli --configuration $Configuration -- @cliArgs
exit $LASTEXITCODE
