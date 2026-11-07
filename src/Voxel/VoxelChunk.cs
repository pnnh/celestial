using Celestial.Core;
using Celestial.Terrain;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 单个 16x16x16 体素分块节点（管理体素数据、视觉网格与物理碰撞）
/// </summary>
public partial class VoxelChunk : Node3D
{
    public ChunkCoord Coord { get; private set; }
    public bool IsDirty { get; set; } = true;

    private readonly byte[] _voxels = new byte[Constants.ChunkVolume];
    private MeshInstance3D _meshInstance = null!;
    private StaticBody3D _staticBody = null!;
    private CollisionShape3D _collisionShape = null!;
    private StandardMaterial3D _sharedMaterial = null!;

    public void Initialize(ChunkCoord coord, StandardMaterial3D sharedMaterial)
    {
        Coord = coord;
        _sharedMaterial = sharedMaterial;
        Position = coord.ToWorldPosition();
        Name = $"Chunk_{coord.X}_{coord.Y}_{coord.Z}";

        _meshInstance = new MeshInstance3D { Name = "ChunkMesh" };
        AddChild(_meshInstance);

        // 碰撞体注册在 Layer 1 (与传统地形共享环境碰撞层)
        _staticBody = new StaticBody3D
        {
            Name = "ChunkCollision",
            CollisionLayer = 1,
            CollisionMask = 0
        };

        _collisionShape = new CollisionShape3D { Name = "ChunkShape" };
        _staticBody.AddChild(_collisionShape);
        AddChild(_staticBody);
    }

    public VoxelType GetVoxel(int lx, int ly, int lz)
    {
        if (lx < 0 || lx >= Constants.ChunkSize ||
            ly < 0 || ly >= Constants.ChunkSize ||
            lz < 0 || lz >= Constants.ChunkSize)
        {
            return VoxelType.Air;
        }

        int index = ChunkCoord.LocalToIndex(lx, ly, lz);
        return (VoxelType)_voxels[index];
    }

    public void SetVoxel(int lx, int ly, int lz, VoxelType type)
    {
        if (lx < 0 || lx >= Constants.ChunkSize ||
            ly < 0 || ly >= Constants.ChunkSize ||
            lz < 0 || lz >= Constants.ChunkSize)
        {
            return;
        }

        int index = ChunkCoord.LocalToIndex(lx, ly, lz);
        if (_voxels[index] != (byte)type)
        {
            _voxels[index] = (byte)type;
            IsDirty = true;
        }
    }

    /// <summary>
    /// 重建当前 Chunk 的视觉渲染网格与物理碰撞多边形
    /// </summary>
    public void RebuildMesh(VoxelWorld world, IHeightSampler? terrainSampler = null)
    {
        ArrayMesh? mesh = ChunkMesher.BuildMesh(this, world, terrainSampler);

        if (mesh != null)
        {
            mesh.SurfaceSetMaterial(0, _sharedMaterial);
            _meshInstance.Mesh = mesh;

            // 同步物理碰撞网格
            Vector3[] faces = mesh.GetFaces();
            if (faces.Length > 0)
            {
                ConcavePolygonShape3D shape = new();
                shape.SetFaces(faces);
                _collisionShape.Shape = shape;
            }
            else
            {
                _collisionShape.Shape = null;
            }
        }
        else
        {
            _meshInstance.Mesh = null;
            _collisionShape.Shape = null;
        }

        IsDirty = false;
    }
}

