using System;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 体素类型枚举（紧凑 byte 存储，节省内存与网络带宽）
/// </summary>
public enum VoxelType : byte
{
    Air = 0,
    Dirt = 1,
    Grass = 2,
    Stone = 3,
    Bedrock = 4,   // 触底传统地形边界，不可挖掘破坏
    Wood = 5,
    Planks = 6,
    Sand = 7
}

/// <summary>
/// 体素属性与查询扩展方法
/// </summary>
public static class VoxelTypeExtensions
{
    public static bool IsSolid(this VoxelType type) => type != VoxelType.Air;

    public static bool IsDestructible(this VoxelType type) =>
        type != VoxelType.Air && type != VoxelType.Bedrock;

    public static Color GetDebugColor(this VoxelType type) => type switch
    {
        VoxelType.Air => Colors.Transparent,
        VoxelType.Dirt => new Color(0.45f, 0.28f, 0.16f),
        VoxelType.Grass => new Color(0.28f, 0.58f, 0.24f),
        VoxelType.Stone => new Color(0.55f, 0.55f, 0.55f),
        VoxelType.Bedrock => new Color(0.20f, 0.20f, 0.22f),
        VoxelType.Wood => new Color(0.40f, 0.22f, 0.12f),
        VoxelType.Planks => new Color(0.72f, 0.53f, 0.32f),
        VoxelType.Sand => new Color(0.86f, 0.78f, 0.50f),
        _ => Colors.Magenta
    };
}

