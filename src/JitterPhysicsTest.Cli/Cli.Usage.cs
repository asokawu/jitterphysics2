namespace JitterPhysicsTest.Cli;

internal static partial class Cli
{
    private static void PrintUsage()
    {
        Console.WriteLine("用法：dotnet run --project src/JitterPhysicsTest.Cli -- <命令> [选项]");
        Console.WriteLine();
        Console.WriteLine("命令：");
        Console.WriteLine("  list            列出所有场景");
        Console.WriteLine("  info <场景>     显示场景定义与引擎信息");
        Console.WriteLine("  run <场景>      运行一次并输出统计（耗时、接触数、状态哈希）");
        Console.WriteLine("  watch <场景>    在控制台里逐帧跑：俯视 ASCII 轨迹 + 遥测（--speed 0 表示全速）");
        Console.WriteLine("  hash <场景>     只输出状态哈希，便于跨机器/跨版本比对");
        Console.WriteLine("  compare <场景>  在 4 种求解配置下各跑两遍，检查可复现性与跨线程一致性");
        Console.WriteLine();
        Console.WriteLine("选项：");
        Console.WriteLine("  --steps <n>                 步数，默认取场景定义");
        Console.WriteLine("  --dt <秒>                   步长，默认取场景定义");
        Console.WriteLine("  --threads <on|off>          是否多线程求解，默认 on");
        Console.WriteLine("  --solve <regular|deterministic>  求解模式，默认取场景定义");
        Console.WriteLine("  --force                     允许运行已知会崩溃的缺陷复现场景");
        Console.WriteLine("  --speed <倍数>              仅 watch：按多少倍实时速度播放，0 = 不等待");
        Console.WriteLine("  --manual                    仅 watch：每步等一次按键，Q 退出");
        Console.WriteLine();
        Console.WriteLine("示例：");
        Console.WriteLine("  run box-tower --steps 1200 --solve deterministic");
        Console.WriteLine("  hash ragdoll-drop --threads off");
        Console.WriteLine("  watch raycast-car --speed 4");
        Console.WriteLine("  watch raycast-car --manual");
    }

    private static string Require(string[] args, int index) =>
        args.Length > index
            ? args[index]
            : throw new ArgumentException("缺少场景名。用 list 查看可用场景。");

    private static string Next(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"选项 “{args[index]}” 缺少取值。");
        }

        return args[++index];
    }
}
