using System;
using System.Collections.Generic;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 单个植被变体定义
/// </summary>
public class FoliageVariantInfo
{
    public string VariantId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string GlbPath { get; init; } = "";
    public bool IsTree { get; init; } = true;
    public int TargetCount { get; init; } = 300;
    public float MinElevation { get; init; } = 5.0f;
    public float MaxElevation { get; init; } = 40.0f;
    public float MinSlopeY { get; init; } = 0.80f;
    public float BaseScale { get; init; } = 1.0f;
    public float ScaleVariation { get; init; } = 0.25f;
    public float TrunkRadius { get; init; } = 0.25f;
    public float TrunkHeight { get; init; } = 2.4f;

    public Mesh? Mesh { get; set; }
    public Material? Material { get; set; }
    public float BaseOffsetY { get; set; }
    public MultiMeshInstance3D? MeshInstance { get; set; }
    public readonly List<Transform3D> InstanceTransforms = new();
    public readonly List<float> GroundHeights = new();
}

/// <summary>
/// 开放世界批量程序化植被散布管理器
/// 支持多树种（橡木、云杉、白桦）与多种经典花草（野草、高草、向日葵、虞美人、蒲公英）GPU 实例化并行渲染与生态分层
/// </summary>
[Tool]
public partial class FoliageScatter : MultiMeshInstance3D
{
    [Export] public int InstanceCount { get; set; } = 800;
    [Export] public float ScatterRadius { get; set; } = 350.0f;
    [Export] public float MinSpawnHeight { get; set; } = 2.5f;
    [Export] public float MaxSpawnHeight { get; set; } = 65.0f;
    [Export] public float MinSlopeNormalY { get; set; } = 0.78f;
    [Export] public int Seed { get; set; } = 2026;

    public IHeightSampler? HeightSampler { get; set; }

    private readonly List<Aabb> _exclusionZones = new();
    private readonly List<FoliageVariantInfo> _variants = new();
    private Node3D? _colliderContainer;

    // 统计缓存
    private int _totalValidInstances = 0;
    private int _totalRequestedInstances = 0;
    private int _totalTreeInstances = 0;
    private int _totalFloraInstances = 0;
    private float _oakAvgHeight = 0.0f;
    private float _spruceAvgHeight = 0.0f;
    private int _waterSpawns = 0;
    private int _exclusionViolations = 0;
    private int _waterCandidatesRejected = 0;
    private int _exclusionCandidatesRejected = 0;
    private int _treeCollidersCount = 0;

    public override void _Ready()
    {
        // 1. 获取地形高度采样器
        var terrainManager = GetTree()?.Root?.FindChild("TiledTerrainManager", true, false) as TiledTerrainManager;
        HeightSampler ??= terrainManager?.HeightSampler ?? new IslandHeightSampler();

        // 2. 初始化碰撞体挂载容器
        _colliderContainer = new Node3D { Name = "TreeTrunkColliders" };
        AddChild(_colliderContainer);

        // 3. 构建多物种配置与网格
        SetupVariants();

        // 4. 采集排除区域并执行生态分层散布
        CollectExclusionZones();
        PopulateFoliage();
    }

