# JitterPhysicsTest — Jitter Physics 2（双精度）测试工程

针对 **Jitter Physics 2 的双精度构建** 做测试：确定性、稳定性、性能各一套跑法，三处共用同一批场景定义。

- 引擎源码不放在本仓库，放在 `engine/jitterphysics2`（上游 `notgiven688/jitterphysics2` 的 git 克隆，锁定 commit 记录在 `engine.version`）。
- 引擎只有一份源码，单/双精度靠编译期符号 `USE_DOUBLE_PRECISION` 切换：`-p:DoublePrecision=true` 时程序集名变成 `Jitter2.Double`，也就是 NuGet 上的 `Jitter2.Double`。

## 目录结构

```
JitterPhysicsTest/
├─ JitterPhysicsTest.slnx           解决方案（不含引擎工程；引擎以 ProjectReference 引入）
├─ Directory.Build.props            空文件，只为阻止 MSBuild 继续向上层目录找配置
├─ engine.version                   锁定的上游 commit（测试哈希基线的前提）
├─ engine/jitterphysics2/           上游克隆 + 本地补丁（已 .gitignore）
├─ patches/*.patch                  对上游的本地改动，按文件名顺序应用
├─ build/
│  ├─ JitterPhysicsTest.props       各工程显式 Import 的公共设置（net10.0 / 双精度 / 引擎引用）
│  └─ GlobalUsings.cs               共享全局 using，Real = double；非双精度构建直接 #error
├─ src/JitterPhysicsTest.Scenes/    场景库 + 运行器 + 状态哈希（测试、命令行、性能测试共用）
│  ├─ ScenarioDefinition.cs         一个场景 = 名字/步长/步数/求解模式/怎么搭/每步做什么
│  ├─ ScenarioContext.cs            建场景时记录刚体（顺序固定，哈希才可比）+ Tag 挂载点
│  ├─ SimulationRunner.cs           跑一次并收集统计
│  ├─ StateHasher.cs                FNV-1a 64，逐位比较（含 -0.0 / NaN）
│  ├─ Scenarios.cs                  场景目录
│  ├─ Cases/                        具体场景，一个文件一个
│  └─ Vehicle/                      从上游 Demo06 移植的射线悬挂车（RayCastCar + Wheel）
├─ src/JitterPhysicsTest.Cli/       命令行：list / info / run / hash / compare
├─ tests/JitterPhysicsTest.Tests/   NUnit（精度、确定性、稳定性）
├─ bench/JitterPhysicsTest.Bench/   BenchmarkDotNet 单步耗时
└─ tools/*.ps1                      脚本，均为 UTF-8 BOM（Windows PowerShell 5.1 才能正确解析中文）
```

> 为什么用 `build/JitterPhysicsTest.props` 而不是把设置写在 `Directory.Build.props`：
> 引擎是上游克隆，一旦自动继承，我们的东西（全局 using、警告即错误、net10.0）会漏进上游工程里。
> 曾经因为这个把引擎编译崩过（`CS1537: using 别名 "Real" 重复`）。改成显式 Import 之后互不干扰。

## 引擎版本与本地补丁

实测的引擎 = `engine.version` 里的上游 commit **加上** `patches/` 下的补丁。
`tools\bootstrap.ps1` 会克隆/切到那个 commit，然后自动调用 `tools\apply-patches.ps1`；
补丁是幂等的，已经应用过就跳过。

这样「测的是哪个版本」和「我们改了什么」都是可复现、可审查的，
也不会因为 `engine/` 被 gitignore 就把改动弄丢。
当前有一个补丁：`patches\0001-dynamictree-bound-height-balancing.patch`（见下文「已修复的问题」）。

## 上手

```powershell
# 1. 准备引擎源码 + 应用本地补丁（engine/ 被 gitignore，新克隆的仓库先跑一次）
tools\bootstrap.ps1

# 2. 跑本仓库的测试
tools\test-local.ps1

# 3. 以双精度跑上游自带的测试
tools\test-engine-double.ps1

# 4. 命令行看场景
tools\run.ps1 list
tools\run.ps1 run ragdoll-drop --steps 1200
tools\run.ps1 compare sphere-cannon

# 5. 性能
dotnet run -c Release --project bench\JitterPhysicsTest.Bench -- --filter *StepBenchmarks*

# 6. 图形 Demo（看物理表现，不是自动化测试）
tools\run-demo.ps1
```

### 图形 Demo 的两个坑

用上游 `JitterDemo` 时注意：

1. **工作目录**。`glfw3.dll` 是按相对路径 `runtimes\win-x64\native\glfw3.dll` 加载的，而工作目录来自启动它的
   shell。在仓库根执行 `dotnet run --project engine\jitterphysics2\src\JitterDemo` 会报
   `DllNotFoundException (0x8007007E)`。`tools\run-demo.ps1` 先切到输出目录再启动，所以随便在哪个目录执行都行。
