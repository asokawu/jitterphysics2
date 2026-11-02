using JitterPhysicsTest.Scenes;
using JitterPhysicsTest.Scenes.Cases;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterPhysicsTest.Tests;

/// <summary>
/// 射线悬挂车的物理断言。移植自上游 Demo06 的 RayCastCar，参数与算法一字未改，
/// 只是把「肉眼看车开没开」换成了可重复的数值判据。
/// </summary>
[TestFixture]
public sealed class RayCastCarTests
{
    /// <summary>场景里刚体的创建顺序：0 是地面，1 是车。哈希与断言都依赖这个顺序。</summary>
    private const int CarBodyIndex = 1;

    private readonly record struct Snapshot(JVector Position, JVector Forward, Real Speed, Real MinHeight);

    private static Snapshot Run(int steps)
    {
        JVector position = JVector.Zero;
        JVector forward = -JVector.UnitZ;
        Real speed = 0;

        SimulationResult result = SimulationRunner.Run(Scenarios.Require("raycast-car"), new SimulationOptions
        {
            Steps = steps,
            Inspect = bodies =>
            {
                RigidBody car = bodies[CarBodyIndex];
                position = car.Position;
                forward = JVector.Transform(-JVector.UnitZ, car.Orientation);
                speed = car.Velocity.Length();
            }
        });

        Assert.That(result.IsFinite, Is.True, "状态出现 NaN/Inf");

        return new Snapshot(position, forward, speed, result.MinHeight);
    }

    [Test]
    public void SuspensionHoldsTheCarAboveTheGround()
    {
        Snapshot settled = Run(RayCastCarScenario.SettleSteps);

        Assert.Multiple(() =>
        {
            // 悬挂托住车：车体原点不该掉到地面（y=0）以下，也不该被弹飞。
            Assert.That(settled.Position.Y, Is.GreaterThan(0.2), "悬挂没托住，车陷进地面");
            Assert.That(settled.Position.Y, Is.LessThan(2.0), "悬挂把车弹飞了");
            Assert.That(settled.Speed, Is.LessThan(1.0), "落地收敛后应该基本静止");
            Assert.That(settled.MinHeight, Is.GreaterThan(0.0), "任何时刻都不能穿地");
        });
    }

    [Test]
    public void FullThrottleDrivesTheCarForward()
    {
        Snapshot start = Run(RayCastCarScenario.SettleSteps);
        Snapshot driven = Run(RayCastCarScenario.AfterDriveSteps);

        double travelled = Math.Abs((double)(driven.Position.Z - start.Position.Z));

        Assert.Multiple(() =>
        {
            // 车头指向 -Z，全油门 3 秒应该实打实跑出去几十米。
            Assert.That(driven.Position.Z, Is.LessThan(start.Position.Z), "车没有朝 -Z 前进");
            Assert.That(travelled, Is.GreaterThan(20.0), $"3 秒只跑了 {travelled:F1} m，油门没生效");
            Assert.That(driven.Speed, Is.GreaterThan(5.0), "速度上不去");
            Assert.That(driven.Speed, Is.LessThan(60.0), "速度异常发散");
        });
    }

    [Test]
    public void SteeringTurnsTheCar()
    {
        Snapshot straight = Run(RayCastCarScenario.AfterDriveSteps);
        Snapshot turned = Run(RayCastCarScenario.TotalSteps);

        // 车头方向的横向分量：直线段应该接近 0，转向段应该明显不为 0。
        double before = Math.Abs((double)straight.Forward.X);
        double after = Math.Abs((double)turned.Forward.X);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.LessThan(0.05), $"直线段车头已经偏了：forward.X={before:F3}");
            Assert.That(after - before, Is.GreaterThan(0.15), $"转向没生效：forward.X {before:F3} -> {after:F3}");
            Assert.That(turned.Speed, Is.LessThan(60.0), "转向过程中速度发散");
        });
    }
}
