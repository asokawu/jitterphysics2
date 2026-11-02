using JitterPhysicsTest.Scenes.Vehicle;

namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>
/// 射线悬挂车（移植自上游 Demo06 的 RayCastCar），跑一条固定的三段输入曲线：
/// 落地收敛 -> 全油门直线 -> 油门+转向。
/// </summary>
/// <remarks>
/// 这个场景的测试价值不在「车能不能开」（那是 demo 的事），而在于它是目前唯一一个
/// 每步都要打射线、且把射线结果直接当力施加的负载：动态树射线、窄相、约束求解
/// 三条路径同时被压住，而且输入是纯函数式的，可复现性可以被哈希验证。
///
/// 车辆对象挂在 <see cref="ScenarioContext.Tag"/> 上而不是场景定义的字段里，
/// 保证同一进程内反复运行不会串味。
/// </remarks>
public static class RayCastCarScenario
{
    /// <summary>落地收敛段步数。车从 y=2 落到悬挂上，弹簧刻意留了弹性（dampingFrac 0.8），需要 ~1.5 s 才停稳。</summary>
    public const int SettleSteps = 150;

    /// <summary>全油门直线段步数。</summary>
    public const int DriveSteps = 300;

    /// <summary>转向段步数。</summary>
    public const int TurnSteps = 250;

    /// <summary>默认总步数。</summary>
    public const int TotalSteps = SettleSteps + DriveSteps + TurnSteps;

    /// <summary>直线段结束时的步数，测试用它切分阶段。</summary>
    public const int AfterDriveSteps = SettleSteps + DriveSteps;

    private const Real Throttle = 1;
    private const Real Steer = (Real)0.6;

    public static ScenarioDefinition Definition { get; } = new()
    {
        Name = "raycast-car",
        Description = "射线悬挂车（移植自 Demo06）：落地收敛 150 步 -> 全油门 300 步 -> 油门+转向 250 步，" +
                      "每步 4 条射线打地面，射线结果直接当悬挂力施加。",
        TimeStep = (Real)0.01,
        DefaultSteps = TotalSteps,
        Build = Build,
        Step = Drive
    };

    private static void Build(World world, ScenarioContext ctx)
    {
        // 与上游 Demo06 相同的求解设置：射线悬挂对子步比较敏感。
        world.SubstepCount = 2;
        world.SolverIterations = (4, 4);

        // 地面开大一点：6 秒全油门能跑很远，地板小了车会开出边界变成自由落体。
        ctx.AddStaticBox(world, new JVector(800, 1, 800), new JVector(0, (Real)(-0.5), 0));

        RayCastCar car = new(world);
        car.Body.Position = new JVector(0, 2, 0);
        car.Body.DeactivationTime = TimeSpan.MaxValue;

        ctx.Track(car.Body);
        ctx.Tag = car;
    }

    private static void Drive(World world, ScenarioContext ctx, int step, Real timeStep)
    {
        RayCastCar car = (RayCastCar)ctx.Tag!;

        (Real throttle, Real steer) = step switch
        {
            < SettleSteps => (0, 0),
            < AfterDriveSteps => (Throttle, 0),
            _ => (Throttle, Steer)
        };

        car.SetInput(throttle, steer);
        car.Step(timeStep);
    }
}
