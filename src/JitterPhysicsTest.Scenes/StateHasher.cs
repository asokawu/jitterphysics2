using System.Runtime.CompilerServices;

namespace JitterPhysicsTest.Scenes;

/// <summary>
/// 对场景内所有刚体的状态做 FNV-1a 64 位哈希。逐位比较（而不是数值相等），
/// 所以 -0.0 与 0.0、NaN 都不会被掩盖；哈希相同意味着两次模拟每一位状态都相同。
/// 代价是它对引擎版本敏感：引擎升级后哈希必然变化，属于预期的回归信号。
/// </summary>
public static class StateHasher
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(IReadOnlyList<RigidBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        ulong hash = OffsetBasis;

        foreach (RigidBody body in bodies)
        {
            hash = Mix(hash, (ulong)body.MotionType);
            hash = Mix(hash, body.IsActive ? 1UL : 0UL);
            hash = Mix(hash, (ulong)body.Contacts.Count);

            JVector p = body.Position;
            hash = Mix(hash, Bits(p.X));
            hash = Mix(hash, Bits(p.Y));
            hash = Mix(hash, Bits(p.Z));

            JQuaternion q = body.Orientation;
            hash = Mix(hash, Bits(q.X));
            hash = Mix(hash, Bits(q.Y));
            hash = Mix(hash, Bits(q.Z));
            hash = Mix(hash, Bits(q.W));

            JVector v = body.Velocity;
            hash = Mix(hash, Bits(v.X));
            hash = Mix(hash, Bits(v.Y));
            hash = Mix(hash, Bits(v.Z));

            JVector w = body.AngularVelocity;
            hash = Mix(hash, Bits(w.X));
            hash = Mix(hash, Bits(w.Y));
            hash = Mix(hash, Bits(w.Z));
        }

        return hash;
    }

    /// <summary>所有刚体状态是否都是有限值（没有 NaN / Inf）。</summary>
    public static bool IsFinite(IReadOnlyList<RigidBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        foreach (RigidBody body in bodies)
        {
            if (!IsFinite(body.Position) || !IsFinite(body.Velocity) || !IsFinite(body.AngularVelocity))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>动态刚体中的最大线速度。</summary>
    public static Real MaxSpeed(IReadOnlyList<RigidBody> bodies)
    {
        Real max = 0;

        foreach (RigidBody body in bodies)
        {
            if (body.MotionType != MotionType.Dynamic)
            {
                continue;
            }

            Real speed = body.Velocity.Length();
            if (speed > max)
            {
                max = speed;
            }
        }

        return max;
    }

    /// <summary>动态刚体中的最低高度，用来检查穿地 / 爆炸。</summary>
    public static Real MinHeight(IReadOnlyList<RigidBody> bodies)
    {
        Real min = Real.MaxValue;

        foreach (RigidBody body in bodies)
        {
            if (body.MotionType != MotionType.Dynamic)
            {
                continue;
            }

            if (body.Position.Y < min)
            {
                min = body.Position.Y;
            }
        }

        return min == Real.MaxValue ? 0 : min;
    }

    private static bool IsFinite(JVector v) =>
        double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Bits(Real value) => BitConverter.DoubleToInt64Bits((double)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong hash, long value) => Mix(hash, (ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong hash, ulong value)
    {
        hash ^= value;
        hash *= Prime;
        return hash;
    }
}
