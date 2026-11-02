namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>8 层 3×3 箱子塔：堆叠稳定性、休眠（deactivation）与静止抖动。</summary>
internal static class BoxTowerScenario
{
    public static ScenarioDefinition Definition { get; } = new()
    {
        Name = "box-tower",
        Description = "8 层 3×3 堆叠箱子（72 个动态体），检验堆叠稳定性、休眠与静止抖动。" +
                      "回归用例：修复 DynamicTree 高度平衡的无限递归前，双精度下第 1 步即栈溢出。",
        TimeStep = (Real)1.0 / (Real)60.0,
        DefaultSteps = 600,
        Build = (world, ctx) =>
        {
            ctx.AddStaticBox(world, new JVector(50, 1, 50), new JVector(0, (Real)(-0.5), 0));

            for (int layer = 0; layer < 8; layer++)
            {
                for (int x = 0; x < 3; x++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        JVector position = new((Real)(x - 1), (Real)0.5 + layer, (Real)(z - 1));
                        RigidBody box = ctx.AddDynamicBox(world,
                            new JVector((Real)0.9, (Real)0.9, (Real)0.9), position);

                        // 逐层留一点初始间隙，避免第一帧就落在最坏接触情形上。
                        box.Position += new JVector(0, (Real)0.02 * layer, 0);
                    }
                }
            }
        }
    };
}
