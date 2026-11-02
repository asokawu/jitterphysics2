namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>高速球撞薄墙：检验 speculative contact 与穿透保护。</summary>
internal static class SphereCannonScenario
{
    public static ScenarioDefinition Definition { get; } = new()
    {
        Name = "sphere-cannon",
        Description = "半径 0.25 的球以 20 m/s 撞向 0.1 厚的薄墙，检验高速穿透与推测接触。" +
                      "实测（单双精度一致）：20 m/s 跑 60 步 0/16 穿墙、90 步 1/16、30 m/s 3/16、40 m/s 16/16。",
        TimeStep = (Real)1.0 / (Real)60.0,
        DefaultSteps = 60,
        Build = (world, ctx) =>
        {
            // 地面开得足够大：即使球穿墙飞出，也仍然落在地面上，不会掉出场景变成自由落体。
            ctx.AddStaticBox(world, new JVector(600, 1, 600), new JVector(0, (Real)(-0.5), 0));

            // 薄墙厚度 0.1：球一步就能跨过去。
            for (int i = 0; i < 8; i++)
            {
                JVector position = new(0, 2, (Real)(i * 2 - 7));
                ctx.AddStaticBox(world, new JVector((Real)0.1, 4, 2), position);
            }

            for (int i = 0; i < 16; i++)
            {
                JVector position = new((Real)(-20), (Real)(0.5 + i % 4), (Real)((i / 4) * 1.5 - 3));
                RigidBody ball = ctx.AddSphere(world, (Real)0.25, position);
                ball.Velocity = new JVector(20, 0, 0);
                ball.Restitution = (Real)0.2;
            }
        }
    };
}
