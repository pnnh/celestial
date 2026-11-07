using Godot;

namespace Celestial.Player;

/// <summary>
/// 射线拾取命中对象的物理语义类型
/// </summary>
public enum HitType
{
    None = 0,
    Voxel = 1,        // 可破坏/可放置体素方块
    Bedrock = 2       // 不可破坏基岩屏障（传统 3D 地形、3D 树木/建筑等预制体、或体素底部基岩）
}

/// <summary>
/// 标准化交互射线检测判定结果
/// </summary>
public readonly record struct InteractionRaycastResult
{
    public HitType Type { get; init; }
    public Vector3 HitPoint { get; init; }
    public Vector3 HitNormal { get; init; }
    public Vector3I TargetVoxelCoord { get; init; }   // 挖掘目标方块坐标（仅当 Type == Voxel 时有效）
    public Vector3I PlaceVoxelCoord { get; init; }    // 放置吸附目标方块坐标
    public GodotObject? HitObject { get; init; }

    public static InteractionRaycastResult Empty => new()
    {
        Type = HitType.None,
        HitPoint = Vector3.Zero,
        HitNormal = Vector3.Up,
        TargetVoxelCoord = Vector3I.Zero,
        PlaceVoxelCoord = Vector3I.Zero,
        HitObject = null
    };
}

