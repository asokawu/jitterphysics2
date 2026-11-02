using BenchmarkDotNet.Attributes;
using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Bench;

/// <summary>
/// 单步耗时基准：每次迭代重建世界并步进 <see cref="StepsPerIteration"/> 步，
/// 测的是「一个场景推进固定步数要多久」，方便双精度改动前后对比。
/// </summary>
[MemoryDiagnoser]
public class StepBenchmarks
{
    [Params("ragdoll-drop", "sphere-cannon", "raycast-car")]
    public string Scenario { get; set; } = "ragdoll-drop";

    [Params(1, 10)]
    public bool MultiThread { get; set; } = true;

    private ScenarioDefinition _scenario = null!;
    private World _world = null!;
    private Real _timeStep;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _scenario = Scenarios.Require(Scenario);
        _timeStep = _scenario.TimeStep;
        _world = BuildWorld();
    }

    [IterationSetup(Target = nameof(Step))]
    public void IterationSetup() => _world = BuildWorld();

    [Benchmark]
    public void Step()
    {
        for (int i = 0; i < _scenario.DefaultSteps; i++)
        {
            _world.Step(_timeStep, MultiThread);
        }
    }

    private World BuildWorld()
    {
        _world?.Dispose();

        World world = new()
        {
            Gravity = new JVector(0, (Real)(-9.81), 0)
        };

        ScenarioContext context = new();
        _scenario.Build(world, context);

        return world;
    }
}
