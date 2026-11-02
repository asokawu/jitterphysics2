<#
.SYNOPSIS
    把引擎更新到新的上游 commit，并提示同步 engine.version。

.DESCRIPTION
    更新后一定要重新跑一遍测试：状态哈希基线、ms/step 都可能变。
    把输出里的 commit 写回 engine.version，测试结果才可复现。

.EXAMPLE
    tools\update-engine.ps1
    tools\update-engine.ps1 -Ref v2.12.0
#>
[CmdletBinding()]
param(
    # 分支、tag 或 commit，默认 main
    [string]$Ref = 'main',
    # 只看不切
    [switch]$WhatIfOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$engineDir = Join-Path $root 'engine\jitterphysics2'
$versionFile = Join-Path $root 'engine.version'

if (-not (Test-Path (Join-Path $engineDir '.git'))) {
    throw 'engine\jitterphysics2 不存在，请先运行 tools\bootstrap.ps1。'
}

$before = (git -C $engineDir rev-parse HEAD).Trim()

git -C $engineDir fetch --quiet origin
if ($LASTEXITCODE -ne 0) { throw 'git fetch 失败。' }

$target = (git -C $engineDir rev-parse --verify --quiet "origin/$Ref" 2>$null)
if (-not $target) {
    $target = (git -C $engineDir rev-parse --verify --quiet $Ref 2>$null)
}
if (-not $target) { throw "解析不了 ref：$Ref" }

Write-Host "[update] $before -> $($target.Trim())"

if ($WhatIfOnly) {
    Write-Host '[update] -WhatIfOnly：没有实际切换。'
    return
}

git -C $engineDir checkout --quiet $Ref
if ($LASTEXITCODE -ne 0) { throw "无法切换到 $Ref。" }

$after = (git -C $engineDir rev-parse HEAD).Trim()

if ($after -eq $before) {
    Write-Host '[update] 已经是最新。' -ForegroundColor Green
    return
}

$lines = @(
    '# 上游引擎锁定的 commit（notgiven688/jitterphysics2）。',
    '# tools\bootstrap.ps1 会按这里记录的 commit 克隆/校验 engine\jitterphysics2。',
    $after
)
[System.IO.File]::WriteAllLines($versionFile, $lines, [System.Text.UTF8Encoding]::new($false))

Write-Host "[update] 已更新 engine.version -> $after" -ForegroundColor Green
Write-Host '[update] 请重新运行 tools\test-engine-double.ps1 与 tools\test-local.ps1。' -ForegroundColor Yellow
