using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Tests;

/// <summary>
/// 确定性测试：同样的场景 + 同样的配置，跑两次必须得到逐位相同的状态哈希。
/// 只有 Deterministic 求解模式承诺这一点；Regular 模式只保证“不炸”。
/// </summary>
[TestFixture]
public sealed class DeterminismTests
{
    private static SimulationOptions Deterministic(bool multiThread) =>
        new() { SolveMode = SolveMode.Deterministic, MultiThread = multiThread };

    [Test]
    public void DeterministicSolverReproducesAcrossRuns()
    {
        foreach (ScenarioDefinition scenario in Scenarios.Runnable)
        {
            ulong first = SimulationRunner.Run(scenario, Deterministic(multiThread: false)).StateHash;
            ulong second = SimulationRunner.Run(scenario, Deterministic(multiThread: false)).StateHash;

            Assert.That(second, Is.EqualTo(first),
                $"{scenario.Name}: 单线程 Deterministic 重复运行哈希不一致");
        }
    }

    [Test]
    public void DeterministicSolverIsIndependentOfThreading()
    {
        foreach (ScenarioDefinition scenario in Scenarios.Runnable)
        {
            ulong single = SimulationRunner.Run(scenario, Deterministic(multiThread: false)).StateHash;
            ulong multi = SimulationRunner.Run(scenario, Deterministic(multiThread: true)).StateHash;

            Assert.That(multi, Is.EqualTo(single),
                $"{scenario.Name}: Deterministic 模式应当与线程数无关（单 {single:X16} / 多 {multi:X16}）");
        }
    }

    [Test]
    public void RegularSolverKeepsStateFinite()
    {
        // Regular 不保证可复现，这里只验证“跑得完、状态不发散”。
        foreach (ScenarioDefinition scenario in Scenarios.Runnable)
        {
            SimulationResult result = SimulationRunner.Run(scenario, new SimulationOptions
            {
                SolveMode = SolveMode.Regular,
                MultiThread = true
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.IsFinite, Is.True, $"{scenario.Name}: 状态出现 NaN/Inf");
                Assert.That(result.MaxSpeed, Is.LessThan(1e4), $"{scenario.Name}: 速度发散 {result.MaxSpeed}");
            });
        }
    }
}
