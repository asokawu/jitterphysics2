<#
.SYNOPSIS
    把 patches\ 下的补丁依次应用到 engine\jitterphysics2。

.DESCRIPTION
    engine.version 锁的是上游 commit，而本地改动以补丁形式保存在 patches\ 里：
    这样「测的是哪个版本 + 我们改了什么」是可复现、可审查的，
    也不会因为 engine\ 被 gitignore 就把改动弄丢。

    tools\bootstrap.ps1 会自动调用本脚本。补丁已应用时会跳过，可重复执行。

.EXAMPLE
    tools\apply-patches.ps1
    tools\apply-patches.ps1 -Check     # 只检查，不应用
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$engineDir = Join-Path $root 'engine\jitterphysics2'
$patchDir = Join-Path $root 'patches'

if (-not (Test-Path (Join-Path $engineDir '.git'))) {
    throw 'engine\jitterphysics2 不存在，请先运行 tools\bootstrap.ps1。'
}

$patches = @(Get-ChildItem (Join-Path $patchDir '*.patch') -ErrorAction SilentlyContinue | Sort-Object Name)

if ($patches.Count -eq 0) {
    Write-Host '[patches] 没有补丁，引擎保持上游原样。'
    return
}

foreach ($patch in $patches) {
    # 已经应用过：反向检查能干净地通过
    git -C $engineDir apply --check --reverse -- $patch.FullName 2>$null | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[patches] 已应用  $($patch.Name)" -ForegroundColor DarkGray
        continue
    }

    if ($Check) {
        Write-Host "[patches] 待应用  $($patch.Name)" -ForegroundColor Yellow
        continue
    }

    git -C $engineDir apply -- $patch.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "应用失败：$($patch.Name)（上游可能已经自带等价修复，请人工确认）"
    }

    Write-Host "[patches] 已应用  $($patch.Name)" -ForegroundColor Green
}

$applied = @(git -C $engineDir diff --stat)
if ($applied.Count -gt 0) {
    Write-Host '[patches] 引擎当前相对上游的本地改动：' -ForegroundColor Cyan
    $applied | ForEach-Object { Write-Host "          $_" -ForegroundColor Cyan }
}
