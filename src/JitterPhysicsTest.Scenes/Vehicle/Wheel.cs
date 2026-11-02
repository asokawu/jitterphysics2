// 移植自上游 demo：engine\jitterphysics2\src\JitterDemo\Demos\Car\Wheel.cs
//
// 与上游的差异：
//   1. 所有物理量由 float 换成 Real（双精度构建下即 double）；
//   2. 删除了渲染的一行（DebugRenderer.PushPoint）与随之而来的 using JitterDemo.Renderer；
//   3. 新增两个只读属性 OnFloor / SuspensionLength，供自动化测试断言用（不改算法）。
//
// NOTE: The ray cast car demo is a copied and slightly modified version
//       of the vehicle example from the great JigLib. License follows.
/*
Copyright (c) 2007 Danny Chapman
http://www.rowlhouse.co.uk

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
claim that you wrote the original software. If you use this software
in a product, an acknowledgment in the product documentation would be
appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
misrepresented as being the original software.

3. This notice may not be removed or altered from any source
distribution.
*/

using System;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterPhysicsTest.Scenes.Vehicle;

/// <summary>
/// 射线悬挂车轮：向下打射线找地面，按侧滑/纵滑给刚体施加力。
/// </summary>
public class Wheel
{
    private readonly World world;

    private readonly RigidBody car;

    private Real displacement, upSpeed, lastDisplacement;
    private bool onFloor;
    private Real driveTorque;

    private Real angVel;

    /// used to estimate the friction
    private Real angVelForGrip;

    private Real torque;

    private readonly DynamicTree.RayCastFilterPre rayCast;

    /// <summary>当前转向角（弧度）。</summary>
    public Real SteerAngle { get; set; }

    /// <summary>车轮累计转角（弧度），只用于渲染/观察。</summary>
    public Real WheelRotation { get; private set; }

    /// <summary>悬挂阻尼系数。</summary>
    public Real Damping { get; set; }

    /// <summary>悬挂弹簧刚度。</summary>
    public Real Spring { get; set; }

    /// <summary>车轮转动惯量。</summary>
    public Real Inertia { get; set; }

    /// <summary>车轮半径。</summary>
    public Real Radius { get; set; }

    /// <summary>侧向摩擦系数。</summary>
    public Real SideFriction { get; set; }

    /// <summary>纵向摩擦系数。</summary>
    public Real ForwardFriction { get; set; }

    /// <summary>悬挂行程。</summary>
    public Real WheelTravel { get; set; }

    /// <summary>为 true 时车轮锁死。</summary>
    public bool Locked { get; set; }

    /// <summary>车轮角速度上限。</summary>
    public Real MaximumAngularVelocity { get; set; }

    /// <summary>每个车轮使用的射线数量。</summary>
    public int NumberOfRays { get; set; }

    /// <summary>车轮在刚体局部空间的位置。</summary>
    public JVector Position { get; set; }

    public Real AngularVelocity => angVel;

    public readonly JVector Up = JVector.UnitY;

    /// <summary>上一轮 <see cref="PreStep"/> 中射线是否打到了地面。</summary>
    public bool OnFloor => onFloor;

    /// <summary>当前悬挂压缩量（0 表示悬挂完全伸展）。</summary>
    public Real SuspensionLength => displacement;

    /// <param name="world">所在世界。</param>
    /// <param name="car">要施加车轮力的刚体。</param>
    /// <param name="position">车轮在刚体局部空间的位置。</param>
    /// <param name="radius">车轮半径。</param>
    public Wheel(World world, RigidBody car, JVector position, Real radius)
    {
        this.world = world;
        this.car = car;
        Position = position;

        rayCast = RayCastCallback;

        // 与上游 demo 相同的默认值。
        SideFriction = 3;
        ForwardFriction = 5;
        Radius = radius;
        Inertia = 1;
        WheelTravel = (Real)0.2;
        MaximumAngularVelocity = 200;
        NumberOfRays = 1;
    }

    /// <summary>车轮中心在世界空间的位置。</summary>
    public JVector GetWheelCenter()
    {
        return Position + JVector.Transform(Up, car.Orientation) * displacement;
    }

