<#
.SYNOPSIS
    编译并运行本仓库自己的测试（tests\JitterPhysicsTest.Tests）。

.DESCRIPTION
    工程本身已经把 DoublePrecision=true 写进 build\JitterPhysicsTest.props，
    所以这里不需要（也不应该）再手动传 -p:DoublePrecision=true。

.EXAMPLE
    tools\test-local.ps1
    tools\test-local.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    # 透传给 dotnet test 的额外参数，例如 --filter
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$TestArgs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'JitterPhysicsTest.slnx'
$tests = Join-Path $root 'tests\JitterPhysicsTest.Tests'

if (-not (Test-Path (Join-Path $root 'engine\jitterphysics2\.git'))) {
    throw '引擎源码不存在，请先运行 tools\bootstrap.ps1。'
}

Write-Host '[test-local] 编译解决方案 ...' -ForegroundColor Cyan
& dotnet build $solution --configuration $Configuration --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw '编译失败。' }

$arguments = @('test', $tests, '--configuration', $Configuration, '--no-build', '--nologo')
if ($TestArgs) {
    $arguments += $TestArgs
}

Write-Host '[test-local] 运行测试 ...' -ForegroundColor Cyan
& dotnet @arguments
exit $LASTEXITCODE
