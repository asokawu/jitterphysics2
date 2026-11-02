using System.Globalization;
using System.Text;
using JitterPhysicsTest.Scenes;
using JitterPhysicsTest.Scenes.Vehicle;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterPhysicsTest.Cli;

/// <summary>
/// watch 命令：把场景在控制台里逐帧跑出来。没有 OpenGL 渲染器可用（上游的渲染器只在
/// JitterDemo 里，而它目前只能单精度编译），所以这里用俯视 ASCII 图 + 遥测代替。
/// </summary>
internal static partial class Cli
{
    private static int Watch(string[] args)
    {
        // --speed/--manual 是 watch 自己的选项，先摘出来，免得通用解析器把它们当未知选项。
        List<string> shared = [];
        Real speed = 1;
        bool manual = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--speed" when i + 1 < args.Length:
                    speed = Real.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;

                case "--manual":
                    manual = true;
                    break;

                default:
                    shared.Add(args[i]);
                    break;
            }
        }

        (ScenarioDefinition scenario, SimulationOptions options, _) = ParseRun([.. shared]);

        if (!AllowKnownIssue(scenario, false))
        {
            return 1;
        }

        int steps = options.Steps ?? scenario.DefaultSteps;
        Real timeStep = options.TimeStep ?? scenario.TimeStep;

        (int width, int height) = Viewport();
        AsciiSceneView view = new(width, height);

        try
        {
            SimulationResult result = SimulationRunner.RunInteractive(
                scenario,
                (world, context, step) =>
                {
                    RigidBody actor = MainActor(context);
                    JVector position = actor.Position;
                    JVector forward = JVector.Transform(-JVector.UnitZ, actor.Orientation);

                    view.AddTrailPoint(position.X, position.Z, Mark(step));
                    view.Render(position, forward, Telemetry(scenario, context, actor, step, timeStep));

                    if (manual)
                    {
                        WaitForKey();
                    }
                    else if (speed > 0)
                    {
                        int sleep = (int)(timeStep * 1000 / speed);
                        if (sleep > 0)
                        {
                            Thread.Sleep(sleep);
                        }
                    }
                },
                options with { Steps = steps });

            Clear();
            Console.WriteLine();
            Console.WriteLine($"跑完 {steps} 步：{result.Describe()}");
            return 0;
        }
        catch (WatchAbortedException)
        {
            Console.WriteLine();
            Console.WriteLine("已手动退出。");
            return 0;
        }
    }

    /// <summary>取终端大小；输出被重定向时给一个固定默认值。</summary>
    private static (int Width, int Height) Viewport()
    {
        try
        {
            if (!Console.IsOutputRedirected && !Console.IsInputRedirected)
            {
                return (Math.Max(40, (Console.WindowWidth - 2) & ~1), Math.Max(10, Console.WindowHeight - 8));
            }
        }
        catch (IOException)
        {
            // 拿不到窗口大小就用默认值。
        }

        return (100, 30);
    }

    private static void Clear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // 输出被重定向时 Console.Clear 不可用。
        }
    }

    /// <summary>取场景里唯一（或最后一个）动态刚体作为观察对象。</summary>
    private static RigidBody MainActor(ScenarioContext context)
    {
        RigidBody? actor = null;

        foreach (RigidBody body in context.Bodies)
        {
            if (body.MotionType == MotionType.Dynamic)
            {
                actor = body;
            }
        }

        return actor ?? context.Bodies[^1];
    }

    private static char Mark(int step)
    {
        if (step < RayCastCarScenario.SettleSteps)
        {
            return ',';
        }

        return step < RayCastCarScenario.AfterDriveSteps ? '#' : '*';
    }

    private static string Telemetry(
        ScenarioDefinition scenario, ScenarioContext context, RigidBody actor, int step, Real timeStep)
    {
        StringBuilder text = new();
        text.Append(CultureInfo.InvariantCulture, $"场景 {scenario.Name}  步 {step + 1}  t={step * (double)timeStep:F2}s  轨迹: ,收敛  #全油门  *转向");
        text.AppendLine();

        text.Append(CultureInfo.InvariantCulture,
            $"位置 ({actor.Position.X,7:F2}, {actor.Position.Y,5:F2}, {actor.Position.Z,8:F2})   " +
            $"速度 {actor.Velocity.Length(),6:F2} m/s   悬挂 " +
            $"{(actor.AngularVelocity.Length(),5:F2} rad/s");

        if (context.Tag is RayCastCar car)
        {
            text.Append(CultureInfo.InvariantCulture, "   轮胎: ");
            text.Append(string.Join(" ", car.Wheels.Select(w => w.OnFloor ? "■" : "□")));
            text.Append(CultureInfo.InvariantCulture, $"   压缩量 {car.Wheels[0].SuspensionLength:F3} m   转向 {car.Wheels[0].SteerAngle:F3} rad");
        }

        return text.ToString();
    }

    private static void WaitForKey()
    {
        Console.Write("[按任意键继续，Q 退出] ");

        try
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Q)
            {
                throw new WatchAbortedException();
            }
        }
        catch (InvalidOperationException)
        {
            // 没有可交互的输入（被重定向）：退化成不停顿。
        }
    }

    /// <summary>用户主动退出逐帧模式；不算错误。</summary>
    private sealed class WatchAbortedException : Exception;
}
