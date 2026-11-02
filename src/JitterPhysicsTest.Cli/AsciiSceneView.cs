using Jitter2.LinearMath;

namespace JitterPhysicsTest.Cli;

/// <summary>
/// 控制台俯视图：把世界 XZ 平面画成字符网格。没有渲染器时，这是唯一能"看着它跑"的办法。
/// </summary>
/// <remarks>
/// 每帧自动缩放以容纳已经走过的轨迹，所以直线段是慢慢铺开、转向段能看到车头拐弯。
/// 帧按阶段用不同字符标记：收敛 ',、全油门 '#、转向 '*、当前位置用方向箭头。
/// </remarks>
internal sealed class AsciiSceneView
{
    private readonly int _width;
    private readonly int _height;
    private readonly List<(double X, double Z, char Mark)> _trail = [];

    public AsciiSceneView(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void AddTrailPoint(double x, double z, char mark)
    {
        _trail.Add((x, z, mark));

        // 轨迹点比格子多时丢一半早期点，避免长场景把内存吃满；视觉上无影响。
        if (_trail.Count > 20000)
        {
            _trail.RemoveRange(0, _trail.Count / 2);
        }
    }

    public void Render(JVector position, JVector forward, string telemetry)
    {
        double minX = position.X, maxX = position.X, minZ = position.Z, maxZ = position.Z;

        foreach ((double x, double z, _) in _trail)
        {
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minZ = Math.Min(minZ, z);
            maxZ = Math.Max(maxZ, z);
        }

        // 留一圈空白，并保证比例不失真（字符格大约 2:1 高）。
        double spanX = Math.Max(maxX - minX, 1e-3);
        double spanZ = Math.Max(maxZ - minZ, 1e-3);
        double padding = 0.06;
        double scale = Math.Min(_width / (spanX * (1 + 2 * padding)), _height / (spanZ * (1 + 2 * padding)));

        double offsetX = minX - spanX * padding;
        double offsetZ = minZ - spanZ * padding;

        char[,] cells = new char[_height, _width];
        for (int row = 0; row < _height; row++)
        {
            for (int column = 0; column < _width; column++)
            {
                cells[row, column] = ' ';
            }
        }

        foreach ((double x, double z, char mark) in _trail)
        {
            Place(cells, x, z, mark, offsetX, offsetZ, scale);
        }

        Place(cells, position.X, position.Z, Heading(forward), offsetX, offsetZ, scale);

        var builder = new System.Text.StringBuilder();

        for (int row = 0; row < _height; row++)
        {
            for (int column = 0; column < _width; column++)
            {
                builder.Append(cells[row, column]);
            }

            builder.AppendLine();
        }

        builder.Append(telemetry);

        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // 输出被重定向时 Console.Clear 不可用，忽略即可。
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(builder.ToString());
        Console.ResetColor();
    }

    private void Place(char[,] cells, double x, double z, char mark, double offsetX, double offsetZ, double scale)
    {
        int column = (int)Math.Round((x - offsetX) * scale);
        int row = (int)Math.Round((z - offsetZ) * scale);

        // 车朝 -Z 时往屏幕上方走，所以行号直接用 z（不翻转）。
        column = Math.Clamp(column, 0, _width - 1);
        row = Math.Clamp(row, 0, _height - 1);

        cells[row, column] = mark;
    }

    private static char Heading(JVector forward)
    {
        double x = forward.X;
        double z = forward.Z;

        if (Math.Abs(z) >= Math.Abs(x))
        {
            return z <= 0 ? '^' : 'v';
        }

        return x >= 0 ? '>' : '<';
    }
}
