using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 植被散布参数配置
/// </summary>
public record struct FoliageScatterConfig
{
    public int InstanceCount { get; init; } = 800;
    public float ScatterRadius { get; init; } = 350.0f;
    public float MinSpawnHeight { get; init; } = 2.5f;   // 避开海水与沙滩
    public float MaxSpawnHeight { get; init; } = 48.0f;  // 避开最高绝壁
    public float MinSlopeNormalY { get; init; } = 0.80f; // 避开陡峭悬崖
    public Vector2 ScaleRange { get; init; } = new(0.85f, 1.45f);
    public int Seed { get; init; } = 2026;

    public FoliageScatterConfig() { }
}

/// <summary>
/// 开放世界批量程序化植被散布组件
/// 基于 MultiMeshInstance3D 实现 GPU 单批次高效实例化渲染千株卡通树木与草丛
/// </summary>
[Tool]
public partial class FoliageScatter : MultiMeshInstance3D
{
    [Export] public int InstanceCount { get; set; } = 800;
    [Export] public float ScatterRadius { get; set; } = 350.0f;
    [Export] public float MinSpawnHeight { get; set; } = 2.5f;
    [Export] public float MaxSpawnHeight { get; set; } = 48.0f;
    [Export] public float MinSlopeNormalY { get; set; } = 0.80f;
    [Export] public int Seed { get; set; } = 2026;

    public IHeightSampler? HeightSampler { get; set; }

    private readonly Godot.Collections.Array<Transform3D> _instanceTransforms = new();
    private readonly System.Collections.Generic.List<Aabb> _exclusionZones = new();
    public Godot.Collections.Array<Transform3D> InstanceTransforms => _instanceTransforms;

    public override void _Ready()
    {
        // 1. 获取地形高度采样器
        var terrainManager = GetTree()?.Root?.FindChild("TiledTerrainManager", true, false) as TiledTerrainManager;
        HeightSampler ??= terrainManager?.HeightSampler ?? new IslandHeightSampler();

        // 2. 生成植被网格与材质
        BuildFoliageMesh();

        // 3. 散布实例
        PopulateFoliage();
    }

    private void BuildFoliageMesh()
    {
        // 构建赛璐璐卡通树木复合几何体 (低模树干 + 锥形树冠)
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // 树木材质：双层赛璐璐着色 (棕色树干 + 翠绿树冠)
        var treeMat = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Roughness = 0.9f,
            DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon,
            SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon
        };

