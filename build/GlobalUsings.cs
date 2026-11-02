// 本仓库所有工程共享的全局 using（由 Directory.Build.props 链接进来）。
//
// 精度：引擎以 -p:DoublePrecision=true 编译，本仓库的 Directory.Build.props 也定义同样的符号，
// 两边的 Real / MathR 才是同一种类型，场景代码可以直接用引擎的 API。
#if !USE_DOUBLE_PRECISION
#error JitterPhysicsTest 必须以双精度编译：请加 -p:DoublePrecision=true（tools\*.ps1 已内置）。
#endif

global using Jitter2;
global using Jitter2.Collision;
global using Jitter2.Collision.Shapes;
global using Jitter2.Dynamics;
global using Jitter2.Dynamics.Constraints;
global using Jitter2.LinearMath;

global using Real = System.Double;
global using MathR = System.Math;
