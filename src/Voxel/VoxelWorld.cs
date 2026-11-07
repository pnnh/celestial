using System.Collections.Generic;
using Celestial.Core;
using Celestial.Terrain;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 全局体素世界管理器（Chunk 分块调度、全局坐标映射、跨 Chunk 边界联动与传统地形混合）
/// </summary>
public partial class VoxelWorld : Node3D
{
    [Export] public StaticTerrain? TraditionalTerrain;
    [Export] public int RegionChunkRadiusX = 1; // 范围 [-1, 1] 轴向 Chunk 数量
    [Export] public int RegionChunkRadiusZ = 1; // 范围 [-1, 1] 轴向 Chunk 数量
    [Export] public int RegionChunkHeight = 1;  // 高度方向 Chunk 数量 (0 ~ 1)
    [Export] public int VoxelLayerThickness = 4; // 传统地形上覆盖的可挖掘体素层厚度（米）

    private readonly Dictionary<ChunkCoord, VoxelChunk> _chunks = [];
    private readonly HashSet<ChunkCoord> _dirtyChunks = [];
    private readonly Dictionary<ChunkCoord, Dictionary<int, VoxelType>> _playerModifications = [];
    private StandardMaterial3D _sharedVoxelMaterial = null!;

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

        if (TraditionalTerrain == null)
        {
            TraditionalTerrain = GetNodeOrNull<StaticTerrain>("../StaticTerrain");
        }

