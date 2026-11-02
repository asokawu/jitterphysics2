<#
.SYNOPSIS
    以双精度运行上游自带的 NUnit 测试（engine\jitterphysics2\src\JitterTests）。

.DESCRIPTION
    上游测试同时支持单/双精度，靠 -p:DoublePrecision=true 切换；双精度下程序集名变成
    Jitter2.Double，测试里的容差常量也会换成更严的一档。

    注意：不要先用单精度构建过同一个工程再直接跑双精度，两个精度的产物会同时留在 bin 里，
    容易让测试发现产生歧义。本脚本固定带 DoublePrecision=true，保持一致。

.EXAMPLE
    tools\test-engine-double.ps1
    tools\test-engine-double.ps1 -Filter 'FullyQualifiedName~Reproducibility'
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    # 透传给 dotnet test 的额外参数
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$TestArgs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$tests = Join-Path $root 'engine\jitterphysics2\src\JitterTests'

if (-not (Test-Path $tests)) {
    throw 'engine\jitterphysics2\src\JitterTests 不存在，请先运行 tools\bootstrap.ps1。'
}

$arguments = @(
    'test', $tests,
    '--configuration', $Configuration,
    '-p:DoublePrecision=true',
    '--nologo'
)

if ($TestArgs) {
    $arguments += $TestArgs
}

Write-Host '[test-engine] 以双精度运行上游测试 ...' -ForegroundColor Cyan
& dotnet @arguments
exit $LASTEXITCODE
