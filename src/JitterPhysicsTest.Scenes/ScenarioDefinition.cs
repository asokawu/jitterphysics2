namespace JitterPhysicsTest.Scenes;

/// <summary>
/// 一个测试场景的定义：怎么建世界、步进多久、用哪种求解模式。
/// </summary>
public sealed class ScenarioDefinition
{
    /// <summary>场景名（命令行里用它选择场景）。</summary>
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>固定步长（秒）。</summary>
    public Real TimeStep { get; init; } = (Real)1.0 / (Real)60.0;

    /// <summary>默认步进步数。</summary>
    public int DefaultSteps { get; init; } = 600;

    /// <summary>默认求解模式（Deterministic 可跨线程复现，Regular 更快但非确定）。</summary>
    public SolveMode SolveMode { get; init; } = SolveMode.Regular;

    /// <summary>往世界里搭场景；必须只依赖参数，保证每次构建完全一致。</summary>
    public required Action<World, ScenarioContext> Build { get; init; }

    /// <summary>
    /// 每步在 <c>world.Step</c> 之前调用一次，给「需要按时间给输入」的载具类场景用。
    /// 参数依次是：世界、建场景时的上下文（可以拿到自己创建的刚体）、步序号（从 0 开始）、步长。
    /// </summary>
    public Action<World, ScenarioContext, int, Real>? Step { get; init; }

    /// <summary>
    /// 已知会让引擎崩溃/异常的缺陷说明；非 null 表示这个场景是“缺陷复现用例”，
    /// 默认不参与批量运行（命令行要加 --force，NUnit 会标记为跳过）。
    /// </summary>
    public string? KnownIssue { get; init; }

    public bool HasKnownIssue => KnownIssue is not null;

    public override string ToString() => Name;
}