2. **JitterDemo 目前只能单精度编译**。`patches\0002-jitterdemo-real-alias-and-raycast-signatures.patch`
   已经补好了前置件（`Real` 别名、`DoublePrecision` → `USE_DOUBLE_PRECISION` 开关、
   `IRayCastable.RayCast` 的 `out Real` 签名），但**双精度下整个工程仍有约 132 处
   `double→float` 收窄错误**，分布在 26 个文件（`Conversion.cs` 48、`Demo31.cs` 44、
   `Playground.cs` 30、车相关的 `Wheel.cs` 14 / `RayCastCar.cs` 12 等）。
   根因是 JitterDemo 通篇按单精度写：渲染层 `System.Numerics.Vector3/Quaternion` 本来就只能是
   float，双精度下每个转换点都要显式收窄。所以「双精度跑 Demo06」不是 5 处改动，是一次系统性的移植。

`RayCastCar` 对应 **`Demo06`**（菜单里是 `Demo 06 - Ray-cast Car`，方向键转向/加速）；
`Demo01` 用的是 `ConstraintCar`，是约束悬挂那条线，不是同一个东西。

`run` 的输出长这样：

```
ragdoll-drop | steps=600 dt=0.010000 solve=Regular threads=on | bodies=31 (dynamic=30, active=30)
  contacts=30 constraints=60 | hash=0x35CC73D4D176D468 | 0.540 ms/step (peak 38.909) | finite=True ...
```

`hash` 只输出一个十六进制哈希，方便跨机器、跨引擎版本比对同一场景的结果。

## 精度是怎么控制的

| 位置 | 做法 |
|------|------|
| 引擎源码 | 一份源码，`src/Jitter2/Precision.cs` 里按 `USE_DOUBLE_PRECISION` 切换 `Real` |
| 构建开关 | `-p:DoublePrecision=true`，产物程序集名 `Jitter2.Double.dll` |
| 本仓库 | `build/JitterPhysicsTest.props` 已经写死这个开关，各工程不用再传 |
| 本仓库的 `Real` | `build/GlobalUsings.cs` 里 `global using Real = System.Double`，非双精度直接编译失败 |

**不要**把单精度和双精度混着构建同一个引擎工程：两个精度的产物会同时留在 `bin` 里，测试发现会有歧义。
要对比两种精度，就分两个构建目录，或者开两个终端分别构建后再跑。

## 已修复的问题

### 双精度下 DynamicTree 高度平衡无限递归（栈溢出）——本仓库已打补丁

`engine/jitterphysics2/src/Jitter2/Collision/DynamicTree/DynamicTree.Balance.cs`
的 `RestoreHeightBalanceWithoutIncreasingCost` 末尾对子树递归再递归，没有深度上限：

```csharp
int subtreeRoot = ApplyRotation(node, selected);
RestoreHeightBalanceWithoutIncreasingCost(node);       // 递归
RefitNode(subtreeRoot);
return RestoreHeightBalanceWithoutIncreasingCost(subtreeRoot);
```

**触发条件（实测）**

- 最小复现：1 个静态地面 + 3×3 共 9 个箱子，`World.Step` **一次**即栈溢出。
- 单精度同一场景正常；`box-tower`（8 层 3×3）、`box-pyramid`（12 层）在双精度下都是第 1 步崩。
- `2.10.0`（8db0c00）与主线顶端（1f2bdb40）都存在，两版之间该文件逐字节相同——不是回归，是一直如此。

**根因**：判定一次旋转是否可接受用的是「合并后表面积不增大」
（`IsAcceptedHeightRotation`）。两个表面积相等的拓扑在两个方向上都会被判为可接受，
于是 `node` 与它被提升的子节点可以互相旋转、回到原状，再旋转……
成本不下降、拓扑也不变，递归就没有终点。

**补丁**：`patches\0001-dynamictree-bound-height-balancing.patch`

- 旋转顺序完全保留（先处理被降下来的节点，再处理新的子树根），只是把递归换成显式栈 + 旋转预算（`MaxHeightBalanceRotations = 64`）。
- 预算耗尽时直接返回，树可能仍有轻微失衡，下一次树更新会继续修——这比无界下降好得多。
- 预算正常情况下永远碰不到；成本中立导致的来回旋转会被它硬性截断。

**验证**

| 项目 | 结果 |
|------|------|
| 最小复现（双精度 9 箱） | 不再崩溃 |
| `box-tower` / `box-pyramid`（双精度） | 600/300 步跑完，72/144 接触，全部刚体休眠，箱体没穿地 |
| 上游测试 @8db0c00 | 双精度 1106/1106、单精度 1099/1099 通过 |
| 上游测试 @1f2bdb40 | 双精度 1607/1607、单精度 1600/1600 通过 |
| 性能 | `ragdoll-drop` 0.54–0.69 ms/step、`sphere-cannon` 0.87–0.99 ms/step，打补丁前后无差异（噪声范围内） |
| 本仓库测试 | 11/11 通过，连跑三轮无抖动 |

