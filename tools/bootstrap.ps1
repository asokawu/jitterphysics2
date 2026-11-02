<#
.SYNOPSIS
    准备上游引擎源码：把 notgiven688/jitterphysics2 克隆/校验到 engine\jitterphysics2，并停在 engine.version 记录的 commit。

.DESCRIPTION
    engine\ 是被 .gitignore 忽略的目录，任何人拿到本仓库后先跑一次本脚本即可。
    锁定 commit 的原因：测试里有状态哈希，引擎一动哈希就会变，必须知道“测的是哪个版本”。

.EXAMPLE
    tools\bootstrap.ps1
    tools\bootstrap.ps1 -Ref v2.11.0
#>
[CmdletBinding()]
param(
    # 不填则使用 engine.version 里记录的 commit
    [string]$Ref
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$engineDir = Join-Path $root 'engine\jitterphysics2'
$repo = 'https://github.com/notgiven688/jitterphysics2.git'
$versionFile = Join-Path $root 'engine.version'

if (-not $Ref) {
    if (-not (Test-Path $versionFile)) {
        throw "找不到 $versionFile，无法确定引擎版本。"
    }

    $Ref = (Get-Content $versionFile |
        Where-Object { $_ -and -not $_.StartsWith('#') } |
        Select-Object -First 1).Trim()
}

if (-not $Ref) {
    throw 'engine.version 里没有记录 commit。'
}

if (-not (Test-Path (Join-Path $engineDir '.git'))) {
    Write-Host "[bootstrap] 克隆 $repo -> $engineDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $engineDir) | Out-Null
    git clone --quiet $repo $engineDir
    if ($LASTEXITCODE -ne 0) { throw 'git clone 失败。' }
}
else {
    Write-Host '[bootstrap] 更新已有克隆 ...' -ForegroundColor Cyan
    git -C $engineDir fetch --quiet origin
    if ($LASTEXITCODE -ne 0) { throw 'git fetch 失败。' }
}

git -C $engineDir checkout --quiet $Ref
if ($LASTEXITCODE -ne 0) { throw "无法切换到 $Ref。" }

$commit = (git -C $engineDir rev-parse HEAD).Trim()
$short = (git -C $engineDir rev-parse --short HEAD).Trim()
# 只关心源码改动，bin/obj 已经被 .git/info/exclude 排除
$dirty = @(git -C $engineDir status --porcelain)

Write-Host "[bootstrap] 引擎就绪：$short ($commit)" -ForegroundColor Green

if ($dirty.Count -gt 0) {
    Write-Host '[bootstrap] 警告：engine\ 下有本地改动（说明你改过上游源码）：' -ForegroundColor Yellow
    $dirty | Select-Object -First 10 | ForEach-Object { Write-Host "          $_" -ForegroundColor Yellow }
}

Write-Host '[bootstrap] 双精度编译需要 -p:DoublePrecision=true（tools 下其它脚本已内置）。'

& (Join-Path $PSScriptRoot 'apply-patches.ps1')
