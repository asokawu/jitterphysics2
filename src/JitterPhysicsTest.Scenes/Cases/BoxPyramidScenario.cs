namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>12 层金字塔：接触数量大而密集，检验接触流形与求解器收敛。</summary>
internal static class BoxPyramidScenario
{
    public static ScenarioDefinition Definition { get; } = new()
    {
        Name = "box-pyramid",
        Description = "12 层金字塔（78 个动态体），大量同时接触，检验接触流形与求解器收敛。" +
                      "回归用例：同 box-tower，修复前双精度下第 1 步栈溢出。",
        TimeStep = (Real)1.0 / (Real)60.0,
        DefaultSteps = 300,
        Build = (world, ctx) =>
        {
            ctx.AddStaticBox(world, new JVector(60, 1, 60), new JVector(0, (Real)(-0.5), 0));

            const int rows = 12;
            const Real size = 1;
            Real side = size * (Real)0.98;

            for (int row = 0; row < rows; row++)
            {
                int count = rows - row;
                Real offset = (count - 1) * size * (Real)0.5;

                for (int i = 0; i < count; i++)
                {
                    JVector position = new(i * size - offset, size * (Real)0.5 + row * size, 0);
                    ctx.AddDynamicBox(world, new JVector(side, side, side), position);
                }
            }
        }
    };
}
