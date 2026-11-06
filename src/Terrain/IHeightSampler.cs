using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 传统 3D 地形高度与表面采样契约接口
/// </summary>
public interface IHeightSampler
{
    /// <summary>
    /// 获取世界平面坐标 (worldX, worldZ) 处传统地形的最高表面高度 (Y 值)
    /// </summary>
    float GetHeight(float worldX, float worldZ);

    /// <summary>
    /// 判断给定的世界坐标 (worldX, worldY, worldZ) 是否处于传统地形网格内部或表面以下
    /// </summary>
    bool IsBelowSurface(float worldX, float worldY, float worldZ);

    /// <summary>
    /// 获取世界平面坐标 (worldX, worldZ) 处的地形表面法线
    /// </summary>
    Vector3 GetNormal(float worldX, float worldZ);
}

