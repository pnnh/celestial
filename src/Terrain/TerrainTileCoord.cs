using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 64m 大世界瓦片离散空间网格坐标
/// </summary>
public readonly record struct TerrainTileCoord(int X, int Z)
{
    public const float DefaultTileSize = 64.0f;

    /// <summary>
    /// 从世界坐标转换至瓦片坐标
    /// </summary>
    public static TerrainTileCoord FromWorld(float worldX, float worldZ, float tileSize = DefaultTileSize)
    {
        return new TerrainTileCoord(
            (int)MathF.Floor(worldX / tileSize),
            (int)MathF.Floor(worldZ / tileSize)
        );
    }

    /// <summary>
    /// 从 Vector3 世界坐标转换至瓦片坐标
    /// </summary>
    public static TerrainTileCoord FromWorld(Vector3 worldPos, float tileSize = DefaultTileSize)
    {
        return FromWorld(worldPos.X, worldPos.Z, tileSize);
    }

    /// <summary>
    /// 获取瓦片西南角（局部原点）的世界空间坐标
    /// </summary>
    public Vector3 ToWorldOrigin(float tileSize = DefaultTileSize) =>
        new(X * tileSize, 0.0f, Z * tileSize);

    /// <summary>
    /// 获取瓦片几何中心的世界空间坐标 (Y=0)
    /// </summary>
    public Vector3 ToWorldCenter(float tileSize = DefaultTileSize) =>
        new((X + 0.5f) * tileSize, 0.0f, (Z + 0.5f) * tileSize);

    /// <summary>
    /// 计算与另一个瓦片的切比雪夫网格距离（用于方形视野半径与 LOD 环判定）
    /// </summary>
    public int ChebyshevDistance(TerrainTileCoord other) =>
        Math.Max(Math.Abs(X - other.X), Math.Abs(Z - other.Z));

    /// <summary>
    /// 计算与目标世界坐标在水平面上的欧几里得距离（米）
    /// </summary>
    public float EuclideanDistance(Vector3 worldPos, float tileSize = DefaultTileSize)
    {
        var center = ToWorldCenter(tileSize);
        float dx = worldPos.X - center.X;
        float dz = worldPos.Z - center.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public override string ToString() => $"Tile({X}, {Z})";
}

