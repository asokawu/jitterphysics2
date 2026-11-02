namespace JitterPhysicsTest.Scenes;

/// <summary>运行一次场景时可以覆盖的参数。</summary>
public sealed record SimulationOptions
{
    /// <summary>步数，null 表示用场景默认值。</summary>
    public int? Steps { get; init; }

    /// <summary>步长（秒），null 表示用场景默认值。</summary>
    public Real? TimeStep { get; init; }

    /// <summary>求解模式，null 表示用场景默认值。</summary>
    public SolveMode? SolveMode { get; init; }

    /// <summary>是否多线程求解。</summary>
    public bool MultiThread { get; init; } = true;

    /// <summary>
    /// 步进结束、统计之前回调一次，参数是按创建顺序排列的刚体。
    /// 用来做场景专属的断言（例如“没有任何球穿过墙”），不用改运行器本身。
    /// </summary>
    public Action<IReadOnlyList<RigidBody>>? Inspect { get; init; }
}
