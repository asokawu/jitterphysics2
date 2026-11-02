using BenchmarkDotNet.Running;
using JitterPhysicsTest.Bench;

// 用法：dotnet run -c Release --project bench/JitterPhysicsTest.Bench -- --filter *StepBenchmarks*
BenchmarkSwitcher.FromAssembly(typeof(StepBenchmarks).Assembly).Run(args);
