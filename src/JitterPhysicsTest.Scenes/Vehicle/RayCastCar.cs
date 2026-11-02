// 移植自上游 demo：engine\jitterphysics2\src\JitterDemo\Demos\Car\RayCastCar.cs
//
// 与上游的差异仅两处，都是为了让它能在两种精度下编译并被自动化测试驱动：
//   1. 所有物理量由 float 换成 Real（双精度构建下即 double），小数写成 (Real)0.45 这样的形式；
//   2. 删除了渲染相关代码（demo 里的 DrawUpdate 在 Playground 里，本类本来就没有）。
// 算法与参数一字未改——这是刻意的：我们要测的就是 demo 里那套射线悬挂本身。
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
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterPhysicsTest.Scenes.Vehicle;

/// <summary>
/// 默认 4 轮射线悬挂车。要自定义车，用 <see cref="Wheel"/> 往刚体上加。
/// </summary>
public class RayCastCar
{
    private readonly World world;

    public RigidBody Body { get; }

    private Real destSteering;
    private Real destAccelerate;
    private Real steering;
    private Real accelerate;

    /// <summary>前轮的最大转向角（弧度）。</summary>
    public Real SteerAngle { get; set; }

    /// <summary>加速时施加的最大扭矩。</summary>
    public Real DriveTorque { get; set; }

    /// <summary>油门响应速度，越大加得越快。</summary>
    public Real AccelerationRate { get; set; }

    /// <summary>转向响应速度。</summary>
    public Real SteerRate { get; set; }

    // don't damp perfect, allow some bounciness.
    private const Real dampingFrac = (Real)0.8;
    private const Real springFrac = (Real)0.45;

    public RayCastCar(World world)
    {
        this.world = world;

        // 与上游 demo 相同的默认值。
        AccelerationRate = 10;
        SteerAngle = (Real)JAngle.FromDegree((Real)40);
        DriveTorque = 340;
        SteerRate = 5;

        Body = world.CreateRigidBody();

        TransformedShape tfs1 = new(new BoxShape((Real)3.1, (Real)1.4, 8), new JVector(0, (Real)0.4, 0));
        TransformedShape tfs2 = new(new BoxShape((Real)2.4, (Real)0.8, 5), new JVector(0, (Real)1.5, (Real)1.1));

        Body.AddShape(tfs1);
        Body.AddShape(tfs2);

        Real mass = 100;
        JVector sides = new((Real)3.1, 1, 8);

        Real Ixx = (Real)(1.0 / 12.0) * mass * (sides.Y * sides.Y + sides.Z * sides.Z);
        Real Iyy = (Real)(1.0 / 12.0) * mass * (sides.X * sides.X + sides.Z * sides.Z);
        Real Izz = (Real)(1.0 / 12.0) * mass * (sides.X * sides.X + sides.Y * sides.Y);

        JSymmetricMatrix inertia = JSymmetricMatrix.CreateScale(Ixx, Iyy, Izz);

        Body.Position = new JVector(0, (Real)0.5, -4);
        Body.SetMassInertia(inertia, mass);

        Body.Damping = ((Real)0.0001, (Real)0.0001);

        // 默认车轮布局。
        Wheels[0] = new Wheel(world, Body, new JVector((Real)(-1.3), (Real)(-0.1), (Real)(-2.5)), (Real)0.60);
        Wheels[1] = new Wheel(world, Body, new JVector((Real)1.3, (Real)(-0.1), (Real)(-2.5)), (Real)0.60);
        Wheels[2] = new Wheel(world, Body, new JVector((Real)(-1.3), (Real)(-0.1), (Real)2.4), (Real)0.60);
        Wheels[3] = new Wheel(world, Body, new JVector((Real)1.3, (Real)(-0.1), (Real)2.4), (Real)0.60);

        AdjustWheelValues();
    }

    /// <summary>
    /// 按车质量、轮半径和重力重新算惯量、阻尼与弹簧刚度。改了车轮参数后要调用。
    /// </summary>
    public void AdjustWheelValues()
    {
        Real mass = Body.Mass / 4;
        Real wheelMass = Body.Mass * (Real)0.03;

        foreach (Wheel w in Wheels)
        {
            w.Inertia = (Real)0.5 * (w.Radius * w.Radius) * wheelMass;
            w.Spring = mass * world.Gravity.Length() / (w.WheelTravel * springFrac);
            w.Damping = (Real)2 * MathR.Sqrt(w.Spring * Body.Mass) * (Real)0.25 * dampingFrac;
        }
    }

    public Wheel[] Wheels { get; } = new Wheel[4];

    /// <summary>
    /// 设置输入。
    /// </summary>
    /// <param name="accelerate">-1..1 的油门。最大加速度由 <see cref="AccelerationRate"/> 决定。</param>
    /// <param name="steer">-1..1 的转向。最大转角由 <see cref="SteerAngle"/> 决定。</param>
    public void SetInput(Real accelerate, Real steer)
    {
        destAccelerate = accelerate;
        destSteering = steer;
    }

    public void Step(Real timestep)
    {
        foreach (Wheel w in Wheels) w.PreStep(timestep);

        Real deltaAccelerate = timestep * AccelerationRate;
        Real deltaSteering = timestep * SteerRate;

        Real dAccelerate = destAccelerate - accelerate;
        dAccelerate = Math.Clamp(dAccelerate, -deltaAccelerate, deltaAccelerate);

        Real dSteering = destSteering - steering;
        dSteering = Math.Clamp(dSteering, -deltaSteering, deltaSteering);

        accelerate += dAccelerate;
        steering += dSteering;

        Real maxTorque = DriveTorque * (Real)0.5;

        foreach (Wheel w in Wheels)
        {
            w.AddTorque(maxTorque * accelerate);

            if (destAccelerate == 0 && w.AngularVelocity < (Real)0.8)
            {
                // 车速足够低且没有油门时，反向施加一点扭矩让车彻底停住。
                w.AddTorque(-w.AngularVelocity);
            }
        }

        Real alpha = SteerAngle * steering;

        Wheels[0].SteerAngle = alpha;
        Wheels[1].SteerAngle = alpha;

        foreach (Wheel w in Wheels)
        {
            w.PostStep(timestep);
        }
    }
}
