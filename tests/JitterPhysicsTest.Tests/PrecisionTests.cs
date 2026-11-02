using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Tests;

/// <summary>
/// 先确认“我们测的确实是双精度引擎”。这一点如果不成立，后面所有精度相关的结论都没有意义，
/// 所以把它做成最先跑的测试。
/// </summary>
[TestFixture]
public sealed class PrecisionTests
{
    [Test]
    public void EngineIsBuiltAsDoublePrecision()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Precision.IsDoublePrecision, Is.True,
                "引擎必须以 -p:DoublePrecision=true 编译（tools 下的脚本已内置该参数）");

            Assert.That(typeof(World).Assembly.GetName().Name, Is.EqualTo("Jitter2.Double"),
                "双精度构建的程序集名应为 Jitter2.Double");
        });
    }

    [Test]
    public void RealAliasIsDouble()
    {
        Assert.That(sizeof(Real), Is.EqualTo(sizeof(double)),
            "本仓库的 Real 应当与引擎的 Real 同为 double");
    }

    [Test]
    public void DoublePrecisionUsesWiderStructLayouts()
    {
        // 这些常量是引擎按精度硬编码的（见 engine 的 Precision.cs），双精度下应当是更大的那一组。
        Assert.Multiple(() =>
        {
            Assert.That(Precision.ConstraintSizeFull, Is.EqualTo(512));
            Assert.That(Precision.ConstraintSizeSmall, Is.EqualTo(256));
            Assert.That(Precision.RigidBodyDataSize, Is.EqualTo(256));
        });
    }

    [Test]
    public void ScenarioCatalogIsNotEmpty()
    {
        Assert.That(Scenarios.All, Is.Not.Empty, "场景目录不能为空，否则测试等于没跑");
        Assert.That(Scenarios.Runnable, Is.Not.Empty, "至少要有一个默认可运行场景");
    }
}
