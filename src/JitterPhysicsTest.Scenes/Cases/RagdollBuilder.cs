namespace JitterPhysicsTest.Scenes.Cases;

/// <summary>
/// 搭一个 10 刚体、20 约束的布娃娃（头 / 躯干 / 双臂两段 / 双腿两段）。
/// 参考上游 ReproducibilityTest 里的布娃娃拓扑，约束类型覆盖球铰、锥限位、扭转、铰链。
/// </summary>
internal static class RagdollBuilder
{
    public static void Build(World world, ScenarioContext ctx, JVector origin)
    {
        RigidBody head = ctx.AddSphere(world, (Real)0.15, origin, mass: (Real)2);

        RigidBody torso = ctx.AddDynamicBox(world,
            new JVector((Real)0.35, (Real)0.6, (Real)0.2),
            origin + new JVector(0, (Real)(-0.46), 0), mass: (Real)8);

        RigidBody upperArmLeft = ctx.AddCapsule(world, (Real)0.07, (Real)0.2,
            origin + new JVector((Real)0.30, (Real)(-0.2), 0), mass: (Real)1);
        RigidBody upperArmRight = ctx.AddCapsule(world, (Real)0.07, (Real)0.2,
            origin + new JVector((Real)(-0.30), (Real)(-0.2), 0), mass: (Real)1);
        RigidBody lowerArmLeft = ctx.AddCapsule(world, (Real)0.06, (Real)0.2,
            origin + new JVector((Real)0.55, (Real)(-0.2), 0), mass: (Real)1);
        RigidBody lowerArmRight = ctx.AddCapsule(world, (Real)0.06, (Real)0.2,
            origin + new JVector((Real)(-0.55), (Real)(-0.2), 0), mass: (Real)1);

        RigidBody upperLegLeft = ctx.AddCapsule(world, (Real)0.08, (Real)0.3,
            origin + new JVector((Real)0.11, (Real)(-0.85), 0), mass: (Real)2);
        RigidBody upperLegRight = ctx.AddCapsule(world, (Real)0.08, (Real)0.3,
            origin + new JVector((Real)(-0.11), (Real)(-0.85), 0), mass: (Real)2);
        RigidBody lowerLegLeft = ctx.AddCapsule(world, (Real)0.08, (Real)0.3,
            origin + new JVector((Real)0.11, (Real)(-1.2), 0), mass: (Real)1.5);
        RigidBody lowerLegRight = ctx.AddCapsule(world, (Real)0.08, (Real)0.3,
            origin + new JVector((Real)(-0.11), (Real)(-1.2), 0), mass: (Real)1.5);

        JQuaternion horizontal = JQuaternion.CreateRotationZ((Real)MathR.PI / 2);
        upperArmLeft.Orientation = horizontal;
        upperArmRight.Orientation = horizontal;
        lowerArmLeft.Orientation = horizontal;
        lowerArmRight.Orientation = horizontal;

        world.CreateConstraint<BallSocket>(head, torso)
            .Initialize(origin + new JVector(0, (Real)(-0.15), 0));
        world.CreateConstraint<ConeLimit>(head, torso)
            .Initialize(-JVector.UnitZ, AngularLimit.FromDegree(0, 45));

        world.CreateConstraint<BallSocket>(torso, upperArmLeft)
            .Initialize(origin + new JVector((Real)0.20, (Real)(-0.2), 0));
        world.CreateConstraint<TwistAngle>(torso, upperArmLeft)
            .Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        world.CreateConstraint<BallSocket>(torso, upperArmRight)
            .Initialize(origin + new JVector((Real)(-0.20), (Real)(-0.2), 0));
        world.CreateConstraint<TwistAngle>(torso, upperArmRight)
            .Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        world.CreateConstraint<BallSocket>(upperArmLeft, lowerArmLeft)
            .Initialize(origin + new JVector((Real)0.42, (Real)(-0.2), 0));
        world.CreateConstraint<HingeAngle>(lowerArmLeft, upperArmLeft)
            .Initialize(JVector.UnitY, AngularLimit.FromDegree(-160, 0));

        world.CreateConstraint<BallSocket>(upperArmRight, lowerArmRight)
            .Initialize(origin + new JVector((Real)(-0.42), (Real)(-0.2), 0));
        world.CreateConstraint<HingeAngle>(lowerArmRight, upperArmRight)
            .Initialize(JVector.UnitY, AngularLimit.FromDegree(0, 160));

        world.CreateConstraint<BallSocket>(torso, upperLegLeft)
            .Initialize(origin + new JVector((Real)0.11, (Real)(-0.7), 0));
        world.CreateConstraint<TwistAngle>(torso, upperLegLeft)
            .Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, 80));
        world.CreateConstraint<ConeLimit>(torso, upperLegLeft)
            .Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        world.CreateConstraint<BallSocket>(torso, upperLegRight)
            .Initialize(origin + new JVector((Real)(-0.11), (Real)(-0.7), 0));
        world.CreateConstraint<TwistAngle>(torso, upperLegRight)
            .Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, 80));
        world.CreateConstraint<ConeLimit>(torso, upperLegRight)
            .Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        world.CreateConstraint<BallSocket>(upperLegLeft, lowerLegLeft)
            .Initialize(origin + new JVector((Real)0.11, (Real)(-1.05), 0));
        world.CreateConstraint<HingeAngle>(upperLegLeft, lowerLegLeft)
            .Initialize(JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        world.CreateConstraint<BallSocket>(upperLegRight, lowerLegRight)
            .Initialize(origin + new JVector((Real)(-0.11), (Real)(-1.05), 0));
        world.CreateConstraint<HingeAngle>(upperLegRight, lowerLegRight)
            .Initialize(JVector.UnitX, AngularLimit.FromDegree(-120, 0));
    }
}