`box-tower`/`box-pyramid` 已从缺陷隔离名单移出，并作为回归用例写进
`StabilityTests.BoxStacksSettleInsteadOfOverflowingTheStack`：
断言它们能跑完、不穿地、速度收敛、最终全部休眠。

**上游状态**：`origin/main` 顶端仍是 `1f2bdb40`，尚未包含此修复，可直接提 PR。

## 已知限制

### 上游的部分工具不支持双精度

`engine/jitterphysics2/other/SceneBenchmark` 里有硬编码的 `float`，
加 `-p:DoublePrecision=true` 会编译失败（CS1503）。要跑上游性能基线只能用单精度。

### Regular 求解模式不可复现

`SolveMode.Regular` + 多线程下，同一场景连跑三次哈希各不相同（`ragdoll-drop` 实测 3 个不同值）；
单线程下往往稳定，但引擎不承诺。**只有 `Deterministic` 模式承诺可复现且与线程数无关**，
所有哈希类断言都用它（`tools\run.ps1 compare` 可以直接看到四种配置的对比）。

### 高速穿墙是概率事件（不是双精度问题）

`sphere-cannon` 实测（半径 0.25 的球撞 0.1 厚的墙，单双精度结果完全一致）：

| 初速 | 60 步 | 90 步 |
|------|-------|-------|
| 20 m/s | 0/16 穿墙 | 1/16 穿墙 |
| 30 m/s | 3/16 穿墙 | — |
| 40 m/s | 16/16 穿墙 | — |

所以场景默认只跑 60 步，测试断言「一个都没穿过去」；这是当前引擎的行为基线，不是「引擎保证不穿透」。

## 场景清单

| 场景 | 刚体 | 步长 × 步数 | 测什么 |
|------|------|------------|--------|
| `box-tower` | 73（72 动态） | 1/60 × 600 | 堆叠稳定性、休眠（修复 DynamicTree 无限递归后的回归用例） |
| `box-pyramid` | 79（78 动态） | 1/60 × 300 | 大量同时接触下的流形与收敛 |
| `ragdoll-drop` | 31（30 动态） | 1/100 × 600 | 约束求解器、落地后休眠 |
| `sphere-cannon` | 25（16 动态） | 1/60 × 60 | 高速穿透、推测接触 |
| `raycast-car` | 2（1 动态） | 1/100 × 600 | **每步 4 条射线打地面**、悬挂力、侧滑摩擦、输入曲线 |

### raycast-car：为什么把 demo 的车搬进来

`src/JitterPhysicsTest.Scenes/Vehicle/` 是上游 `JitterDemo/Demos/Car` 的移植，
**参数与算法一字未改**，只做了两件事：把 `float` 换成 `Real`（于是同一份代码能编出两种精度），
删掉渲染那一行。它是当前唯一一个「每步打射线、且把射线结果直接当力施加」的负载——
动态树射线、窄相、约束求解三条路径同时被压住，而输入是纯函数式的，可复现性可以被哈希验证。

三段输入曲线：落地收敛 150 步 → 全油门 300 步 → 油门+转向 250 步。
断言在 `tests/.../RayCastCarTests.cs`：悬挂托住车（原点高度 0.2–2.0、不穿地、静止）、
3 秒至少跑 20 m、转向后车头横向偏转 > 0.15。

**单/双精度交叉验证**（Deterministic、单线程、600 步）：

| | 双精度 | 单精度 | 绝对差 |
|----|--------|--------|--------|
| x | -42.8877 | -42.8799 | 7.8e-3 |
| y | 1.6429 | 1.6196 | 2.3e-2 |
| z | -109.6987 | -109.7267 | 2.8e-2 |
| 车头 forward.X | -0.9046 | -0.9084 | 3.8e-3 |
| 速度 | 30.9003 | 30.9317 | 3.1e-2 |
| 状态哈希 | `0xDCE5653AB2EA0F58` | `0xC3504CFA2052B5F0` | 预期不同 |

6 秒、600 步之后两种精度仍然只差 1e-2 量级（相对 1e-4），说明这套悬挂+摩擦的数值行为
本身是精度无关的；哈希不同则是逐位比较的必然结果。

## 加一个场景

1. 在 `src/JitterPhysicsTest.Scenes/Cases/` 新建一个文件，暴露 `public static ScenarioDefinition Definition { get; }`。
2. 在 `Scenarios.All` 里登记（`All` 包含缺陷复现用例，`Runnable` 是默认可运行的那批）。
3. 想让它进批量测试，就在 `tests/.../StabilityTests.cs` 里补一条**物理断言**（“应该发生什么”），
   光有哈希不能说明结果是对的。需要看具体刚体时用 `SimulationOptions.Inspect`。
4. 命令行和性能测试会自动带上它，不用改别的地方。

## 备注

- 建议把本仓库 `git init` 起来；`engine/` 已在 `.gitignore` 里，用 `tools\bootstrap.ps1` 恢复。
- 改引擎源码做实验是允许的，`tools\bootstrap.ps1` 会提示 `engine\` 下有本地改动。
