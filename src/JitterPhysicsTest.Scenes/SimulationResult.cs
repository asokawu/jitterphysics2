namespace JitterPhysicsTest.Scenes;

/// <summary>一次模拟跑完之后的统计数据。</summary>
public sealed record SimulationResult
{
    public required string Scenario { get; init; }

    public required int Steps { get; init; }

    public required Real TimeStep { get; init; }

    public required SolveMode SolveMode { get; init; }

    public required bool MultiThread { get; init; }

    /// <summary>刚体总数（含静态体）。</summary>
    public required int BodyCount { get; init; }

    public required int DynamicBodyCount { get; init; }

    /// <summary>步进结束时仍处于激活状态的动态刚体数。</summary>
    public required int ActiveBodyCount { get; init; }

    /// <summary>接触对（Arbiter）数量，每个接触被两个刚体各记一次，因此已除以 2。</summary>
    public required int ContactPairCount { get; init; }

    /// <summary>约束对数量，同样已除以 2。</summary>
    public required int ConstraintPairCount { get; init; }

    /// <summary>所有刚体状态的哈希，见 <see cref="StateHasher"/>。</summary>
    public required ulong StateHash { get; init; }

    public required TimeSpan Elapsed { get; init; }

    public required double MillisecondsPerStep { get; init; }

    /// <summary>单步最慢一次的耗时，含 GC / 抖动，用来发现偶发的长帧。</summary>
    public required double PeakMillisecondsPerStep { get; init; }

    public required bool IsFinite { get; init; }

    public required Real MaxSpeed { get; init; }

    public required Real MinHeight { get; init; }

    public string Describe() =>
        $"{Scenario} | steps={Steps} dt={TimeStep:F6} solve={SolveMode} threads={(MultiThread ? "on" : "off")} | " +
        $"bodies={BodyCount} (dynamic={DynamicBodyCount}, active={ActiveBodyCount}) " +
        $"contacts={ContactPairCount} constraints={ConstraintPairCount} | " +
        $"hash=0x{StateHash:X16} | {MillisecondsPerStep:F3} ms/step (peak {PeakMillisecondsPerStep:F3}) | " +
        $"finite={IsFinite} maxSpeed={MaxSpeed:F3} minY={MinHeight:F3}";
}
