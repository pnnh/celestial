using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 大世界瓦片视距等级
/// </summary>
public enum LODLevel
{
    LOD0_Near = 0,    // 0 ~ 96m: 步长 1m, 64x64 格子, 8192 三角面, 包含物理碰撞体
    LOD1_Mid = 1,     // 96 ~ 320m: 步长 4m, 16x16 格子, 512 三角面, 仅渲染无物理碰撞
    LOD2_Far = 2,     // 320 ~ 640m: 步长 16m, 4x4 格子, 32 三角面, 远景低模天际线
    Unloaded = 3      // > 640m: 卸载或放入对象池
}

/// <summary>
/// 异步网格计算结果数值传输对象
/// 完全由纯数值数组构成，可在多线程后台并发填充，无需触碰 Godot 渲染线程 API
/// </summary>
public sealed class RawTileMeshData
{
    public TerrainTileCoord Coord { get; init; }
    public LODLevel LOD { get; init; }
    public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
    public Vector3[] Normals { get; set; } = Array.Empty<Vector3>();
    public Vector2[] UVs { get; set; } = Array.Empty<Vector2>();
    public int[] Indices { get; set; } = Array.Empty<int>();
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public bool HasCollider => LOD == LODLevel.LOD0_Near;

    /// <summary>
    /// 根据 LOD 等级获取对应的网格分辨率步长（米）
    /// </summary>
    public static int GetStepForLOD(LODLevel lod) => lod switch
    {
        LODLevel.LOD0_Near => 1,
        LODLevel.LOD1_Mid => 4,
        LODLevel.LOD2_Far => 16,
        _ => 16
    };

    /// <summary>
    /// 后台多线程网格生成器：基于 IHeightSampler 填充顶点、法线与索引
    /// </summary>
    public static RawTileMeshData Build(TerrainTileCoord coord, LODLevel lod, IHeightSampler sampler, float tileSize = 64.0f)
    {
        int step = GetStepForLOD(lod);
        int quadsPerSide = (int)tileSize / step;
        int verticesPerSide = quadsPerSide + 1;
        int totalVertices = verticesPerSide * verticesPerSide;
        int totalIndices = quadsPerSide * quadsPerSide * 6;

        var vertices = new Vector3[totalVertices];
        var normals = new Vector3[totalVertices];
        var uvs = new Vector2[totalVertices];
        var indices = new int[totalIndices];

        float originX = coord.X * tileSize;
        float originZ = coord.Z * tileSize;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // 1. 生成顶点高度与 UV
        for (int zIndex = 0; zIndex < verticesPerSide; zIndex++)
        {
            float localZ = zIndex * step;
            float worldZ = originZ + localZ;

            for (int xIndex = 0; xIndex < verticesPerSide; xIndex++)
            {
                float localX = xIndex * step;
                float worldX = originX + localX;

                float height = sampler.GetHeight(worldX, worldZ);
                if (height < minY) minY = height;
                if (height > maxY) maxY = height;

                int vIndex = zIndex * verticesPerSide + xIndex;
                vertices[vIndex] = new Vector3(localX, height, localZ);
                normals[vIndex] = sampler.GetNormal(worldX, worldZ);
                uvs[vIndex] = new Vector2(localX / tileSize, localZ / tileSize);
            }
        }

        // 2. 生成三角面索引 (顺时针，逆时针朝外法线)
        int iIndex = 0;
        for (int zIndex = 0; zIndex < quadsPerSide; zIndex++)
        {
            for (int xIndex = 0; xIndex < quadsPerSide; xIndex++)
            {
                int row1 = zIndex * verticesPerSide;
                int row2 = (zIndex + 1) * verticesPerSide;

                int a = row1 + xIndex;
                int b = row1 + xIndex + 1;
                int c = row2 + xIndex;
                int d = row2 + xIndex + 1;

                // 三角形 1: a -> b -> c
                indices[iIndex++] = a;
                indices[iIndex++] = b;
                indices[iIndex++] = c;

                // 三角形 2: b -> d -> c
                indices[iIndex++] = b;
                indices[iIndex++] = d;
                indices[iIndex++] = c;
            }
        }

        return new RawTileMeshData
        {
            Coord = coord,
            LOD = lod,
            Vertices = vertices,
            Normals = normals,
            UVs = uvs,
            Indices = indices,
            MinY = minY,
            MaxY = maxY
        };
    }
}

