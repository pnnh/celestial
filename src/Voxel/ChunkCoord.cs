using System;
using Celestial.Core;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 三维 Chunk 空间坐标结构体
/// </summary>
public readonly struct ChunkCoord : IEquatable<ChunkCoord>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public ChunkCoord(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// 将世界三维坐标转换为对应的 Chunk 坐标
    /// </summary>
    public static ChunkCoord FromWorldPosition(Vector3 worldPos)
    {
        int cx = Mathf.FloorToInt(worldPos.X / Constants.ChunkSize);
        int cy = Mathf.FloorToInt(worldPos.Y / Constants.ChunkSize);
        int cz = Mathf.FloorToInt(worldPos.Z / Constants.ChunkSize);
        return new ChunkCoord(cx, cy, cz);
    }

    /// <summary>
    /// 将世界体素整数坐标转换为对应的 Chunk 坐标
    /// </summary>
    public static ChunkCoord FromVoxelPosition(int vx, int vy, int vz)
    {
        int cx = Mathf.FloorToInt((float)vx / Constants.ChunkSize);
        int cy = Mathf.FloorToInt((float)vy / Constants.ChunkSize);
        int cz = Mathf.FloorToInt((float)vz / Constants.ChunkSize);
        return new ChunkCoord(cx, cy, cz);
    }

    /// <summary>
    /// 获取当前 Chunk 在世界坐标系中的原点位置（最小角坐标）
    /// </summary>
    public Vector3 ToWorldPosition()
    {
        return new Vector3(
            X * Constants.ChunkSize * Constants.VoxelSize,
            Y * Constants.ChunkSize * Constants.VoxelSize,
            Z * Constants.ChunkSize * Constants.VoxelSize
        );
    }

    /// <summary>
    /// 计算世界体素坐标在所在 Chunk 内部的局部坐标 (0 ~ 15)
    /// </summary>
    public static Vector3I GetLocalVoxelPosition(int vx, int vy, int vz)
    {
        int lx = ((vx % Constants.ChunkSize) + Constants.ChunkSize) % Constants.ChunkSize;
        int ly = ((vy % Constants.ChunkSize) + Constants.ChunkSize) % Constants.ChunkSize;
        int lz = ((vz % Constants.ChunkSize) + Constants.ChunkSize) % Constants.ChunkSize;
        return new Vector3I(lx, ly, lz);
    }

    /// <summary>
    /// 将 Chunk 内局部坐标转换为连续一维数组索引 (0 ~ 4095)
    /// </summary>
    public static int LocalToIndex(int lx, int ly, int lz)
    {
        return lx + Constants.ChunkSize * (ly + Constants.ChunkSize * lz);
    }

    /// <summary>
    /// 将一维数组索引还原为 Chunk 内局部坐标
    /// </summary>
    public static Vector3I IndexToLocal(int index)
    {
        int lx = index % Constants.ChunkSize;
        int rem = index / Constants.ChunkSize;
        int ly = rem % Constants.ChunkSize;
        int lz = rem / Constants.ChunkSize;
        return new Vector3I(lx, ly, lz);
    }

    public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object? obj) => obj is ChunkCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"Chunk({X}, {Y}, {Z})";

    public static bool operator ==(ChunkCoord left, ChunkCoord right) => left.Equals(right);
    public static bool operator !=(ChunkCoord left, ChunkCoord right) => !left.Equals(right);
}