    private void SetupVariants()
    {
        _variants.Clear();

        // === 树木变体 (Trees) ===
        // 1. 橡木：主打平原、低丘陵与河岸边缘
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "oak",
            DisplayName = "橡木",
            GlbPath = "res://assets/models/trees/oak/oak.glb",
            IsTree = true,
            TargetCount = 520,
            MinElevation = 3.5f,
            MaxElevation = 28.0f,
            MinSlopeY = 0.80f,
            BaseScale = 5.0f,
            ScaleVariation = 0.20f,
            TrunkRadius = 0.35f,
            TrunkHeight = 2.4f
        });

        // 2. 白桦：主打山林交界带与丘陵坡地
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "birch",
            DisplayName = "白桦",
            GlbPath = "res://assets/models/trees/birch/birch.glb",
            IsTree = true,
            TargetCount = 300,
            MinElevation = 10.0f,
            MaxElevation = 38.0f,
            MinSlopeY = 0.78f,
            BaseScale = 5.2f,
            ScaleVariation = 0.20f,
            TrunkRadius = 0.22f,
            TrunkHeight = 2.6f
        });

        // 3. 云杉：主打高山冷杉林、山脊与高地
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "spruce",
            DisplayName = "云杉",
            GlbPath = "res://assets/models/trees/spruce/spruce.glb",
            IsTree = true,
            TargetCount = 350,
            MinElevation = 22.0f,
            MaxElevation = 65.0f,
            MinSlopeY = 0.75f,
            BaseScale = 5.5f,
            ScaleVariation = 0.22f,
            TrunkRadius = 0.28f,
            TrunkHeight = 2.8f
        });

        // === 花草变体 (Flora & Bushes) ===
        // 4. 野草：全岛广泛分布于草地表层
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "wild_grass",
            DisplayName = "野草",
            GlbPath = "res://assets/models/flora/wild_grass.glb",
            IsTree = false,
            TargetCount = 600,
            MinElevation = 2.5f,
            MaxElevation = 45.0f,
            MinSlopeY = 0.82f,
            BaseScale = 1.3f,
            ScaleVariation = 0.25f
        });

        // 5. 高草：平原草甸灌丛
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "tall_grass",
            DisplayName = "高草",
            GlbPath = "res://assets/models/flora/tall_grass.glb",
            IsTree = false,
            TargetCount = 300,
            MinElevation = 2.5f,
            MaxElevation = 35.0f,
            MinSlopeY = 0.82f,
            BaseScale = 1.2f,
            ScaleVariation = 0.25f
        });

        // 6. 向日葵：平原阳光充沛地带与湖畔
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "sunflower",
            DisplayName = "向日葵",
            GlbPath = "res://assets/models/flora/sunflower.glb",
            IsTree = false,
            TargetCount = 150,
            MinElevation = 3.5f,
            MaxElevation = 25.0f,
            MinSlopeY = 0.85f,
            BaseScale = 1.5f,
            ScaleVariation = 0.20f
        });

        // 7. 虞美人：花海与缓坡草地
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "poppy",
            DisplayName = "虞美人",
            GlbPath = "res://assets/models/flora/poppy.glb",
            IsTree = false,
            TargetCount = 150,
            MinElevation = 4.0f,
            MaxElevation = 35.0f,
            MinSlopeY = 0.82f,
            BaseScale = 0.9f,
            ScaleVariation = 0.25f
        });

        // 8. 蒲公英：微风草地与田野
        _variants.Add(new FoliageVariantInfo
        {
            VariantId = "dandelion",
            DisplayName = "蒲公英",
            GlbPath = "res://assets/models/flora/dandelion.glb",
            IsTree = false,
            TargetCount = 150,
            MinElevation = 3.0f,
            MaxElevation = 30.0f,
            MinSlopeY = 0.82f,
            BaseScale = 0.8f,
            ScaleVariation = 0.25f
        });

        // 为每个变体加载资产或构建回退网格，并创建 MultiMeshInstance3D
        for (int i = 0; i < _variants.Count; i++)
        {
            var v = _variants[i];
            var (mesh, mat, offsetY) = LoadVariantAsset(v.GlbPath, v.IsTree);
            v.Mesh = mesh;
            v.Material = mat;
            v.BaseOffsetY = offsetY;

            var mm = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = false,
                Mesh = mesh,
                InstanceCount = v.TargetCount
            };

            if (i == 0)
            {
                // 主 MultiMeshInstance3D (this) 挂载第一个变体（橡木），保持向下兼容
                this.Multimesh = mm;
                if (mat != null) this.MaterialOverride = mat;
                v.MeshInstance = this;
            }
            else
            {
                var subInst = new MultiMeshInstance3D
                {
                    Name = $"{v.VariantId}_MultiMesh",
                    Multimesh = mm
                };
                if (mat != null) subInst.MaterialOverride = mat;
                AddChild(subInst);
                v.MeshInstance = subInst;
            }

            // GPU 性能调优：视距裁剪 (Visibility Range) 与阴影分离 (Shadow Casting)
            if (!v.IsTree)
            {
                // 1. 地表野花与草丛：关闭动态平行光阴影投射（消除数百万阴影多边形重绘），仅在 90m 视距内渲染
                v.MeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                v.MeshInstance.VisibilityRangeEnd = 90.0f;
                v.MeshInstance.VisibilityRangeEndMargin = 15.0f;
                v.MeshInstance.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
            }
            else
            {
                // 2. 树木：保留主干与树冠阴影投射，超出 260m 浓雾线后硬件自动渐隐剔除
                v.MeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
                v.MeshInstance.VisibilityRangeEnd = 260.0f;
                v.MeshInstance.VisibilityRangeEndMargin = 25.0f;
                v.MeshInstance.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
            }
        }
    }

    private static (Mesh mesh, Material? material, float baseOffsetY) LoadVariantAsset(string glbPath, bool isTree)
    {
        if (ResourceLoader.Exists(glbPath))
        {
            var scene = GD.Load<PackedScene>(glbPath);
            if (scene != null)
            {
                var root = scene.Instantiate();
                var mi = root.FindChild("geometry_0", true, false) as MeshInstance3D
                         ?? (root as MeshInstance3D);
                if (mi?.Mesh != null)
                {
                    var mesh = mi.Mesh;
                    var aabb = mesh.GetAabb();
                    float baseOffsetY = -aabb.Position.Y;

                    var origMat = mesh.SurfaceGetMaterial(0) as BaseMaterial3D
                                  ?? mi.GetSurfaceOverrideMaterial(0) as BaseMaterial3D;
                    StandardMaterial3D toonMat;
                    if (origMat != null)
                    {
                        toonMat = (StandardMaterial3D)origMat.Duplicate();
                    }
                    else
                    {
                        toonMat = new StandardMaterial3D();
                    }

                    toonMat.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon;
                    toonMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon;
                    toonMat.Roughness = 0.85f;
                    if (!isTree)
                    {
                        toonMat.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // 花草双面着色
                    }

                    root.QueueFree();
                    return (mesh, toonMat, baseOffsetY);
                }
                root.QueueFree();
            }
        }

        // 若模型缺失，生成低模回退
        return GenerateFallbackMesh(isTree);
    }

    private static (Mesh mesh, Material material, float baseOffsetY) GenerateFallbackMesh(bool isTree)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var mat = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Roughness = 0.85f,
            DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon,
            SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon
        };

        if (isTree)
        {
            // 简单圆锥形树木
            Color trunkColor = new(0.42f, 0.28f, 0.16f);
            Color leafColor = new(0.24f, 0.62f, 0.28f);
            st.SetColor(trunkColor);
            st.AddVertex(new Vector3(-0.2f, 0, -0.2f));
            st.AddVertex(new Vector3(0.2f, 0, -0.2f));
            st.AddVertex(new Vector3(0, 1.5f, 0));

            st.SetColor(leafColor);
            st.AddVertex(new Vector3(-1.0f, 1.0f, -1.0f));
            st.AddVertex(new Vector3(1.0f, 1.0f, -1.0f));
            st.AddVertex(new Vector3(0, 3.5f, 0));
        }
        else
        {
            // 简单交叉草叶
            mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            Color grassColor = new(0.35f, 0.75f, 0.25f);
            st.SetColor(grassColor);
            st.AddVertex(new Vector3(-0.3f, 0, 0));
            st.AddVertex(new Vector3(0.3f, 0, 0));
            st.AddVertex(new Vector3(0, 0.8f, 0));
        }

        var mesh = st.Commit();
        return (mesh, mat, 0.0f);
    }

    private void CollectExclusionZones()
    {
        _exclusionZones.Clear();
        // 避开中心初始区域与古图腾遗迹
        _exclusionZones.Add(new Aabb(new Vector3(-8, 5, -24), new Vector3(32, 30, 32)));

        var root = GetTree()?.Root;
        if (root == null) return;

        // 避开 QuarryVolume 等局部体素卷
        var volumes = root.FindChildren("*", "VoxelVolume", true, false);
        foreach (var node in volumes)
        {
            if (node is Celestial.Voxel.VoxelVolume vol)
            {
                _exclusionZones.Add(vol.WorldBounds.Grow(4.0f));
            }
        }
    }

    private bool IsInExclusionZone(float x, float y, float z)
    {
        var pt = new Vector3(x, y, z);
        for (int i = 0; i < _exclusionZones.Count; i++)
        {
            if (_exclusionZones[i].HasPoint(pt)) return true;
        }
        return false;
    }

    public void PopulateFoliage()
    {
        if (HeightSampler == null) return;

        var rng = new Random(Seed);
        _totalValidInstances = 0;
        _totalRequestedInstances = 0;
        _totalTreeInstances = 0;
        _totalFloraInstances = 0;
        _oakAvgHeight = 0.0f;
        _spruceAvgHeight = 0.0f;
        _waterSpawns = 0;
        _exclusionViolations = 0;
        _treeCollidersCount = 0;

        float oakTotalH = 0.0f;
        int oakCount = 0;
        float spruceTotalH = 0.0f;
        int spruceCount = 0;

        foreach (var v in _variants)
        {
            v.InstanceTransforms.Clear();
            v.GroundHeights.Clear();
            _totalRequestedInstances += v.TargetCount;
            if (v.MeshInstance?.Multimesh == null) continue;

            int validForVariant = 0;
            int attempts = 0;
            int maxAttempts = v.TargetCount * 8;

            while (validForVariant < v.TargetCount && attempts < maxAttempts)
            {
                attempts++;

                // 极坐标均匀散布
                double angle = rng.NextDouble() * Math.PI * 2.0;
                double dist = Math.Sqrt(rng.NextDouble()) * ScatterRadius;

                float wx = (float)(Math.Cos(angle) * dist);
                float wz = (float)(Math.Sin(angle) * dist);

                float wy = HeightSampler.GetHeight(wx, wz);
                Vector3 normal = HeightSampler.GetNormal(wx, wz);

                // 水下生长严格检查
                if (wy < MinSpawnHeight)
                {
                    _waterCandidatesRejected++;
                    continue;
                }

                // 生态高度与坡度过滤 (兼顾物种偏好与全局配置上限)
                float minH = Math.Max(v.MinElevation, MinSpawnHeight);
                float maxH = Math.Min(v.MaxElevation, MaxSpawnHeight);
                float minSlope = Math.Max(v.MinSlopeY, MinSlopeNormalY);

                if (wy < minH || wy > maxH || normal.Y < minSlope)
                {
                    continue;
                }

                // 避让体素采矿区与古遗迹
                if (IsInExclusionZone(wx, wy, wz))
                {
                    _exclusionCandidatesRejected++;
                    continue;
                }

                // 随机旋转与尺寸微调
                float scaleMul = (float)(1.0 + (rng.NextDouble() * 2.0 - 1.0) * v.ScaleVariation);
                float scale = v.BaseScale * scaleMul;
                float rotY = (float)(rng.NextDouble() * Math.PI * 2.0);

                var basis = Basis.FromEuler(new Vector3(0, rotY, 0));
                basis = basis.Scaled(new Vector3(scale, scale, scale));

                // 树木或花草基底着地对齐：通过 BaseOffsetY 确保网格底部严丝合缝贴地
                float spawnY = wy + v.BaseOffsetY * scale;
                var transform = new Transform3D(basis, new Vector3(wx, spawnY, wz));

                v.MeshInstance.Multimesh.SetInstanceTransform(validForVariant, transform);
                v.InstanceTransforms.Add(transform);
                v.GroundHeights.Add(wy);

                // 若为树木，生成 StaticBody3D 与 CylinderShape3D 物理碰撞体
                if (v.IsTree && _colliderContainer != null)
                {
                    var treeCol = new StaticBody3D
                    {
                        Name = $"{v.VariantId}_Col_{validForVariant}",
                        CollisionLayer = 1, // Layer 1 (Environment)
                        CollisionMask = 0,
                        Position = new Vector3(wx, wy + v.TrunkHeight * 0.5f, wz)
                    };
                    var colShape = new CollisionShape3D
                    {
                        Shape = new CylinderShape3D
                        {
                            Radius = v.TrunkRadius,
                            Height = v.TrunkHeight
                        }
                    };
                    treeCol.AddChild(colShape);
                    _colliderContainer.AddChild(treeCol);
                    _treeCollidersCount++;
                }

                // 生态统计追踪
                if (v.VariantId == "oak")
                {
                    oakTotalH += wy;
                    oakCount++;
                }
                else if (v.VariantId == "spruce")
                {
                    spruceTotalH += wy;
                    spruceCount++;
                }

                if (v.IsTree) _totalTreeInstances++;
                else _totalFloraInstances++;

                validForVariant++;
                _totalValidInstances++;
            }

            v.MeshInstance.Multimesh.VisibleInstanceCount = validForVariant;
        }

        if (oakCount > 0) _oakAvgHeight = oakTotalH / oakCount;
        if (spruceCount > 0) _spruceAvgHeight = spruceTotalH / spruceCount;

        GD.Print($"[FoliageScatter] 植被散布完成: 树木 {_totalTreeInstances} 株, 花草 {_totalFloraInstances} 株, 碰撞体 {_treeCollidersCount} 个, 橡木均高 {_oakAvgHeight:F1}m, 云杉均高 {_spruceAvgHeight:F1}m");
    }

    /// <summary>
    /// 获取植被散布统计数据（供自动化测试与调试面板调用）
    /// </summary>
    public Godot.Collections.Dictionary GetScatterSummary()
    {
        float minH = float.MaxValue;
        float maxH = float.MinValue;
        int waterSpawns = 0;
        int exclusionViolations = 0;

        foreach (var v in _variants)
        {
            for (int i = 0; i < v.InstanceTransforms.Count; i++)
            {
                var t = v.InstanceTransforms[i];
                float h = i < v.GroundHeights.Count ? v.GroundHeights[i] : t.Origin.Y;
                if (h < minH) minH = h;
                if (h > maxH) maxH = h;
                if (h < MinSpawnHeight) waterSpawns++;
                if (IsInExclusionZone(t.Origin.X, h, t.Origin.Z)) exclusionViolations++;
            }
        }

        return new Godot.Collections.Dictionary
        {
            // 向下兼容 keys
            { "valid_count", _totalValidInstances },
            { "requested_count", _totalRequestedInstances },
            { "min_height", minH == float.MaxValue ? 0.0f : minH },
            { "max_height", maxH == float.MinValue ? 0.0f : maxH },
            { "water_spawns", waterSpawns },

            // 多变体生态 keys
            { "total_trees", _totalTreeInstances },
            { "total_floras", _totalFloraInstances },
            { "total_instances", _totalValidInstances },
            { "oak_avg_height", _oakAvgHeight },
            { "spruce_avg_height", _spruceAvgHeight },
            { "exclusion_violations", exclusionViolations },
            { "tree_colliders_count", _treeCollidersCount },
            { "water_candidates_rejected", _waterCandidatesRejected },
            { "exclusion_candidates_rejected", _exclusionCandidatesRejected }
        };
    }
}
