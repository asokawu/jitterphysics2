using System.Globalization;
using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Cli;

/// <summary>compare 命令与参数解析。</summary>
internal static partial class Cli
{
    /// <summary>
    /// 同一场景在 4 种求解配置下各跑两遍：既看“同配置是否可复现”，也看“跨线程是否一致”。
    /// 只有 Deterministic 模式承诺跨线程一致；Regular 模式的哈希仅作记录。
    /// </summary>
    private static int Compare(string[] args)
    {
        (ScenarioDefinition scenario, SimulationOptions options, bool force) = ParseRun(args);

        if (!AllowKnownIssue(scenario, force))
        {
            return 1;
        }

        Console.WriteLine($"场景 {scenario.Name}，步数 {options.Steps ?? scenario.DefaultSteps}，步长 {options.TimeStep ?? scenario.TimeStep}");
        Console.WriteLine();

        SimulationResult[] detSingle = Repeat(scenario, options, SolveMode.Deterministic, false);
        SimulationResult[] detMulti = Repeat(scenario, options, SolveMode.Deterministic, true);
        SimulationResult[] regSingle = Repeat(scenario, options, SolveMode.Regular, false);
        SimulationResult[] regMulti = Repeat(scenario, options, SolveMode.Regular, true);

        bool ok = true;

        ok &= Report("deterministic 单线程", detSingle, expectReproducible: true);
        ok &= Report("deterministic 多线程", detMulti, expectReproducible: true);
        ok &= Report("regular     单线程", regSingle, expectReproducible: false);
        ok &= Report("regular     多线程", regMulti, expectReproducible: false);

        bool crossThread = detSingle[0].StateHash == detMulti[0].StateHash;
        ok &= crossThread;

        Console.WriteLine();
        Console.WriteLine($"deterministic 跨线程一致：{YesNo(crossThread)}");
        Console.WriteLine();
        Console.WriteLine("明细：");

        foreach (SimulationResult result in new[] { detSingle[0], detMulti[0], regSingle[0], regMulti[0] })
        {
            Console.WriteLine($"  {result.Describe()}");
        }

        return ok ? 0 : 1;
    }

    private static SimulationResult[] Repeat(
        ScenarioDefinition scenario, SimulationOptions options, SolveMode mode, bool multiThread)
    {
        SimulationOptions configuration = options with { SolveMode = mode, MultiThread = multiThread };
        return [SimulationRunner.Run(scenario, configuration), SimulationRunner.Run(scenario, configuration)];
    }

    private static bool Report(string label, SimulationResult[] runs, bool expectReproducible)
    {
        bool reproducible = runs[0].StateHash == runs[1].StateHash;
        string note = expectReproducible ? string.Empty : "（不保证）";

        Console.WriteLine($"{label,-22} hash=0x{runs[0].StateHash:X16}  {runs[0].MillisecondsPerStep,7:F3} ms/step  重复运行{YesNo(reproducible)}{note}");

        return reproducible || !expectReproducible;
    }

    private static string YesNo(bool value) => value ? "一致" : "不一致";

    private static (ScenarioDefinition Scenario, SimulationOptions Options, bool Force) ParseRun(string[] args)
    {
        string name = Require(args, 0);
        SimulationOptions options = new();
        bool force = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--steps":
                    options = options with { Steps = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture) };
                    break;

                case "--dt":
                    options = options with { TimeStep = double.Parse(Next(args, ref i), CultureInfo.InvariantCulture) };
                    break;

                case "--threads":
                    options = options with { MultiThread = ParseSwitch(Next(args, ref i)) };
                    break;

                case "--solve":
                    options = options with { SolveMode = ParseSolveMode(Next(args, ref i)) };
                    break;

                case "--force":
                    force = true;
                    break;

                default:
                    throw new ArgumentException($"未知选项 “{args[i]}”。");
            }
        }

        return (Scenarios.Require(name), options, force);
    }

    private static bool ParseSwitch(string value) => value.ToLowerInvariant() switch
    {
        "on" or "true" or "1" => true,
        "off" or "false" or "0" => false,
        _ => throw new ArgumentException($"无法解析开关值 “{value}”，用 on/off。")
    };

    private static SolveMode ParseSolveMode(string value) => value.ToLowerInvariant() switch
    {
        "regular" => SolveMode.Regular,
        "deterministic" => SolveMode.Deterministic,
        _ => throw new ArgumentException($"无法解析求解模式 “{value}”，用 regular/deterministic。")
    };

    private static string EngineDescription()
    {
        System.Reflection.AssemblyName assembly = typeof(World).Assembly.GetName();
        return $"{assembly.Name}（双精度={Precision.IsDoublePrecision}）";
    }
}
