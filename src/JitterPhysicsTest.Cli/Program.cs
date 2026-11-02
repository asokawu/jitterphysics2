using System.Globalization;
using JitterPhysicsTest.Cli;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// 命令：list / info / run / hash / compare
// 退出码：0 正常，1 用法错误或校验失败，2 运行异常。
return Cli.Execute(args);
