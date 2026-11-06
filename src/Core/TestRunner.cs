using System;
using Celestial.Terrain;
using Celestial.Voxel;
using Godot;

namespace Celestial.Core;

/// <summary>
/// 核心数学逻辑与体素契约自测套件
/// </summary>
public static class TestRunner
{
    public static void RunAllTests()
    {
        GD.Print("[TestRunner] 开始执行核心数学与体素契约自测...");

        TestChunkCoordMapping();
        TestHeightmapSampling();
        TestWorldCoordinateConversion();

        GD.Print("[TestRunner] 全部核心逻辑单元测试通过！");
    }

    private static void TestChunkCoordMapping()
    {
        for (int z = 0; z < Constants.ChunkSize; z++)
        {
            for (int y = 0; y < Constants.ChunkSize; y++)
            {
                for (int x = 0; x < Constants.ChunkSize; x++)
                {
                    int index = ChunkCoord.LocalToIndex(x, y, z);
                    if (index < 0 || index >= Constants.ChunkVolume)
                    {
                        throw new InvalidOperationException($"Index {index} 超出 Chunk 容积边界 (0 ~ {Constants.ChunkVolume})");
                    }

                    Vector3I restored = ChunkCoord.IndexToLocal(index);
                    if (restored.X != x || restored.Y != y || restored.Z != z)
                    {
                        throw new InvalidOperationException($"坐标还原失真: 原始({x}, {y}, {z}) -> 还原({restored})");
                    }
                }
            }
        }
        GD.Print("  ✓ ChunkCoord 局部坐标与一维紧凑数组双向映射通过 (4096项校验)");
    }

    private static void TestHeightmapSampling()
    {
        SimpleProceduralHeightmap heightmap = new(baseHeight: 5.0f, valleyDepth: 3.0f, valleyRadius: 20.0f);

        float centerHeight = heightmap.GetHeight(0, 0);
        float edgeHeight = heightmap.GetHeight(30, 30);

        if (centerHeight >= edgeHeight)
        {
            throw new InvalidOperationException($"地形凹坑中心高度应低于边缘: center={centerHeight}, edge={edgeHeight}");
        }

        Vector3 normal = heightmap.GetNormal(0, 0);
        if (Mathf.Abs(normal.Length() - 1.0f) > 0.01f)
        {
            throw new InvalidOperationException($"法线向量未单位化: {normal}");
        }

        GD.Print($"  ✓ 地形采样器高度与法线校验通过 (中心坑高: {centerHeight:F2}m, 外缘高: {edgeHeight:F2}m)");
    }

    private static void TestWorldCoordinateConversion()
    {
        Vector3 worldPos1 = new(17.5f, -2.5f, 33.1f);
        ChunkCoord chunk1 = ChunkCoord.FromWorldPosition(worldPos1);

        if (chunk1.X != 1 || chunk1.Y != -1 || chunk1.Z != 2)
        {
            throw new InvalidOperationException($"跨负数象限世界坐标转 Chunk 失败: {chunk1}");
        }

        Vector3I local1 = ChunkCoord.GetLocalVoxelPosition(17, -2, 33);
        if (local1.X != 1 || local1.Y != 14 || local1.Z != 1)
        {
            throw new InvalidOperationException($"负数世界体素求余局部坐标失败: {local1}");
        }

        GD.Print("  ✓ 负数象限与跨界世界体素坐标求余映射通过");
    }
}

