<#
.SYNOPSIS
    启动上游 JitterDemo（图形窗口），用于肉眼看物理表现。

.DESCRIPTION
    两个必须知道的点，脚本都替你处理了：

    1) 工作目录。glfw3.dll 是按相对路径 "runtimes\win-x64\native\glfw3.dll" 加载的，
       而工作目录来自启动它的 shell。直接在仓库根执行
       `dotnet run --project engine\jitterphysics2\src\JitterDemo` 会报
       DllNotFoundException（0x8007007E）。本脚本先切到输出目录再启动。
    2) 精度。JitterDemo 目前只能单精度编译：Demo20/21/25 的
       IRayCastable.RayCast 签名写死了 out float，双精度下接口要求 out double。

    RayCastCar 对应的是 Demo06（菜单里显示 "Demo 06 - Ray-cast Car"），
    Demo01 用的是 ConstraintCar，不是同一个东西。

.EXAMPLE
    tools\run-demo.ps1
    tools\run-demo.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$NoBuild,

    # 预留开关：等 JitterDemo 适配双精度后才有意义
    [switch]$Double
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$demoDir = Join-Path $root 'engine\jitterphysics2\src\JitterDemo'
$project = Join-Path $demoDir 'JitterDemo.csproj'

if (-not (Test-Path $project)) {
    throw 'engine\jitterphysics2\src\JitterDemo 不存在，请先运行 tools\bootstrap.ps1。'
}

if ($Double) {
    Write-Warning 'JitterDemo 的双精度移植尚未完成（patches\0002 只补了 Real 别名与 RayCast 签名，' `
        '双精度下仍有约 132 处 double->float 收窄错误），已忽略 -Double。'
}

if (-not $NoBuild) {
    # 显式 false：别让上一次的双精度构建产物把这里绊倒
    & dotnet build $project --configuration $Configuration -p:DoublePrecision=false --nologo -v:q
    if ($LASTEXITCODE -ne 0) { throw 'JitterDemo 编译失败。' }
}

$outDir = Join-Path $demoDir "bin\$Configuration\net10.0"
$exe = Join-Path $outDir 'JitterDemo.exe'

if (-not (Test-Path $exe)) {
    throw "找不到 $exe，先去掉 -NoBuild 跑一次。"
}

Write-Host '[demo] 工作目录已切到输出目录（glfw3.dll 按相对路径加载）' -ForegroundColor Cyan
Write-Host '[demo] 窗口左上角 "Jitter2 Demo" 面板 -> 菜单 -> "Demo 06 - Ray-cast Car"' -ForegroundColor Cyan
Write-Host '[demo] 操作：方向键 转向/加速，空格 发射刚体，M 切换多线程求解' -ForegroundColor Cyan
Write-Host ''

Push-Location $outDir
try {
    & $exe
}
finally {
    Pop-Location
}
