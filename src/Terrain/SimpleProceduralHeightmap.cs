using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 确定性程序化起伏地形采样器与网格生成器（用于原型测试与基岩参照）
/// </summary>
public class SimpleProceduralHeightmap : IHeightSampler
{
    private readonly float _baseHeight;
    private readonly float _valleyDepth;
    private readonly float _valleyRadius;

    public SimpleProceduralHeightmap(float baseHeight = 6.0f, float valleyDepth = 4.0f, float valleyRadius = 24.0f)
    {
        _baseHeight = baseHeight;
        _valleyDepth = valleyDepth;
        _valleyRadius = valleyRadius;
    }

    /// <summary>
    /// 计算指定世界坐标的高度：中心为下凹峡谷/矿坑，外围为缓坡丘陵
    /// </summary>
    public float GetHeight(float worldX, float worldZ)
    {
        // 缓坡波浪丘陵
        float hills = Mathf.Sin(worldX * 0.08f) * Mathf.Cos(worldZ * 0.08f) * 2.5f
                    + Mathf.Sin(worldX * 0.15f + 1.2f) * 1.0f;

        // 中心采矿盆地/凹坑 (Gaussian Valley)
        float distSq = worldX * worldX + worldZ * worldZ;
        float valleyFactor = Mathf.Exp(-distSq / (_valleyRadius * _valleyRadius));
        float pit = -_valleyDepth * valleyFactor;

        return Mathf.Max(0.0f, _baseHeight + hills + pit);
    }

    public bool IsBelowSurface(float worldX, float worldY, float worldZ)
    {
        return worldY <= GetHeight(worldX, worldZ) + 0.05f;
    }

    public Vector3 GetNormal(float worldX, float worldZ)
    {
        const float delta = 0.2f;
        float hL = GetHeight(worldX - delta, worldZ);
        float hR = GetHeight(worldX + delta, worldZ);
        float hD = GetHeight(worldX, worldZ - delta);
        float hU = GetHeight(worldX, worldZ + delta);

        Vector3 normal = new(hL - hR, 2.0f * delta, hD - hU);
        return normal.Normalized();
    }

    /// <summary>
    /// 根据采样函数生成测试 3D 传统地形网格 (ArrayMesh)
    /// </summary>
    public ArrayMesh GenerateMesh(int minX, int maxX, int minZ, int maxZ, float step = 1.0f)
    {
        int countX = Mathf.CeilToInt((maxX - minX) / step) + 1;
        int countZ = Mathf.CeilToInt((maxZ - minZ) / step) + 1;

        int vertexCount = countX * countZ;
        int indexCount = (countX - 1) * (countZ - 1) * 6;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] indices = new int[indexCount];

        for (int z = 0; z < countZ; z++)
        {
            float wz = minZ + z * step;
            for (int x = 0; x < countX; x++)
            {
                float wx = minX + x * step;
                float wy = GetHeight(wx, wz);
                int idx = x + z * countX;

                vertices[idx] = new Vector3(wx, wy, wz);
                normals[idx] = GetNormal(wx, wz);
                uvs[idx] = new Vector2(wx * 0.1f, wz * 0.1f);

                // 根据高度生成简单的地层坡度基色（岩石、泥土、基岩渐变）
                float slope = 1.0f - normals[idx].Y;
                Color rockColor = new(0.48f, 0.46f, 0.44f);
                Color grassColor = new(0.35f, 0.55f, 0.25f);
                colors[idx] = grassColor.Lerp(rockColor, Mathf.Clamp(slope * 1.5f, 0.0f, 1.0f));
            }
        }

        int indexPtr = 0;
        for (int z = 0; z < countZ - 1; z++)
        {
            for (int x = 0; x < countX - 1; x++)
            {
                int i0 = x + z * countX;
                int i1 = (x + 1) + z * countX;
                int i2 = x + (z + 1) * countX;
                int i3 = (x + 1) + (z + 1) * countX;

                // Triangle 1: i0 -> i1 -> i2
                indices[indexPtr++] = i0;
                indices[indexPtr++] = i1;
                indices[indexPtr++] = i2;

                // Triangle 2: i1 -> i3 -> i2
                indices[indexPtr++] = i1;
                indices[indexPtr++] = i3;
                indices[indexPtr++] = i2;
            }
        }

        Godot.Collections.Array surfaceArrays = [];
        surfaceArrays.Resize((int)Mesh.ArrayType.Max);
        surfaceArrays[(int)Mesh.ArrayType.Vertex] = vertices;
        surfaceArrays[(int)Mesh.ArrayType.Normal] = normals;
        surfaceArrays[(int)Mesh.ArrayType.TexUV] = uvs;
        surfaceArrays[(int)Mesh.ArrayType.Color] = colors;
        surfaceArrays[(int)Mesh.ArrayType.Index] = indices;

        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrays);
        return mesh;
    }
}