        InitializeVoxelRegion();
    }

    /// <summary>
    /// 在传统地形的中央峡谷/可挖掘区域初始化体素分块数据
    /// </summary>
    public void InitializeVoxelRegion()
    {
        IHeightSampler? sampler = TraditionalTerrain?.HeightSampler;

        for (int cx = -RegionChunkRadiusX; cx <= RegionChunkRadiusX; cx++)
        {
            for (int cz = -RegionChunkRadiusZ; cz <= RegionChunkRadiusZ; cz++)
            {
                for (int cy = 0; cy < RegionChunkHeight; cy++)
                {
                    ChunkCoord coord = new(cx, cy, cz);
                    VoxelChunk chunk = CreateChunkNode(coord);

                    PopulateChunkVoxels(chunk, sampler);
                    _dirtyChunks.Add(coord);
                }
            }
        }

        UpdateDirtyChunks();
        GD.Print($"VoxelWorld initialized. Total Chunks: {_chunks.Count}");
    }

    private VoxelChunk CreateChunkNode(ChunkCoord coord)
    {
        VoxelChunk chunk = new();
        chunk.Initialize(coord, _sharedVoxelMaterial);
        _chunks[coord] = chunk;
        AddChild(chunk);
        return chunk;
    }

    private void PopulateChunkVoxels(VoxelChunk chunk, IHeightSampler? sampler)
    {
        for (int lz = 0; lz < Constants.ChunkSize; lz++)
        {
            int wz = chunk.Coord.Z * Constants.ChunkSize + lz;
            for (int lx = 0; lx < Constants.ChunkSize; lx++)
            {
                int wx = chunk.Coord.X * Constants.ChunkSize + lx;
                float terrainHeight = sampler?.GetHeight(wx + 0.5f, wz + 0.5f) ?? 0.0f;
                int baseTerrainY = Mathf.FloorToInt(terrainHeight);

                for (int ly = 0; ly < Constants.ChunkSize; ly++)
                {
                    int wy = chunk.Coord.Y * Constants.ChunkSize + ly;

                    if (wy < baseTerrainY)
                    {
                        // 低于传统 3D 地形表面：作为坚固基岩层实心填充，杜绝地下镂空与挖掘后透视穿帮
                        chunk.SetVoxel(lx, ly, lz, VoxelType.Bedrock);
                    }
                    else if (wy >= baseTerrainY && wy < baseTerrainY + VoxelLayerThickness)
                    {
                        // 覆盖在传统地形之上的可编辑体素层（表层草地、下层泥土和矿石）
                        if (wy == baseTerrainY + VoxelLayerThickness - 1)
                        {
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Grass);
                        }
                        else if (wy == baseTerrainY)
                        {
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Stone);
                        }
                        else
                        {
                            chunk.SetVoxel(lx, ly, lz, VoxelType.Dirt);
                        }
                    }
                    else
                    {
                        chunk.SetVoxel(lx, ly, lz, VoxelType.Air);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取世界体素坐标处的体素类型
    /// </summary>
    public VoxelType GetVoxel(int wx, int wy, int wz)
    {
        ChunkCoord coord = ChunkCoord.FromVoxelPosition(wx, wy, wz);
        if (_chunks.TryGetValue(coord, out VoxelChunk? chunk))
        {
            Vector3I local = ChunkCoord.GetLocalVoxelPosition(wx, wy, wz);
            return chunk.GetVoxel(local.X, local.Y, local.Z);
        }

        // 超出体素生成区域：外部无实体方块，视为空气，确保边界方块朝外表面完整生成
        return VoxelType.Air;
    }

    /// <summary>
    /// 检查指定世界体素位置是否允许被挖掘（关键：触底传统地形阻隔判定）
    /// </summary>
    public bool CanDig(int wx, int wy, int wz)
    {
        VoxelType type = GetVoxel(wx, wy, wz);
        if (!type.IsDestructible())
        {
            return false;
        }

        // 如果该体素紧贴或已经达到传统地形表面高度，判定为触底不可挖掘
        if (TraditionalTerrain?.HeightSampler is { } sampler)
        {
            float terrainH = sampler.GetHeight(wx + 0.5f, wz + 0.5f);
            if (wy <= Mathf.FloorToInt(terrainH))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 设置世界体素坐标处的体素类型，并级联更新受影响的相邻 Chunk
    /// </summary>
    public bool SetVoxel(int wx, int wy, int wz, VoxelType type)
    {
        ChunkCoord coord = ChunkCoord.FromVoxelPosition(wx, wy, wz);
        if (!_chunks.TryGetValue(coord, out VoxelChunk? chunk))
        {
            // 仅对非空气方块动态创建新 Chunk
            if (type == VoxelType.Air)
            {
                return false;
            }
            chunk = CreateChunkNode(coord);
        }

        Vector3I local = ChunkCoord.GetLocalVoxelPosition(wx, wy, wz);
        chunk.SetVoxel(local.X, local.Y, local.Z, type);
        _dirtyChunks.Add(coord);

        // 记录差异修改用于存档
        int localIndex = ChunkCoord.LocalToIndex(local.X, local.Y, local.Z);
        if (!_playerModifications.TryGetValue(coord, out Dictionary<int, VoxelType>? diffs))
        {
            diffs = [];
            _playerModifications[coord] = diffs;
        }
        diffs[localIndex] = type;

        // 检查边界并级联标记相邻 Chunk 为 Dirty
        MarkBorderNeighborsDirty(coord, local);
        return true;
    }

    /// <summary>
    /// 导出当前世界的玩家修改差异数据
    /// </summary>
    public Celestial.Persistence.WorldSaveData ExportDeltaSaveData()
    {
        Celestial.Persistence.WorldSaveData data = new();
        foreach (KeyValuePair<ChunkCoord, Dictionary<int, VoxelType>> kv in _playerModifications)
        {
            if (kv.Value.Count == 0) continue;
            Celestial.Persistence.ChunkDelta cd = new()
            {
                ChunkX = kv.Key.X,
                ChunkY = kv.Key.Y,
                ChunkZ = kv.Key.Z
            };
            foreach (KeyValuePair<int, VoxelType> d in kv.Value)
            {
                cd.Diffs.Add(new Celestial.Persistence.VoxelDeltaEntry(d.Key, (byte)d.Value));
            }
            data.Chunks.Add(cd);
        }
        return data;
    }

    /// <summary>
    /// 导入并应用玩家修改差异数据，还原世界修改状态
    /// </summary>
    public void ImportDeltaSaveData(Celestial.Persistence.WorldSaveData data)
    {
        foreach (Celestial.Persistence.ChunkDelta chunkDelta in data.Chunks)
        {
            ChunkCoord coord = new(chunkDelta.ChunkX, chunkDelta.ChunkY, chunkDelta.ChunkZ);
            if (!_chunks.TryGetValue(coord, out VoxelChunk? chunk))
            {
                chunk = CreateChunkNode(coord);
            }

            if (!_playerModifications.TryGetValue(coord, out Dictionary<int, VoxelType>? diffs))
            {
                diffs = [];
                _playerModifications[coord] = diffs;
            }

            foreach (Celestial.Persistence.VoxelDeltaEntry entry in chunkDelta.Diffs)
            {
                Vector3I local = ChunkCoord.IndexToLocal(entry.Index);
                VoxelType type = (VoxelType)entry.Type;
                chunk.SetVoxel(local.X, local.Y, local.Z, type);
                diffs[entry.Index] = type;
            }

            _dirtyChunks.Add(coord);
        }

        UpdateDirtyChunks();
    }

    public void MarkChunkDirty(ChunkCoord targetCoord)
    {
        if (_chunks.TryGetValue(targetCoord, out VoxelChunk? chunk))
        {
            chunk.IsDirty = true;
            _dirtyChunks.Add(targetCoord);
        }
    }

    private void MarkBorderNeighborsDirty(ChunkCoord coord, Vector3I local)
    {
        if (local.X == 0) MarkChunkDirty(new ChunkCoord(coord.X - 1, coord.Y, coord.Z));
        if (local.X == Constants.ChunkSize - 1) MarkChunkDirty(new ChunkCoord(coord.X + 1, coord.Y, coord.Z));
        if (local.Y == 0) MarkChunkDirty(new ChunkCoord(coord.X, coord.Y - 1, coord.Z));
        if (local.Y == Constants.ChunkSize - 1) MarkChunkDirty(new ChunkCoord(coord.X, coord.Y + 1, coord.Z));
        if (local.Z == 0) MarkChunkDirty(new ChunkCoord(coord.X, coord.Y, coord.Z - 1));
        if (local.Z == Constants.ChunkSize - 1) MarkChunkDirty(new ChunkCoord(coord.X, coord.Y, coord.Z + 1));
    }

    /// <summary>
    /// 重建所有标记为脏状态的 Chunk 网格
    /// </summary>
    public void UpdateDirtyChunks()
    {
        if (_dirtyChunks.Count == 0)
        {
            return;
        }

        IHeightSampler? sampler = TraditionalTerrain?.HeightSampler;
        foreach (ChunkCoord coord in _dirtyChunks)
        {
            if (_chunks.TryGetValue(coord, out VoxelChunk? chunk))
            {
                chunk.RebuildMesh(this, sampler);
            }
        }

        _dirtyChunks.Clear();
    }

    public override void _Process(double delta)
    {
        // 每帧检测是否有需要更新的脏 Chunk 并执行重建
        if (_dirtyChunks.Count > 0)
        {
            UpdateDirtyChunks();
        }
    }
}

