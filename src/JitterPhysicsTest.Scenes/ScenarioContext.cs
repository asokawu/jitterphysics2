namespace JitterPhysicsTest.Scenes;

/// <summary>
/// 场景构建期上下文：记录所有被创建的刚体。运行结束后的统计与哈希只依赖这个列表的顺序，
/// 所以场景本身必须“每次构建顺序完全一致”，哈希才可比对。
/// </summary>
public sealed class ScenarioContext
{
    private readonly List<RigidBody> _bodies = [];

    /// <summary>按创建顺序排列的刚体（含静态体）。</summary>
    public IReadOnlyList<RigidBody> Bodies => _bodies;

    /// <summary>
    /// 场景自己的挂载点（例如载具对象），供 <see cref="ScenarioDefinition.Step"/> 取用。
    /// 每次运行都是新的上下文，所以场景定义本身可以保持无状态——不要把随运行变化的东西
    /// 存在 <see cref="ScenarioDefinition"/> 的字段里，那会在同一进程内多次运行时串味。
    /// </summary>
    public object? Tag { get; set; }

    public RigidBody Track(RigidBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _bodies.Add(body);
        return body;
    }

    public RigidBody AddDynamicBox(World world, JVector size, JVector position, Real mass = 1)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new BoxShape(size));
        body.SetMassInertia(mass);
        body.Position = position;
        return Track(body);
    }

    public RigidBody AddStaticBox(World world, JVector size, JVector position)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new BoxShape(size));
        body.MotionType = MotionType.Static;
        body.Position = position;
        return Track(body);
    }

    public RigidBody AddSphere(World world, Real radius, JVector position, Real mass = 1)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(radius));
        body.SetMassInertia(mass);
        body.Position = position;
        return Track(body);
    }

    public RigidBody AddCapsule(World world, Real radius, Real length, JVector position, Real mass = 1)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new CapsuleShape(radius, length));
        body.SetMassInertia(mass);
        body.Position = position;
        return Track(body);
    }
}
