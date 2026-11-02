namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>布娃娃掉落：约束求解器 + 大量约束耦合下的休眠。</summary>
internal static class RagdollDropScenario
{
    public static ScenarioDefinition Definition { get; } = new()
    {
        Name = "ragdoll-drop",
        Description = "3 个布娃娃（各 10 刚体）落到地面，检验约束求解器与落地后的休眠。",
        TimeStep = (Real)1.0 / (Real)100.0,
        DefaultSteps = 600,
        Build = (world, ctx) =>
        {
            ctx.AddStaticBox(world, new JVector(100, 1, 100), new JVector(0, (Real)(-0.5), 0));

            for (int i = 0; i < 3; i++)
            {
                RagdollBuilder.Build(world, ctx, new JVector(0, (Real)(4 + 2 * i), 0));
            }

            world.SolverIterations = (8, 4);
        }
    };
}
