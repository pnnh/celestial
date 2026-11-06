using System.Collections.Generic;
using Celestial.Core;
using Celestial.Terrain;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 体素分块面剔除网格生成器 (Face-Culling Mesher) 与碰撞同步器
/// </summary>
public static class ChunkMesher
{
    // 标准立方体 8 个局部顶点偏移 (0 到 1)
    private static readonly Vector3[] CubeVertices =
    [
        new(0, 0, 0), // 0
        new(1, 0, 0), // 1
        new(1, 1, 0), // 2
        new(0, 1, 0), // 3
        new(0, 0, 1), // 4
        new(1, 0, 1), // 5
        new(1, 1, 1), // 6
        new(0, 1, 1)  // 7
    ];

    // 每个面的 4 个顶点索引 (顺时针，右手坐标系法线朝外)
    // 0: East (+X), 1: West (-X), 2: Up (+Y), 3: Down (-Y), 4: South (+Z), 5: North (-Z)
    private static readonly int[][] FaceVertexIndices =
    [
        [1, 5, 6, 2], // +X
        [4, 0, 3, 7], // -X
        [3, 2, 6, 7], // +Y
        [4, 5, 1, 0], // -Y
        [5, 4, 7, 6], // +Z
        [0, 1, 2, 3]  // -Z
    ];

    private static readonly Vector2[] FaceUVs =
    [
        new(0, 1),
        new(1, 1),
        new(1, 0),
        new(0, 0)
    ];

    /// <summary>
    /// 为指定的 Chunk 生成仅包含可见外表面的 ArrayMesh
    /// </summary>
    public static ArrayMesh? BuildMesh(VoxelChunk chunk, VoxelWorld world, IHeightSampler? terrainSampler = null)
    {
        List<Vector3> vertices = [];
        List<Vector3> normals = [];
        List<Vector2> uvs = [];
        List<Color> colors = [];
        List<int> indices = [];

        Vector3 chunkOrigin = chunk.Coord.ToWorldPosition();

        for (int lz = 0; lz < Constants.ChunkSize; lz++)
        {
            int wz = chunk.Coord.Z * Constants.ChunkSize + lz;
            for (int ly = 0; ly < Constants.ChunkSize; ly++)
            {
                int wy = chunk.Coord.Y * Constants.ChunkSize + ly;
                for (int lx = 0; lx < Constants.ChunkSize; lx++)
                {
                    int wx = chunk.Coord.X * Constants.ChunkSize + lx;
                    VoxelType currentType = chunk.GetVoxel(lx, ly, lz);

                    if (!currentType.IsSolid())
                    {
                        continue;
                    }

                    Color voxelColor = currentType.GetDebugColor();
                    Vector3 localBlockPos = new(lx, ly, lz);

                    // 检查 6 个面的相邻邻居
                    for (int face = 0; face < 6; face++)
                    {
                        Vector3I dir = Constants.Directions[face];
                        int nx = lx + dir.X;
                        int ny = ly + dir.Y;
                        int nz = lz + dir.Z;

                        VoxelType neighborType;
                        if (nx >= 0 && nx < Constants.ChunkSize &&
                            ny >= 0 && ny < Constants.ChunkSize &&
                            nz >= 0 && nz < Constants.ChunkSize)
                        {
                            neighborType = chunk.GetVoxel(nx, ny, nz);
                        }
                        else
                        {
                            // 跨越 Chunk 边界，查询全局体素世界
                            neighborType = world.GetVoxel(wx + dir.X, wy + dir.Y, wz + dir.Z);
                        }

                        // 如果邻居非实心（是空气），则当前面可见，需要渲染
                        if (!neighborType.IsSolid())
                        {
                            AddFace(face, localBlockPos, voxelColor, vertices, normals, uvs, colors, indices);
                        }
                    }
                }
            }
        }

        if (vertices.Count == 0)
        {
            return null;
        }

        Godot.Collections.Array arrays = [];
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static void AddFace(
        int faceIndex,
        Vector3 blockPos,
        Color color,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Color> colors,
        List<int> indices)
    {
        int startIndex = vertices.Count;
        Vector3 normal = Constants.Normals[faceIndex];
        int[] vertexIndices = FaceVertexIndices[faceIndex];

        // 顶面或侧面微调环境遮挡明暗（模拟简易 AO）
        float shade = faceIndex switch
        {
            2 => 1.0f,  // Top (+Y): 最亮
            3 => 0.55f, // Bottom (-Y): 最暗
            4 or 5 => 0.85f,
            _ => 0.75f
        };
        Color shadedColor = new(color.R * shade, color.G * shade, color.B * shade, color.A);

        for (int i = 0; i < 4; i++)
        {
            Vector3 v = blockPos + CubeVertices[vertexIndices[i]];
            vertices.Add(v);
            normals.Add(normal);
            uvs.Add(FaceUVs[i]);
            colors.Add(shadedColor);
        }

        // 两个三角形构成一个四边形面 (0 -> 1 -> 2, 0 -> 2 -> 3)
        indices.Add(startIndex + 0);
        indices.Add(startIndex + 1);
        indices.Add(startIndex + 2);

        indices.Add(startIndex + 0);
        indices.Add(startIndex + 2);
        indices.Add(startIndex + 3);
    }
}

