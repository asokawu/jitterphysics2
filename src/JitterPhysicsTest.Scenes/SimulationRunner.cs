using System.Diagnostics;

namespace JitterPhysicsTest.Scenes;

/// <summary>把一个 <see cref="ScenarioDefinition"/> 跑起来，并收集统计与状态哈希。</summary>
public static class SimulationRunner
{
    public static SimulationResult Run(ScenarioDefinition scenario, SimulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return RunCore(scenario, options, onStep: null);
    }

    /// <summary>
    /// 逐帧推进，每步结束回调一次，给「想看着它跑」的用法（控制台可视化）用。
    /// 回调参数是世界、建场景时的上下文和步序号；世界仍然只在本次调用内有效。
    /// </summary>
    public static SimulationResult RunInteractive(
        ScenarioDefinition scenario,
        Action<World, ScenarioContext, int> onStep,
        SimulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(onStep);

        return RunCore(scenario, options, onStep);
    }

    private static SimulationResult RunCore(
        ScenarioDefinition scenario,
        SimulationOptions? options,
        Action<World, ScenarioContext, int>? onStep)
    {
        options ??= new SimulationOptions();

        int steps = options.Steps ?? scenario.DefaultSteps;
        Real timeStep = options.TimeStep ?? scenario.TimeStep;
        SolveMode solveMode = options.SolveMode ?? scenario.SolveMode;

        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), steps, "步数必须为正数。");
        }

        using World world = new()
        {
            Gravity = new JVector(0, (Real)(-9.81), 0),
            SolveMode = solveMode
        };

        ScenarioContext context = new();
        scenario.Build(world, context);

        IReadOnlyList<RigidBody> bodies = context.Bodies;

        Stopwatch stopwatch = Stopwatch.StartNew();
        double peakMilliseconds = 0;

        for (int step = 0; step < steps; step++)
        {
            long start = stopwatch.ElapsedTicks;

            // 载具类场景在这一步施加「这一帧」的输入（油门/转向），必须在积分之前。
            scenario.Step?.Invoke(world, context, step, timeStep);

            world.Step(timeStep, options.MultiThread);

            onStep?.Invoke(world, context, step);

            double milliseconds = (stopwatch.ElapsedTicks - start) * 1000.0 / Stopwatch.Frequency;
            if (milliseconds > peakMilliseconds)
            {
                peakMilliseconds = milliseconds;
            }
        }

        stopwatch.Stop();

        options.Inspect?.Invoke(bodies);

        int contactPairs = 0;
        int constraintPairs = 0;
        int dynamicBodies = 0;
        int activeBodies = 0;

        foreach (RigidBody body in bodies)
        {
            contactPairs += body.Contacts.Count;
            constraintPairs += body.Constraints.Count;

            if (body.MotionType != MotionType.Dynamic)
            {
                continue;
            }

            dynamicBodies++;

            if (body.IsActive)
            {
                activeBodies++;
            }
        }

        return new SimulationResult
        {
            Scenario = scenario.Name,
            Steps = steps,
            TimeStep = timeStep,
            SolveMode = solveMode,
            MultiThread = options.MultiThread,
            BodyCount = bodies.Count,
            DynamicBodyCount = dynamicBodies,
            ActiveBodyCount = activeBodies,
            ContactPairCount = contactPairs / 2,
            ConstraintPairCount = constraintPairs / 2,
            StateHash = StateHasher.Compute(bodies),
            Elapsed = stopwatch.Elapsed,
            MillisecondsPerStep = stopwatch.Elapsed.TotalMilliseconds / steps,
            PeakMillisecondsPerStep = peakMilliseconds,
            IsFinite = StateHasher.IsFinite(bodies),
            MaxSpeed = StateHasher.MaxSpeed(bodies),
            MinHeight = StateHasher.MinHeight(bodies)
        };
    }
}
