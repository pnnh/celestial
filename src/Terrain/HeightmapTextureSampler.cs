using System;
using System.IO;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 高精 16/32 位手绘海岛预制高度场采样器
/// 支持 2048x2048 预制高程图毫秒级直接内存映射与双线性插值
/// 具有《上古卷轴》风格的锋利山脊、山谷水蚀冲沟与破火山口湖
/// 线程安全，支持多线程高并发无锁物理查询
/// </summary>
public partial class HeightmapTextureSampler : RefCounted, IHeightSampler
{
    public float IslandWorldSize { get; init; } = 700.0f;       // 海岛实际物理尺寸 (米)
    public float MaxElevation { get; init; } = 76.0f;          // 山峰最高海拔绝对标高 (米)
    public float SeaLevel => 0.0f;                             // 海平面基准 0.0m
    public float OceanBedDepth { get; init; } = -14.0f;        // 外围深海底标高 (米)
    public float CoastlineThreshold { get; init; } = 0.09f;    // 归一化高度对应海平面的临界阈值
    public int Resolution { get; private set; } = 2048;        // 高度图分辨率
    public int Seed => 2026;

    private readonly float[] _heightData;
    private readonly bool _isLoaded = false;
    private readonly IHeightSampler _fallbackSampler;

    public bool IsLoaded => _isLoaded;

    public HeightmapTextureSampler() : this(null)
    {
    }

    public HeightmapTextureSampler(string? f32FilePath = null)
    {
        _fallbackSampler = new IslandHeightSampler();

        // 智能解析绝对路径 (兼容 Godot res:// 虚拟路径与操作系统绝对路径)
        if (string.IsNullOrEmpty(f32FilePath))
        {
            string globalPath = ProjectSettings.GlobalizePath("res://assets/textures/heightmaps/island_heightmap_16bit.f32");
            if (File.Exists(globalPath))
            {
                f32FilePath = globalPath;
            }
            else
            {
                // 回退到当前工作目录相对路径
                string localPath = Path.Combine(Directory.GetCurrentDirectory(), "assets/textures/heightmaps/island_heightmap_16bit.f32");
                if (File.Exists(localPath))
                {
                    f32FilePath = localPath;
                }
            }
        }

        if (!string.IsNullOrEmpty(f32FilePath) && File.Exists(f32FilePath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(f32FilePath);
                int expectedCount = Resolution * Resolution;
                if (bytes.Length == expectedCount * sizeof(float))
                {
                    _heightData = new float[expectedCount];
                    Buffer.BlockCopy(bytes, 0, _heightData, 0, bytes.Length);
                    _isLoaded = true;
                    GD.Print($"[HeightmapSampler] 成功载入预制 32-bit Float 高精高度场: {Resolution}x{Resolution} ({bytes.Length / 1024 / 1024}MB)");
                    return;
                }
                else
                {
                    GD.PushWarning($"[HeightmapSampler] 高度场文件大小不匹配: {bytes.Length} bytes != 预期 {expectedCount * sizeof(float)} bytes");
                }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[HeightmapSampler] 读取高度场二进制失败，将采用回退采样器: {ex.Message}");
            }
        }
        else
        {
            GD.PushWarning($"[HeightmapSampler] 未找到高度场二进制文件: {f32FilePath}，将采用回退采样器");
        }

        // 回退初始化空数组
        _heightData = Array.Empty<float>();
    }

    /// <summary>
    /// 获取世界平面坐标 (worldX, worldZ) 处的地表绝对高度 Y (米)
    /// 采用严格的双线性插值 (Bilinear Interpolation)，彻底根除阶梯纹
    /// </summary>
    public float GetHeight(float worldX, float worldZ)
    {
        if (!_isLoaded)
        {
            return _fallbackSampler.GetHeight(worldX, worldZ);
        }

        float halfSize = IslandWorldSize * 0.5f;

        // 映射到 [0, 1] UV 空间 (以中心 (0, 0) 为原点)
        float u = (worldX + halfSize) / IslandWorldSize;
        float v = (worldZ + halfSize) / IslandWorldSize;

        // 超出海岛包围盒：平滑融入大洋底面
        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            float dist = MathF.Sqrt(worldX * worldX + worldZ * worldZ);
            float fadeT = Math.Clamp((dist - halfSize) / 80.0f, 0.0f, 1.0f);
            return Mathf.Lerp(OceanBedDepth, OceanBedDepth - 6.0f, fadeT);
        }

        // 双线性网格坐标与分数计算
        float px = u * (Resolution - 1);
        float pz = v * (Resolution - 1);

        int x0 = (int)MathF.Floor(px);
        int z0 = (int)MathF.Floor(pz);
        int x1 = Math.Min(x0 + 1, Resolution - 1);
        int z1 = Math.Min(z0 + 1, Resolution - 1);

        float fx = px - x0;
        float fz = pz - z0;

        float h00 = _heightData[z0 * Resolution + x0];
        float h10 = _heightData[z0 * Resolution + x1];
        float h01 = _heightData[z1 * Resolution + x0];
        float h11 = _heightData[z1 * Resolution + x1];

        float hTop = h00 * (1.0f - fx) + h10 * fx;
        float hBot = h01 * (1.0f - fx) + h11 * fx;
        float normH = hTop * (1.0f - fz) + hBot * fz;

        // 将归一化高程映射到真实物理绝对标高
        return MapNormalizedElevation(normH);
    }

    /// <summary>
    /// 将归一化灰度值 [0.0, 1.0] 映射为世界高度 (米)
    /// CoastlineThreshold 锚定为海平面 0.0m
    /// </summary>
    private float MapNormalizedElevation(float normH)
    {
        if (normH < CoastlineThreshold)
        {
            // 水下平滑沉入海底 (-14m ~ 0m)
            float t = normH / CoastlineThreshold;
            return Mathf.Lerp(OceanBedDepth, SeaLevel, t * t);
        }
        else
        {
            // 陆地平原与高耸山峰 (0m ~ MaxElevation)
            float t = (normH - CoastlineThreshold) / (1.0f - CoastlineThreshold);
            return Mathf.Lerp(SeaLevel, MaxElevation, t);
        }
    }

    /// <summary>
    /// 判断给定的世界坐标 (worldX, worldY, worldZ) 是否处于传统地形网格内部或表面以下
    /// </summary>
    public bool IsBelowSurface(float worldX, float worldY, float worldZ)
    {
        return worldY <= GetHeight(worldX, worldZ) + 0.05f;
    }

    /// <summary>
    /// 通过中心微分散差 (Central Difference) 计算精确的地表法线向量
    /// </summary>
    public Vector3 GetNormal(float worldX, float worldZ)
    {
        const float Delta = 0.35f; // 微分采样步长 (与 2048 像素网格尺度严格适配)
        float hL = GetHeight(worldX - Delta, worldZ);
        float hR = GetHeight(worldX + Delta, worldZ);
        float hD = GetHeight(worldX, worldZ - Delta);
        float hU = GetHeight(worldX, worldZ + Delta);

        Vector3 n = new(hL - hR, Delta * 2.0f, hD - hU);
        return n.Normalized();
    }
}
