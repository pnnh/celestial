using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 海岛高度场参数配置
/// </summary>
public record struct IslandHeightSamplerConfig
{
    public int Seed { get; init; } = 1337;
    public float IslandRadius { get; init; } = 380.0f;       // 核心陆地半径（米）
    public float CoastlineRadius { get; init; } = 460.0f;    // 海滩渐变至海平面半径（米）
    public float SeaLevel { get; init; } = 0.0f;             // 海平面绝对标高（米）
    public float OceanBedDepth { get; init; } = -18.0f;      // 外围大洋底面深度（米）

    // 宏观平原微波
    public float PlainsFrequency { get; init; } = 0.0035f;   // 约 280m 波长
    public float PlainsAmplitude { get; init; } = 8.0f;      // 平原起伏幅度 8 米
    public float PlainsBaseHeight { get; init; } = 7.0f;     // 平原基底标高 7 米

    // 内陆山脊
    public float MountainFrequency { get; init; } = 0.003f;  // 约 330m 波长
    public float MountainAmplitude { get; init; } = 65.0f;   // 山峰最大拔高
    public float MountainRadius { get; init; } = 240.0f;     // 山体集中影响半径
    public Vector2 MountainCenterOffset { get; init; } = new(110.0f, -80.0f); // 山峰偏向东北内陆

    // 微观细节
    public float DetailFrequency { get; init; } = 0.045f;    // 约 22m 波长
    public float DetailAmplitude { get; init; } = 1.8f;      // 微观细节起伏

    public IslandHeightSamplerConfig() { }
}

/// <summary>
/// 千米级复合海岛高度场采样器
/// 实现 O(1) 确定性数学分形求值，支持无锁多线程并发调用
/// </summary>
public partial class IslandHeightSampler : RefCounted, IHeightSampler
{
    public IslandHeightSamplerConfig Config { get; }
    public float SeaLevel => Config.SeaLevel;
    public int Seed => Config.Seed;

    private readonly float _seedOffsetX;
    private readonly float _seedOffsetZ;

    public IslandHeightSampler() : this(null)
    {
    }

    public IslandHeightSampler(IslandHeightSamplerConfig? config)
    {
        Config = config ?? new IslandHeightSamplerConfig();
        // 基于 Seed 生成固定的坐标扰动，保证确定性
        var rng = new Random(Config.Seed);
        _seedOffsetX = (float)(rng.NextDouble() * 1000.0 - 500.0);
        _seedOffsetZ = (float)(rng.NextDouble() * 1000.0 - 500.0);
    }

    /// <summary>
    /// 获取世界平面坐标 (worldX, worldZ) 处的地表绝对高度 Y (米)
    /// </summary>
    public float GetHeight(float worldX, float worldZ)
    {
        // 1. 距离海岛中心 (0, 0) 的水平径向距离
        float r = MathF.Sqrt(worldX * worldX + worldZ * worldZ);

        // 2. 宏观岛屿径向衰减遮罩 (Radial Island Mask)
        float islandMask;
        if (r <= Config.IslandRadius)
        {
            islandMask = 1.0f;
        }
        else if (r >= Config.CoastlineRadius)
        {
            islandMask = 0.0f;
        }
        else
        {
            float t = (r - Config.IslandRadius) / (Config.CoastlineRadius - Config.IslandRadius);
            // 三次 Smoothstep 平滑曲线: 1 - (3t^2 - 2t^3)
            islandMask = 1.0f - (t * t * (3.0f - 2.0f * t));
        }

        // 3. 宏观平原微波丘陵 (Plains Waves)
        float sx = worldX + _seedOffsetX;
        float sz = worldZ + _seedOffsetZ;
        float pFreq = Config.PlainsFrequency * MathF.PI * 2.0f;

        float p1 = MathF.Sin(sx * pFreq) * MathF.Cos(sz * pFreq);
        float p2 = MathF.Sin((sx * 0.7f + sz * 1.3f) * pFreq) * 0.5f;
        float plains = (p1 + p2) * Config.PlainsAmplitude + Config.PlainsBaseHeight;

        // 4. 内陆巍峨山脊 (Mountain Ridge)
        float mx = worldX - Config.MountainCenterOffset.X;
        float mz = worldZ - Config.MountainCenterOffset.Y;
        float mDist = MathF.Sqrt(mx * mx + mz * mz);

        float mountain = 0.0f;
        if (mDist < Config.MountainRadius)
        {
            float mt = mDist / Config.MountainRadius;
            float mountainMask = (1.0f - mt * mt) * (1.0f - mt * mt); // 四次平滑衰减
            float peakProfile = 1.0f - mt; // 锥形山脊轮廓

            float mFreq = Config.MountainFrequency * MathF.PI * 2.0f;
            float mRidge = MathF.Abs(MathF.Cos(mx * mFreq) * MathF.Cos(mz * mFreq));
            mountain = (peakProfile * 0.65f + mRidge * 0.35f) * Config.MountainAmplitude * mountainMask;
        }

        // 5. 微观地表细节 (Micro Detail)
        float dFreq = Config.DetailFrequency * MathF.PI * 2.0f;
        float detail = MathF.Sin(sx * dFreq) * MathF.Cos(sz * dFreq) * Config.DetailAmplitude;

        // 6. 陆地总高度与大洋底面过渡插值
        float landHeight = plains + mountain + detail;
        float finalHeight = Mathf.Lerp(Config.OceanBedDepth, landHeight, islandMask);

        return finalHeight;
    }

    /// <summary>
    /// 判断给定的世界坐标是否处于传统地形网格内部或表面以下
    /// </summary>
    public bool IsBelowSurface(float worldX, float worldY, float worldZ)
    {
        return worldY <= GetHeight(worldX, worldZ) + 0.05f;
    }

    /// <summary>
    /// 使用中心差分法计算指定世界坐标处的精确地表法线向量 (归一化)
    /// </summary>
    public Vector3 GetNormal(float worldX, float worldZ)
    {
        const float delta = 0.5f;
        float hL = GetHeight(worldX - delta, worldZ);
        float hR = GetHeight(worldX + delta, worldZ);
        float hD = GetHeight(worldX, worldZ - delta);
        float hU = GetHeight(worldX, worldZ + delta);

        // 切线向量与次切线向量外积
        var normal = new Vector3(hL - hR, delta * 2.0f, hD - hU).Normalized();
        return normal;
    }
}
