using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Tests;

/// <summary>
/// 场景专属的物理断言：不是“哈希变没变”，而是“物理上应该发生什么”。
/// 哈希变了只能说明结果不同，这些断言才能说明结果是不是错的。
/// </summary>
[TestFixture]
public sealed class StabilityTests
{
    [Test]
    public void RagdollsLandOnTheGroundInsteadOfFallingThrough()
    {
        SimulationResult result = SimulationRunner.Run(Scenarios.Require("ragdoll-drop"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFinite, Is.True);
            Assert.That(result.MinHeight, Is.GreaterThan(-1), $"有零件穿地：最低 {result.MinHeight}");
            Assert.That(result.ContactPairCount, Is.GreaterThan(0), "落地后应该存在接触");
            Assert.That(result.ConstraintPairCount, Is.EqualTo(60), "3 个布娃娃 × 20 个约束");
        });
    }

    [Test]
    public void SphereCannonIsBlockedByTheThinWall()
    {
        int passed = 0;

        SimulationResult result = SimulationRunner.Run(Scenarios.Require("sphere-cannon"), new SimulationOptions
        {
            Inspect = bodies =>
            {
                // 场景里第 1 个刚体是地面，其余是球；墙在 x = 0。
                foreach (RigidBody body in bodies.Skip(1))
                {
                    if (body.Position.X > 1)
                    {
                        passed++;
                    }
                }
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFinite, Is.True);
            Assert.That(passed, Is.Zero, "20 m/s 的球应该被 0.1 厚的墙挡住");
            Assert.That(result.MinHeight, Is.GreaterThan(-1), "球不应该掉出地面");
        });
    }

    [Test]
    public void BoxStacksSettleInsteadOfOverflowingTheStack()
    {
        // 回归用例：修复前这两个场景在双精度下第 1 步就在
        // DynamicTree.RestoreHeightBalanceWithoutIncreasingCost 里无限递归，直接栈溢出。
        foreach (string name in new[] { "box-tower", "box-pyramid" })
        {
            ScenarioDefinition scenario = Scenarios.Require(name);
            SimulationResult result = SimulationRunner.Run(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(scenario.HasKnownIssue, Is.False, $"{name} 仍被标记为已知缺陷");
                Assert.That(result.IsFinite, Is.True, $"{name}: 状态出现 NaN/Inf");
                Assert.That(result.MinHeight, Is.GreaterThan(-1), $"{name}: 有箱子穿到地面以下");
                Assert.That(result.MaxSpeed, Is.LessThan(1.0), $"{name}: 堆叠没有稳定下来");
                Assert.That(result.ActiveBodyCount, Is.Zero, $"{name}: 稳定后应当全部休眠");
            });
        }
    }

    [Test]
    public void RunnableScenariosProduceContactsAndStayAwake()
    {
        foreach (ScenarioDefinition scenario in Scenarios.Runnable)
        {
            SimulationResult result = SimulationRunner.Run(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(result.BodyCount, Is.GreaterThan(1), scenario.Name);
                Assert.That(result.ContactPairCount, Is.GreaterThan(0),
                    $"{scenario.Name}: 场景跑完一步接触都没有，说明根本没跑起来");
                Assert.That(result.MillisecondsPerStep, Is.LessThan(1000),
                    $"{scenario.Name}: 单步 {result.MillisecondsPerStep:F1} ms，量级不对");
            });
        }
    }
}
