using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 树木物种生态与碰撞配置
/// </summary>
public record struct TreeVariantConfig
{
    public string VariantId { get; init; } = "spruce";
    public string DisplayName { get; init; } = "云杉";
    public string MeshPath { get; init; } = "";
    public float MinElevation { get; init; } = 5.0f;
    public float MaxElevation { get; init; } = 70.0f;
    public float MinSlopeY { get; init; } = 0.75f;           // 坡度法线 Y 分量下限 (约 41 度)
    public float RelativeWeight { get; init; } = 1.0f;
    public float TrunkRadius { get; init; } = 0.25f;         // 树干圆柱碰撞体半径 (米)
    public float TrunkHeight { get; init; } = 2.6f;          // 树干圆柱碰撞体高度 (米)
    public float ScaleRangeMin { get; init; } = 0.85f;
    public float ScaleRangeMax { get; init; } = 1.25f;

    public Mesh? LoadedMesh { get; set; }
    public Material? LoadedMaterial { get; set; }

    public TreeVariantConfig() { }
}

/// <summary>
/// 地表花草灌木配置
/// </summary>
public record struct FloraVariantConfig
{
    public string VariantId { get; init; } = "wild_grass";
    public string DisplayName { get; init; } = "野草";
    public string MeshPath { get; init; } = "";
    public float MinElevation { get; init; } = 2.5f;
    public float MaxElevation { get; init; } = 40.0f;
    public float MinSlopeY { get; init; } = 0.82f;
    public float ClusterRadius { get; init; } = 4.0f;
    public int ClusterDensity { get; init; } = 6;

    public Mesh? LoadedMesh { get; set; }
    public Material? LoadedMaterial { get; set; }

    public FloraVariantConfig() { }
}

