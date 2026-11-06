using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 传统 3D 静态地形节点（包含视觉 Mesh 与静态物理碰撞体）
/// </summary>
public partial class StaticTerrain : Node3D
{
    [Export] public float BaseHeight = 6.0f;
    [Export] public float ValleyDepth = 4.0f;
    [Export] public float ValleyRadius = 24.0f;
    [Export] public int TerrainExtent = 48; // -48 到 +48 米范围
    [Export] public float GridStep = 1.0f;

    private MeshInstance3D _meshInstance = null!;
    private StaticBody3D _staticBody = null!;
    private CollisionShape3D _collisionShape = null!;

    public IHeightSampler HeightSampler { get; private set; } = null!;

    public override void _Ready()
    {
        HeightSampler = new SimpleProceduralHeightmap(BaseHeight, ValleyDepth, ValleyRadius);
        BuildTerrainMeshAndCollision();
    }

    /// <summary>
    /// 生成传统地形的网格渲染与物理碰撞网格
    /// </summary>
    public void BuildTerrainMeshAndCollision()
    {
        if (HeightSampler is not SimpleProceduralHeightmap procedural)
        {
            return;
        }

        // 1. 生成视觉 Mesh
        ArrayMesh mesh = procedural.GenerateMesh(-TerrainExtent, TerrainExtent, -TerrainExtent, TerrainExtent, GridStep);

        Texture2D? terrainTex = null;
        if (ResourceLoader.Exists("res://assets/textures/ludo/lush_grass_soil.png"))
        {
            terrainTex = ResourceLoader.Load<Texture2D>("res://assets/textures/ludo/lush_grass_soil.png");
        }

        // 设置传统 3D 地形材质（若存在草地纹理则启用无缝平铺，与体素草地方块风格完全统一）
        StandardMaterial3D material = new()
        {
            AlbedoTexture = terrainTex,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            VertexColorUseAsAlbedo = true,
            Roughness = 0.9f,
            Metallic = 0.05f
        };
        mesh.SurfaceSetMaterial(0, material);

        _meshInstance = new MeshInstance3D
        {
            Name = "TerrainMesh",
            Mesh = mesh
        };
        AddChild(_meshInstance);

        // 2. 生成物理静态碰撞体 (Collision Layer 1: Environment)
        _staticBody = new StaticBody3D
        {
            Name = "TerrainStaticBody",
            CollisionLayer = 1,
            CollisionMask = 0
        };

        ConcavePolygonShape3D shape = new();
        shape.SetFaces(mesh.GetFaces());

        _collisionShape = new CollisionShape3D
        {
            Name = "TerrainCollisionShape",
            Shape = shape
        };

        _staticBody.AddChild(_collisionShape);
        AddChild(_staticBody);

        GD.Print($"StaticTerrain initialized. Range: [-{TerrainExtent}, {TerrainExtent}], Faces: {mesh.GetFaces().Length / 3}");
    }
}

