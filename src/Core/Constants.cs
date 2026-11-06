using Godot;

namespace Celestial.Core;

/// <summary>
/// 全局引擎与体素世界核心常量
/// </summary>
public static class Constants
{
    /// <summary>
    /// 单个 Chunk 在 X、Y、Z 三个轴向上的体素尺寸
    /// </summary>
    public const int ChunkSize = 16;

    /// <summary>
    /// 单个 Chunk 内包含的体素总数量 (16 * 16 * 16 = 4096)
    /// </summary>
    public const int ChunkVolume = ChunkSize * ChunkSize * ChunkSize;

    /// <summary>
    /// 单个体素的边长（单位：Godot 引擎米制单位）
    /// </summary>
    public const float VoxelSize = 1.0f;

    /// <summary>
    /// 六个相邻邻居方向单位向量 (X+, X-, Y+, Y-, Z+, Z-)
    /// </summary>
    public static readonly Vector3I[] Directions =
    [
        new(1, 0, 0),  // East / Right (+X)
        new(-1, 0, 0), // West / Left (-X)
        new(0, 1, 0),  // Up / Top (+Y)
        new(0, -1, 0), // Down / Bottom (-Y)
        new(0, 0, 1),  // South / Forward (+Z)
        new(0, 0, -1)  // North / Back (-Z)
    ];

    /// <summary>
    /// 六个方向的法线向量
    /// </summary>
    public static readonly Vector3[] Normals =
    [
        Vector3.Right,
        Vector3.Left,
        Vector3.Up,
        Vector3.Down,
        Vector3.Back,
        Vector3.Forward
    ];
}

