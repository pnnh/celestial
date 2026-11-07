using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 64m 单体地形瓦片节点
/// 负责单个切片的渲染网格绑定、物理碰撞体管理与对象池复用
/// </summary>
public partial class TerrainTile : Node3D
{
    public TerrainTileCoord Coord { get; private set; }
    public LODLevel CurrentLOD { get; private set; } = LODLevel.Unloaded;
    public bool IsActive { get; private set; }

    private MeshInstance3D _meshInstance = null!;
    private StaticBody3D _staticBody = null!;
    private CollisionShape3D _collisionShape = null!;
    private ConcavePolygonShape3D? _concaveShape;

    public override void _Ready()
    {
        InitializeNodes();
    }

    private void InitializeNodes()
    {
        if (_meshInstance != null) return;

        _meshInstance = new MeshInstance3D
        {
            Name = "TileMesh",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        };
        AddChild(_meshInstance);

        _staticBody = new StaticBody3D
        {
            Name = "TileCollider",
            CollisionLayer = 1, // Layer 1: Environment (与体素及 3D 静态世界物理层统一)
            CollisionMask = 0
        };
        AddChild(_staticBody);

        _collisionShape = new CollisionShape3D
        {
            Name = "TileShape",
            Disabled = true
        };
        _staticBody.AddChild(_collisionShape);
    }

    /// <summary>
    /// 主线程限流装载网格数据与碰撞体
    /// </summary>
    public void ApplyMeshData(RawTileMeshData data, Material? material)
    {
        InitializeNodes();

        Coord = data.Coord;
        CurrentLOD = data.LOD;
        IsActive = true;
        Visible = true;

        // 1. 设置世界局部原点 (瓦片内部顶点坐标基于 [0, 64])
        GlobalPosition = Coord.ToWorldOrigin();

        // 2. 构建 Godot ArrayMesh
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.TexUV] = data.UVs;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        if (material != null)
        {
            arrayMesh.SurfaceSetMaterial(0, material);
        }

        _meshInstance.Mesh = arrayMesh;

        // 3. LOD0 生成物理碰撞体，LOD1/LOD2 禁用物理碰撞体
        if (data.HasCollider)
        {
            // 为 LOD0 生成精确三角形碰撞面
            var faces = arrayMesh.GetFaces();
            _concaveShape ??= new ConcavePolygonShape3D();
            _concaveShape.SetFaces(faces);
            _collisionShape.Shape = _concaveShape;
            _collisionShape.Disabled = false;
        }
        else
        {
            _collisionShape.Disabled = true;
        }
    }

    /// <summary>
    /// 回收至对象池，释放活动状态
    /// </summary>
    public void Recycle()
    {
        IsActive = false;
        Visible = false;
        CurrentLOD = LODLevel.Unloaded;
        if (_collisionShape != null)
        {
            _collisionShape.Disabled = true;
        }
    }
}
