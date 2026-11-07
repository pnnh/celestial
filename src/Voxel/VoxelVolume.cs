using System;
using System.Collections.Generic;
using Celestial.Core;
using Celestial.Terrain;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 局部体素卷节点组件
/// 用于在开放世界局部区域（如采矿场、农田、塌方山洞腔体）按需激活体素地质层
/// 与大地形采样器自动缝合深度，并支持独立差异存档
/// </summary>
public partial class VoxelVolume : Node3D, IVoxelVolume
{
    [Export] public string VolumeId { get; set; } = "quarry_volume";
    [Export] public Vector3 Extents { get; set; } = new Vector3(32.0f, 16.0f, 32.0f);
    [Export] public float ActivationDistance { get; set; } = 64.0f;
    [Export] public int LayerThickness { get; set; } = 4;

    public Aabb WorldBounds
    {
        get
        {
            Vector3 pos = IsInsideTree() ? GlobalPosition : Position;
            return new Aabb(pos - Extents * 0.5f, Extents);
        }
    }

    private readonly Dictionary<ChunkCoord, VoxelChunk> _chunks = new();
    private readonly HashSet<ChunkCoord> _dirtyChunks = new();
    private readonly Dictionary<Vector3I, byte> _modifications = new();

    private StandardMaterial3D _sharedVoxelMaterial = null!;
    private IHeightSampler _sampler = new IslandHeightSampler();
    private Node3D? _observer;
    private bool _isActive = true;

    public override void _Ready()
    {
        Texture2D? atlasTexture = null;
        if (ResourceLoader.Exists("res://assets/textures/voxel_atlas.png"))
        {
            atlasTexture = ResourceLoader.Load<Texture2D>("res://assets/textures/voxel_atlas.png");
        }

        _sharedVoxelMaterial = new StandardMaterial3D
        {
            AlbedoTexture = atlasTexture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            VertexColorUseAsAlbedo = true,
            Roughness = 0.85f,
            Metallic = 0.05f
        };

        // 寻找场景中的大地形采样器
        var terrainManager = GetTree()?.Root?.FindChild("TiledTerrainManager", true, false) as TiledTerrainManager;
        _sampler = terrainManager?.HeightSampler ?? new IslandHeightSampler();

        InitializeVolumeChunks();
    }

    public void SetObserver(Node3D observer)
    {
        _observer = observer;
    }

    private void InitializeVolumeChunks()
    {
        Aabb bounds = WorldBounds;
        int minChunkX = (int)MathF.Floor(bounds.Position.X / Constants.ChunkSize);
        int maxChunkX = (int)MathF.Floor((bounds.Position.X + bounds.Size.X) / Constants.ChunkSize);
        int minChunkY = (int)MathF.Floor(bounds.Position.Y / Constants.ChunkSize);
        int maxChunkY = (int)MathF.Floor((bounds.Position.Y + bounds.Size.Y) / Constants.ChunkSize);
        int minChunkZ = (int)MathF.Floor(bounds.Position.Z / Constants.ChunkSize);
        int maxChunkZ = (int)MathF.Floor((bounds.Position.Z + bounds.Size.Z) / Constants.ChunkSize);

        for (int cx = minChunkX; cx <= maxChunkX; cx++)
        {
            for (int cz = minChunkZ; cz <= maxChunkZ; cz++)
            {
                for (int cy = minChunkY; cy <= maxChunkY; cy++)
                {
                    var coord = new ChunkCoord(cx, cy, cz);
                    var chunk = new VoxelChunk();
                    chunk.Initialize(coord, _sharedVoxelMaterial);
                    AddChild(chunk);
                    _chunks[coord] = chunk;
                    GenerateChunkData(chunk);
                }
            }
        }

        // 构建初始网格
        foreach (var chunk in _chunks.Values)
        {
            // 此处借用 VoxelWorld 适配器或直接重建
            _dirtyChunks.Add(chunk.Coord);
        }
    }

