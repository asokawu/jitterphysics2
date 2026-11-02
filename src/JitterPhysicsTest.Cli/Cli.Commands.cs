using System.Globalization;
using JitterPhysicsTest.Scenes;

namespace JitterPhysicsTest.Cli;

/// <summary>list / info / run / hash 四个命令的实现。</summary>
internal static partial class Cli
{
    public static int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            return args[0] switch
            {
                "list" => List(),
                "info" => Info(Require(args, 1)),
                "run" => Run(args[1..]),
                "watch" => Watch(args[1..]),
                "hash" => Hash(args[1..]),
                "compare" => Compare(args[1..]),
                "help" or "-h" or "--help" => Help(),
                _ => Unknown(args[0])
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            return 2;
        }
    }

    private static int Help()
    {
        PrintUsage();
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"未知命令 “{command}”。");
        PrintUsage();
        return 1;
    }

    private static int List()
    {
        Console.WriteLine($"引擎：{EngineDescription()}");
        Console.WriteLine();
        Console.WriteLine("可运行场景：");

        foreach (ScenarioDefinition scenario in Scenarios.Runnable)
        {
            Console.WriteLine($"  {scenario.Name,-14} dt={scenario.TimeStep:F6}  默认步数={scenario.DefaultSteps,-5} {scenario.Description}");
        }

        List<ScenarioDefinition> broken = Scenarios.All.Where(s => s.HasKnownIssue).ToList();

        if (broken.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("缺陷复现场景（已知会崩，需加 --force）：");

            foreach (ScenarioDefinition scenario in broken)
            {
                Console.WriteLine($"  {scenario.Name,-14} {scenario.KnownIssue}");
            }
        }

        return 0;
    }

    private static int Info(string name)
    {
        ScenarioDefinition scenario = Scenarios.Require(name);

        Console.WriteLine($"引擎：{EngineDescription()}");
        Console.WriteLine($"场景：{scenario.Name}");
        Console.WriteLine($"说明：{scenario.Description}");
        Console.WriteLine($"步长：{scenario.TimeStep}");
        Console.WriteLine($"默认步数：{scenario.DefaultSteps}");
        Console.WriteLine($"默认求解模式：{scenario.SolveMode}");

        if (scenario.KnownIssue is not null)
        {
            Console.WriteLine($"已知问题：{scenario.KnownIssue}");
        }

        return 0;
    }

    private static int Run(string[] args)
    {
        (ScenarioDefinition scenario, SimulationOptions options, bool force) = ParseRun(args);

        if (!AllowKnownIssue(scenario, force))
        {
            return 1;
        }

        SimulationResult result = SimulationRunner.Run(scenario, options);

        Console.WriteLine(result.Describe());
        return result.IsFinite ? 0 : 1;
    }

    private static int Hash(string[] args)
    {
        (ScenarioDefinition scenario, SimulationOptions options, bool force) = ParseRun(args);

        if (!AllowKnownIssue(scenario, force))
        {
            return 1;
        }

        SimulationResult result = SimulationRunner.Run(scenario, options);

        Console.WriteLine($"0x{result.StateHash:X16}");
        return 0;
    }

    /// <summary>已知会崩溃的场景默认拒绝运行，除非显式 --force。</summary>
    private static bool AllowKnownIssue(ScenarioDefinition scenario, bool force)
    {
        if (scenario.KnownIssue is null || force)
        {
            return true;
        }

        Console.Error.WriteLine($"场景 “{scenario.Name}” 是缺陷复现用例，已知问题：{scenario.KnownIssue}");
        Console.Error.WriteLine("确认要复现就加 --force（可能会栈溢出终止进程）。");
        return false;
    }
}