        // 1. 树干 (Cylinder): 半径 0.25m, 高 1.8m
        Color trunkColor = new(0.42f, 0.28f, 0.16f);
        int trunkSides = 6;
        for (int i = 0; i < trunkSides; i++)
        {
            float a1 = i * MathF.PI * 2.0f / trunkSides;
            float a2 = (i + 1) * MathF.PI * 2.0f / trunkSides;

            Vector3 b1 = new(MathF.Cos(a1) * 0.25f, 0.0f, MathF.Sin(a1) * 0.25f);
            Vector3 b2 = new(MathF.Cos(a2) * 0.25f, 0.0f, MathF.Sin(a2) * 0.25f);
            Vector3 t1 = new(MathF.Cos(a1) * 0.18f, 1.8f, MathF.Sin(a1) * 0.18f);
            Vector3 t2 = new(MathF.Cos(a2) * 0.18f, 1.8f, MathF.Sin(a2) * 0.18f);

            st.SetColor(trunkColor);
            st.SetNormal(new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1)));
            st.AddVertex(b1);
            st.SetNormal(new Vector3(MathF.Cos(a2), 0, MathF.Sin(a2)));
            st.AddVertex(b2);
            st.SetNormal(new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1)));
            st.AddVertex(t1);

            st.SetColor(trunkColor);
            st.SetNormal(new Vector3(MathF.Cos(a2), 0, MathF.Sin(a2)));
            st.AddVertex(b2);
            st.SetNormal(new Vector3(MathF.Cos(a2), 0, MathF.Sin(a2)));
            st.AddVertex(t2);
            st.SetNormal(new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1)));
            st.AddVertex(t1);
        }

        // 2. 树冠三层塔伞 (Canopy Cones): 翠绿赛璐璐
        Color foliageColor = new(0.24f, 0.62f, 0.28f);
        float[] layerHeights = [1.5f, 2.7f, 3.8f];
        float[] layerRadii = [1.8f, 1.35f, 0.85f];
        float coneHeight = 1.6f;

        for (int layer = 0; layer < 3; layer++)
        {
            float baseH = layerHeights[layer];
            float r = layerRadii[layer];
            Vector3 apex = new(0.0f, baseH + coneHeight, 0.0f);

            int foliageSides = 7;
            for (int i = 0; i < foliageSides; i++)
            {
                float a1 = i * MathF.PI * 2.0f / foliageSides;
                float a2 = (i + 1) * MathF.PI * 2.0f / foliageSides;

                Vector3 v1 = new(MathF.Cos(a1) * r, baseH, MathF.Sin(a1) * r);
                Vector3 v2 = new(MathF.Cos(a2) * r, baseH, MathF.Sin(a2) * r);

                Vector3 faceNorm = (v1 + v2) * 0.5f - apex;
                faceNorm.Y = 0.5f;
                faceNorm = faceNorm.Normalized();

                st.SetColor(foliageColor);
                st.SetNormal(faceNorm);
                st.AddVertex(v1);
                st.AddVertex(v2);
                st.AddVertex(apex);
            }
        }

        ArrayMesh mesh = st.Commit();
        mesh.SurfaceSetMaterial(0, treeMat);

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = false,
            Mesh = mesh,
            InstanceCount = InstanceCount
        };

        Multimesh = multiMesh;
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
        if (Multimesh == null || HeightSampler == null) return;

        CollectExclusionZones();
        _instanceTransforms.Clear();
        var rng = new Random(Seed);
        int validInstances = 0;
        int attempts = 0;
        int maxAttempts = InstanceCount * 4;

        while (validInstances < InstanceCount && attempts < maxAttempts)
        {
            attempts++;

            // 极坐标均匀散布
            double angle = rng.NextDouble() * Math.PI * 2.0;
            double dist = Math.Sqrt(rng.NextDouble()) * ScatterRadius;

            float wx = (float)(Math.Cos(angle) * dist);
            float wz = (float)(Math.Sin(angle) * dist);

            float wy = HeightSampler.GetHeight(wx, wz);
            Vector3 normal = HeightSampler.GetNormal(wx, wz);

            // 生态过滤：水下/沙滩/悬崖不生长
            if (wy < MinSpawnHeight || wy > MaxSpawnHeight || normal.Y < MinSlopeNormalY)
            {
                continue;
            }

            // 避让体素采矿区与古遗迹：严禁在可挖掘体素区域生成静态 3D 树木，避免嵌入方块与悬空
            if (IsInExclusionZone(wx, wy, wz))
            {
                continue;
            }

            // 随机旋转与尺寸微调
            float scale = (float)(0.85 + rng.NextDouble() * 0.55);
            float rotY = (float)(rng.NextDouble() * Math.PI * 2.0);

            var basis = Basis.FromEuler(new Vector3(0, rotY, 0));
            basis = basis.Scaled(new Vector3(scale, scale, scale));

            var transform = new Transform3D(basis, new Vector3(wx, wy, wz));
            Multimesh.SetInstanceTransform(validInstances, transform);
            _instanceTransforms.Add(transform);
            validInstances++;
        }

        Multimesh.VisibleInstanceCount = validInstances;
    }

    /// <summary>
    /// 获取植被散布统计数据（供自动化测试与调试面板调用）
    /// </summary>
    public Godot.Collections.Dictionary GetScatterSummary()
    {
        float minH = float.MaxValue;
        float maxH = float.MinValue;
        int waterCount = 0;

        foreach (var t in _instanceTransforms)
        {
            float y = t.Origin.Y;
            if (y < minH) minH = y;
            if (y > maxH) maxH = y;
            if (y < MinSpawnHeight) waterCount++;
        }

        return new Godot.Collections.Dictionary
        {
            { "valid_count", _instanceTransforms.Count },
            { "requested_count", InstanceCount },
            { "min_height", minH == float.MaxValue ? 0.0f : minH },
            { "max_height", maxH == float.MinValue ? 0.0f : maxH },
            { "water_spawns", waterCount }
        };
    }
}
