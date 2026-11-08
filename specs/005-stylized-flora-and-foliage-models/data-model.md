# Phase 1: Data Model & Asset Hierarchy

**Feature**: `005-stylized-flora-and-foliage-models`  
**Date**: 2026-11-08  
**Status**: Completed

---

## 1. 植被配置模型 (C# Data Structures)

### 1.1 树木变体配置 (`TreeVariantConfig`)
描述单个树木物种的网格外观、碰撞尺寸与生态高程区间。

```csharp
namespace Celestial.Terrain;

public record struct TreeVariantConfig
{
    public string VariantId { get; init; }           // "oak", "spruce", "birch"
    public string DisplayName { get; init; }         // "橡木", "云杉", "白桦"
    public string MeshPath { get; init; }            // "res://assets/models/trees/oak.glb"
    public float MinElevation { get; init; }         // 最低生长绝对高度（米）
    public float MaxElevation { get; init; }         // 最高生长绝对高度（米）
    public float MinSlopeY { get; init; }            // 坡度法线 Y 分量下限（如 0.75 对应约 41 度）
    public float RelativeWeight { get; init; }       // 该高度区间的生成权重（0.0 ~ 1.0）
    public float TrunkRadius { get; init; }          // 树干圆柱碰撞体半径（米，推荐 0.2 ~ 0.35m）
    public float TrunkHeight { get; init; }          // 树干圆柱碰撞体高度（米，推荐 2.0 ~ 3.2m）
    public Vector3 ScaleRange { get; init; }         // 随机缩放扰动区间，如 Vector3(0.85f, 1.25f, 0)
}
```

### 1.2 花草灌木变体配置 (`FloraVariantConfig`)
描述地表微观花草植物的网格、贴图与聚类散布参数。

```csharp
namespace Celestial.Terrain;

public record struct FloraVariantConfig
{
    public string VariantId { get; init; }           // "wild_grass", "tall_grass", "sunflower", "poppy", "dandelion"
    public string DisplayName { get; init; }         // "野草", "高草", "向日葵", "虞美人", "蒲公英"
    public string MeshPath { get; init; }            // "res://assets/models/flora/poppy.glb"
    public float MinElevation { get; init; }         // 最低生长高度
    public float MaxElevation { get; init; }         // 最高生长高度
    public float MinSlopeY { get; init; }            // 坡度法线下限（平缓地表通常 > 0.85）
    public float ClusterRadius { get; init; }        // 聚类生长半径（米）
    public int ClusterDensity { get; init; }         // 每个簇的花草数量
}
```

### 1.3 大世界混合植被散布总配置 (`FoliageScatterConfig`)
整合多种树种与花草的全岛散布参数容器。

```csharp
namespace Celestial.Terrain;

public record struct FoliageScatterConfig
{
    public int RandomSeed { get; init; }             // 随机数种子 (默认 1337)
    public int TotalTreeCount { get; init; }         // 全岛大树总规划数量 (默认 1000)
    public int TotalFloraCount { get; init; }        // 全岛花草总规划数量 (默认 2000)
    public float WaterClearance { get; init; }       // 海平面避让高度 (默认 0.5m)
    public float ExclusionMargin { get; init; }      // VoxelVolume 外延安全缓冲带 (默认 4.0m)
    public TreeVariantConfig[] Trees { get; init; }  // 树木变体列表
    public FloraVariantConfig[] Floras { get; init; }// 花草变体列表
}
```

---

## 2. 预制体节点体系结构 (Prefab Scene Node Trees)

### 2.1 树木预制体 (`res://scenes/prefabs/trees/{name}_tree.tscn`)
规范化预制体层级结构，杜绝孤立碰撞体与缩放警告：

```text
SpruceTree (Node3D, Transform.Scale = (1, 1, 1))
 ├── VisualMesh (MeshInstance3D, 挂载 Ludo 生成的赛璐璐材质与网格)
 └── StaticBody3D (CollisionLayer: 1, CollisionMask: 0)
       └── CollisionShape3D (Transform.Scale = (1, 1, 1), Transform.Position.Y = TrunkHeight / 2)
             └── shape: CylinderShape3D
                   ├── radius = TrunkRadius (0.22m)
                   └── height = TrunkHeight (2.6m)
```

**关键设计约束**:
1. `StaticBody3D` 必须位于 `CollisionShape3D` 之上；
2. `CollisionShape3D` 绝不使用非等比缩放，尺寸完全由 `CylinderShape3D.radius` 与 `CylinderShape3D.height` 决定；
3. 树干位于 Collision Layer 1（Environment），支持被 `BedrockPhysicsDetector.cs` 识别为不可破坏基岩，并被 `VoxelInteractor.cs` 识别为放置障碍物；
4. 树叶部分保留无碰撞，允许角色与摄像机在树冠下自由穿行。

---

## 3. 本地存储与资产归档目录规范

```text
celestial/
├── assets/
│   ├── models/
│   │   ├── trees/
│   │   │   ├── oak/               # 橡木模型与贴图
│   │   │   │   ├── oak.glb
│   │   │   │   └── textures/
│   │   │   ├── spruce/            # 云杉模型与贴图
│   │   │   │   ├── spruce.glb
│   │   │   │   └── textures/
│   │   │   └── birch/             # 白桦模型与贴图
│   │   │       ├── birch.glb
│   │   │       └── textures/
│   │   └── flora/
│   │       ├── wild_grass.glb     # 野草
│   │       ├── tall_grass.glb     # 高草
│   │       ├── sunflower.glb      # 向日葵
│   │       ├── poppy.glb          # 虞美人
│   │       └── dandelion.glb      # 蒲公英
│   └── textures/ludo/             # 原始手绘贴图素材
└── scenes/prefabs/trees/          # 标准化带碰撞树木预制体
    ├── oak_tree.tscn
    ├── spruce_tree.tscn
    └── birch_tree.tscn
```
- 所有 `.glb` 与高分辨率 `.png` 自动匹配根目录 `.gitattributes`，由 **Git LFS** 托管。

