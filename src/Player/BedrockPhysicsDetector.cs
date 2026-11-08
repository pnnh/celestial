using System;
using Celestial.Terrain;
using Celestial.Voxel;
using Godot;

namespace Celestial.Player;

/// <summary>
/// 普适物理基岩与 3D 实体检测器
/// 负责在 Layer 1 (Environment) 中将传统 3D 地形瓦片、巨石、树干、石柱等实体归类为不可破坏基岩
/// 并计算精准的网格吸附放置坐标
/// </summary>
public partial class BedrockPhysicsDetector : RefCounted
{
    public BedrockPhysicsDetector() { }
    /// <summary>
    /// 解析物理射线命中结果，判定命中语义并计算挖掘/放置目标坐标
    /// </summary>
    public InteractionRaycastResult AnalyzeHit(Godot.Collections.Dictionary rayResult, IHeightSampler? sampler)
    {
        if (rayResult == null || rayResult.Count == 0)
        {
            return InteractionRaycastResult.Empty;
        }

        Vector3 hitPoint = (Vector3)rayResult["position"];
        Vector3 hitNormal = (Vector3)rayResult["normal"];
        GodotObject? hitCollider = rayResult.ContainsKey("collider") ? (GodotObject)rayResult["collider"] : null;

        // 1. 计算挖掘采样点与放置采样点
        Vector3 digPoint = hitPoint - hitNormal * 0.4f;
        Vector3 placePoint = hitPoint + hitNormal * 0.4f;

        Vector3I digCoord = new(
            Mathf.FloorToInt(digPoint.X),
            Mathf.FloorToInt(digPoint.Y),
            Mathf.FloorToInt(digPoint.Z)
        );

        Vector3I placeCoord = new(
            Mathf.FloorToInt(placePoint.X),
            Mathf.FloorToInt(placePoint.Y),
            Mathf.FloorToInt(placePoint.Z)
        );

        // 2. 检查碰撞体类型
        bool isVoxelChunk = false;
        if (hitCollider is Node node)
        {
            // 若碰撞体的父级或自身为 VoxelChunk，说明命中了体素
            Node? curr = node;
            while (curr != null)
            {
                if (curr is VoxelChunk)
                {
                    isVoxelChunk = true;
                    break;
                }
                curr = curr.GetParent();
            }
        }

        // 3. 语义判定
        // 若不是体素 Chunk（属于 TerrainTile 或 3D 预制体），或者低于地表基岩高度，统一判定为 Bedrock
        if (!isVoxelChunk)
        {
            return new InteractionRaycastResult
            {
                Type = HitType.Bedrock,
                HitPoint = hitPoint,
                HitNormal = hitNormal,
                TargetVoxelCoord = digCoord,
                PlaceVoxelCoord = placeCoord,
                HitObject = hitCollider
            };
        }

        // 命中体素 Chunk 时，再校验是否严格低于 3D 大地形表面高度
        // 注意：地表及地表以上放置的方块 (digCoord.Y >= floor(terrainH)) 属于玩家自由建造层，必须允许正常挖掘！
        if (sampler != null)
        {
            float terrainH = sampler.GetHeight(digCoord.X + 0.5f, digCoord.Z + 0.5f);
            if (digCoord.Y < Mathf.FloorToInt(terrainH))
            {
                return new InteractionRaycastResult
                {
                    Type = HitType.Bedrock,
                    HitPoint = hitPoint,
                    HitNormal = hitNormal,
                    TargetVoxelCoord = digCoord,
                    PlaceVoxelCoord = placeCoord,
                    HitObject = hitCollider
                };
            }
        }

        // 命中普通可破坏体素方块
        return new InteractionRaycastResult
        {
            Type = HitType.Voxel,
            HitPoint = hitPoint,
            HitNormal = hitNormal,
            TargetVoxelCoord = digCoord,
            PlaceVoxelCoord = placeCoord,
            HitObject = hitCollider
        };
    }

    /// <summary>
    /// 提供给 GDScript / 自动化测试的 Variant 兼容分析方法
    /// </summary>
    public Godot.Collections.Dictionary AnalyzeHitDictionary(Godot.Collections.Dictionary rayResult, IslandHeightSampler? sampler)
    {
        var res = AnalyzeHit(rayResult, sampler);
        return new Godot.Collections.Dictionary
        {
            ["Type"] = (int)res.Type,
            ["HitPoint"] = res.HitPoint,
            ["HitNormal"] = res.HitNormal,
            ["TargetVoxelCoord"] = res.TargetVoxelCoord,
            ["PlaceVoxelCoord"] = res.PlaceVoxelCoord
        };
    }
}
