using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Celestial.Persistence;

/// <summary>
/// 单个方块的修改记录条目
/// </summary>
public struct VoxelDeltaEntry
{
    [JsonPropertyName("i")] public int Index { get; set; }   // 0 ~ 4095
    [JsonPropertyName("t")] public byte Type { get; set; }    // VoxelType byte 值

    public VoxelDeltaEntry(int index, byte type)
    {
        Index = index;
        Type = type;
    }
}

/// <summary>
/// 单个 Chunk 的差异数据集合（仅保存发生过变更的方块）
/// </summary>
public class ChunkDelta
{
    [JsonPropertyName("cx")] public int ChunkX { get; set; }
    [JsonPropertyName("cy")] public int ChunkY { get; set; }
    [JsonPropertyName("cz")] public int ChunkZ { get; set; }
    [JsonPropertyName("diffs")] public List<VoxelDeltaEntry> Diffs { get; set; } = [];
}

/// <summary>
/// 世界差异存档根对象
/// </summary>
public class WorldSaveData
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    [JsonPropertyName("chunks")] public List<ChunkDelta> Chunks { get; set; } = [];
}

