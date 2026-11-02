using JitterPhysicsTest.Scenes.Cases;

namespace JitterPhysicsTest.Scenes;

/// <summary>
/// 全部测试场景的目录。命令行、NUnit 与 BenchmarkDotNet 都从这里取场景，
/// 保证三处跑的是同一套定义。
/// </summary>
public static class Scenarios
{
    public static IReadOnlyList<ScenarioDefinition> All { get; } =
    [
        BoxTowerScenario.Definition,
        BoxPyramidScenario.Definition,
        RagdollDropScenario.Definition,
        SphereCannonScenario.Definition,
        RayCastCarScenario.Definition
    ];

    public static ScenarioDefinition? Find(string? name) =>
        All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    public static ScenarioDefinition Require(string? name) =>
        Find(name) ?? throw new ArgumentException(
            $"未知场景 “{name}”。可用场景：{string.Join(", ", All.Select(s => s.Name))}", nameof(name));

    /// <summary>默认参与批量运行的场景（排除已知会让引擎崩溃的复现用例）。</summary>
    public static IEnumerable<ScenarioDefinition> Runnable => All.Where(s => !s.HasKnownIssue);
}