    public void AddTorque(Real torque)
    {
        driveTorque += torque;
    }

    public void PostStep(Real timeStep)
    {
        if (timeStep <= 0) return;

        Real origAngVel = angVel;
        upSpeed = (displacement - lastDisplacement) / timeStep;

        if (Locked)
        {
            angVel = 0;
            torque = 0;
        }
        else
        {
            angVel += torque * timeStep / Inertia;
            torque = 0;

            if (!onFloor) driveTorque *= (Real)0.1;

            // prevent friction from reversing dir - todo do this better
            // by limiting the torque
            if ((origAngVel > angVelForGrip && angVel < angVelForGrip) ||
                (origAngVel < angVelForGrip && angVel > angVelForGrip))
                angVel = angVelForGrip;

            angVel += driveTorque * timeStep / Inertia;
            driveTorque = 0;

            Real maxAngVel = MaximumAngularVelocity;
            angVel = Math.Clamp(angVel, -maxAngVel, maxAngVel);

            WheelRotation += timeStep * angVel;
        }
    }

    public void PreStep(Real timeStep)
    {
        JVector force = JVector.Zero;
        lastDisplacement = displacement;
        displacement = 0;

        JVector worldPos = car.Position + JVector.Transform(Position, car.Orientation);
        JVector worldAxis = JVector.Transform(Up, car.Orientation);

        JVector forward = JVector.Transform(-JVector.UnitZ, car.Orientation);
        JVector wheelFwd = JVector.Transform(forward, JMatrix.CreateRotationMatrix(worldAxis, SteerAngle));

        JVector wheelLeft = JVector.Cross(worldAxis, wheelFwd);
        JVector.NormalizeInPlace(ref wheelLeft);

        JVector wheelUp = JVector.Cross(wheelFwd, wheelLeft);

        Real rayLen = (Real)2.0 * Radius + WheelTravel;

        JVector wheelRayEnd = worldPos - Radius * worldAxis;
        JVector wheelRayOrigin = wheelRayEnd + rayLen * worldAxis;
        JVector wheelRayDelta = wheelRayEnd - wheelRayOrigin;

        Real deltaFwd = (Real)2.0 * Radius / (NumberOfRays + 1);
        Real deltaFwdStart = deltaFwd;

        onFloor = false;

        JVector groundNormal = JVector.Zero;
        JVector groundPos = JVector.Zero;
        Real deepestFrac = Real.MaxValue;
        RigidBody worldBody = null!;

        for (int i = 0; i < NumberOfRays; i++)
        {
            Real distFwd = deltaFwdStart + i * deltaFwd - Radius;
            // 上游写的是 (float)Math.Cos(Math.PI / 2.0f * ...)：把 double 的 Math.Cos 结果强转 float。
            // 单精度下 Math.Cos 收不了 float 参数，所以先把角度常量收成 Real，再交给 MathR.Cos。
            Real zOffset = Radius * (1 - MathR.Cos((Real)(Math.PI / 2.0) * (distFwd / Radius)));

            JVector newOrigin = wheelRayOrigin + distFwd * wheelFwd + zOffset * wheelUp;

            bool result = world.DynamicTree.RayCast(newOrigin, wheelRayDelta,
                rayCast, null, out IDynamicTreeProxy? shape, out JVector normal, out Real frac);

            if (result && frac <= 1)
            {
                // shape must be RigidBodyShape since we filter out other ray tests
                RigidBody body = (shape as RigidBodyShape)!.RigidBody;

                if (frac < deepestFrac)
                {
                    deepestFrac = frac;
                    groundPos = newOrigin + frac * wheelRayDelta;
                    worldBody = body;
                    groundNormal = normal;
                }

                onFloor = true;
            }
        }

        if (!onFloor) return;

        if (groundNormal.LengthSquared() > 0) JVector.NormalizeInPlace(ref groundNormal);

        displacement = rayLen * (1 - deepestFrac);
        displacement = Math.Clamp(displacement, 0, WheelTravel);

        Real displacementForceMag = displacement * Spring;

        // reduce force when suspension is par to ground
        displacementForceMag *= JVector.Dot(groundNormal, worldAxis);

        // apply damping
        Real dampingForceMag = upSpeed * Damping;

        Real totalForceMag = displacementForceMag + dampingForceMag;

        if (totalForceMag < 0) totalForceMag = 0;

        JVector extraForce = totalForceMag * worldAxis;

        force += extraForce;

        // side-slip friction and drive force. Work out wheel- and floor-relative coordinate frame
        JVector groundUp = groundNormal;
        JVector groundLeft = JVector.Cross(groundNormal, wheelFwd);
        if (groundLeft.LengthSquared() > 0) JVector.NormalizeInPlace(ref groundLeft);

        JVector groundFwd = JVector.Cross(groundLeft, groundUp);

        JVector wheelPointVel = car.Velocity +
                                JVector.Cross(car.AngularVelocity, JVector.Transform(Position, car.Orientation));

        // rimVel=(wxr)*v
        JVector rimVel = angVel * JVector.Cross(wheelLeft, groundPos - worldPos);
        wheelPointVel += rimVel;

        if (worldBody == null) throw new InvalidOperationException("car: world body is null.");

        JVector worldVel = worldBody.Velocity +
                           JVector.Cross(worldBody.AngularVelocity, groundPos - worldBody.Position);

        wheelPointVel -= worldVel;

        // sideways forces
        Real noslipVel = (Real)0.2;
        Real slipVel = (Real)0.4;
        Real slipFactor = (Real)0.7;

        Real smallVel = 3;
        Real friction = SideFriction;

        Real sideVel = JVector.Dot(wheelPointVel, groundLeft);

        if (sideVel > slipVel || sideVel < -slipVel)
        {
            friction *= slipFactor;
        }
        else if (sideVel > noslipVel || sideVel < -noslipVel)
        {
            friction *= 1 - (1 - slipFactor) * (Math.Abs(sideVel) - noslipVel) / (slipVel - noslipVel);
        }

        if (sideVel < 0)
            friction *= -1;

        if (Math.Abs(sideVel) < smallVel)
            friction *= Math.Abs(sideVel) / smallVel;

        Real sideForce = -friction * totalForceMag;

        extraForce = sideForce * groundLeft;
        force += extraForce;

        // fwd/back forces
        friction = ForwardFriction;
        Real fwdVel = JVector.Dot(wheelPointVel, groundFwd);

        if (fwdVel > slipVel || fwdVel < -slipVel)
        {
            friction *= slipFactor;
        }
        else if (fwdVel > noslipVel || fwdVel < -noslipVel)
        {
            friction *= 1 - (1 - slipFactor) * (Math.Abs(fwdVel) - noslipVel) / (slipVel - noslipVel);
        }

        if (fwdVel < 0)
            friction *= -1;

        if (Math.Abs(fwdVel) < smallVel)
            friction *= Math.Abs(fwdVel) / smallVel;

        Real fwdForce = -friction * totalForceMag;

        extraForce = fwdForce * groundFwd;
        force += extraForce;

        // fwd force also spins the wheel
        JVector wheelCentreVel = car.Velocity +
                                 JVector.Cross(car.AngularVelocity, JVector.Transform(Position, car.Orientation));

        angVelForGrip = JVector.Dot(wheelCentreVel, groundFwd) / Radius;
        torque += -fwdForce * Radius;

        // add force to car
        car.AddForce(force, groundPos);

        // add force to the world
        if (worldBody.MotionType == MotionType.Dynamic)
        {
            const Real maxOtherBodyAcc = 500;
            Real maxOtherBodyForce = maxOtherBodyAcc * worldBody.Mass;

            if (force.LengthSquared() > (maxOtherBodyForce * maxOtherBodyForce))
                force *= maxOtherBodyForce / force.Length();

            worldBody.SetActivationState(true);

            worldBody.AddForce(force * -1, groundPos);
        }
    }

    private bool RayCastCallback(IDynamicTreeProxy shape)
    {
        if (shape is not RigidBodyShape rbs) return false;
        return rbs.RigidBody != car;
    }
}