    private void GenerateChunkData(VoxelChunk chunk)
    {
        for (int lz = 0; lz < Constants.ChunkSize; lz++)
        {
            int wz = chunk.Coord.Z * Constants.ChunkSize + lz;
            for (int lx = 0; lx < Constants.ChunkSize; lx++)
            {
                int wx = chunk.Coord.X * Constants.ChunkSize + lx;
                float terrainH = _sampler?.GetHeight(wx + 0.5f, wz + 0.5f) ?? 0.0f;
                int baseTerrainY = Mathf.FloorToInt(terrainH);

                for (int ly = 0; ly < Constants.ChunkSize; ly++)
                {
                    int wy = chunk.Coord.Y * Constants.ChunkSize + ly;

                    if (!ContainsWorldPosition(new Vector3(wx, wy, wz)))
                    {
                        chunk.SetVoxel(lx, ly, lz, VoxelType.Air);
                        continue;
                    }

                    if (wy < baseTerrainY)
                    {
                        // 3D 地形基底以下：实心 Bedrock
                        chunk.SetVoxel(lx, ly, lz, VoxelType.Bedrock);
                    }
                    else if (wy >= baseTerrainY && wy < baseTerrainY + LayerThickness)
                    {
                        if (wy == baseTerrainY + LayerThickness - 1)
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Grass);
                        else if (wy == baseTerrainY)
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Stone);
                        else
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Dirt);
                    }
                    else
                    {
                        chunk.SetVoxel(lx, ly, lz, VoxelType.Air);
                    }
                }
            }
        }
    }

    public bool ContainsWorldPosition(Vector3 worldPos)
    {
        return WorldBounds.HasPoint(worldPos);
    }

    public VoxelType GetVoxelAtWorld(Vector3 worldPos)
    {
        int wx = Mathf.FloorToInt(worldPos.X);
        int wy = Mathf.FloorToInt(worldPos.Y);
        int wz = Mathf.FloorToInt(worldPos.Z);

        if (!ContainsWorldPosition(worldPos))
        {
            // 超出体素卷但低于地表高度时，视为 Bedrock，封闭侧壁
            if (_sampler != null && wy < Mathf.FloorToInt(_sampler.GetHeight(wx + 0.5f, wz + 0.5f)))
            {
                return VoxelType.Bedrock;
            }
            return VoxelType.Air;
        }

        var coord = ChunkCoord.FromVoxelPosition(wx, wy, wz);
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            int lx = wx - coord.X * Constants.ChunkSize;
            int ly = wy - coord.Y * Constants.ChunkSize;
            int lz = wz - coord.Z * Constants.ChunkSize;
            return chunk.GetVoxel(lx, ly, lz);
        }

        return VoxelType.Air;
    }

    public bool TryDigAtWorld(Vector3 worldPos)
    {
        return TryDigAtWorld(worldPos, out _);
    }

    public bool TryDigAtWorld(Vector3 worldPos, out VoxelType dugType)
    {
        dugType = VoxelType.Air;
        if (!ContainsWorldPosition(worldPos)) return false;

        int wx = Mathf.FloorToInt(worldPos.X);
        int wy = Mathf.FloorToInt(worldPos.Y);
        int wz = Mathf.FloorToInt(worldPos.Z);

        VoxelType current = GetVoxelAtWorld(worldPos);
        if (!current.IsDestructible()) return false;

        if (_sampler != null && wy <= Mathf.FloorToInt(_sampler.GetHeight(wx + 0.5f, wz + 0.5f)))
        {
            return false; // 触底基岩不可挖掘
        }

        var coord = ChunkCoord.FromVoxelPosition(wx, wy, wz);
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            int lx = wx - coord.X * Constants.ChunkSize;
            int ly = wy - coord.Y * Constants.ChunkSize;
            int lz = wz - coord.Z * Constants.ChunkSize;

            dugType = current;
            chunk.SetVoxel(lx, ly, lz, VoxelType.Air);
            _dirtyChunks.Add(coord);
            _modifications[new Vector3I(wx, wy, wz)] = (byte)VoxelType.Air;
            return true;
        }

        return false;
    }

    public bool TryPlaceAtWorld(Vector3 worldPos, VoxelType type)
    {
        if (!ContainsWorldPosition(worldPos)) return false;

        int wx = Mathf.FloorToInt(worldPos.X);
        int wy = Mathf.FloorToInt(worldPos.Y);
        int wz = Mathf.FloorToInt(worldPos.Z);

        var coord = ChunkCoord.FromVoxelPosition(wx, wy, wz);
        if (_chunks.TryGetValue(coord, out var chunk))
        {
            int lx = wx - coord.X * Constants.ChunkSize;
            int ly = wy - coord.Y * Constants.ChunkSize;
            int lz = wz - coord.Z * Constants.ChunkSize;

            chunk.SetVoxel(lx, ly, lz, type);
            _dirtyChunks.Add(coord);
            _modifications[new Vector3I(wx, wy, wz)] = (byte)type;
            return true;
        }

        return false;
    }

    public Dictionary<Vector3I, byte> ExportModifications() => new(_modifications);

    public void ImportModifications(Dictionary<Vector3I, byte> modifications)
    {
        foreach (var (pos, typeByte) in modifications)
        {
            TryPlaceAtWorld(new Vector3(pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f), (VoxelType)typeByte);
        }
    }
}
