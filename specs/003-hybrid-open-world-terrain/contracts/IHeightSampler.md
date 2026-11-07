# Interface Contract: IHeightSampler

**Namespace**: `Celestial.Terrain`  
**Purpose**: 定义大世界地貌高度采样与表面法线计算的标准契约，确保 3D 地形与体素世界共享唯一真理源。

---

## C# 接口契约定义

```csharp
namespace Celestial.Terrain;

public interface IHeightSampler
{
    /// <summary>
    /// 获取指定世界 (X, Z) 平面坐标处的地表真实高度 Y (米)。
    /// 必须保证线程安全，支持并发无锁查询。
    /// </summary>
    /// <param name="x">世界 X 坐标</param>
    /// <param name="z">世界 Z 坐标</param>
    /// <returns>绝对地表标高 Y</returns>
    float GetHeight(float x, float z);

    /// <summary>
    /// 计算指定世界坐标处的解析/近似地表法线向量 (归一化)。
    /// </summary>
    Vector3 GetNormal(float x, float z);

    /// <summary>
    /// 判定指定点是否位于海平面水下。
    /// </summary>
    bool IsUnderwater(float x, float z, float seaLevel = 0.0f);

    /// <summary>
    /// 获取地形的全局海平面高度。
    /// </summary>
    float SeaLevel { get; }

    /// <summary>
    /// 获取生成种子。
    /// </summary>
    int Seed { get; }
}
```

---

## 行为与性能保障

1. **确定性**: 相同 `(x, z)` 坐标的多次查询结果必须完全严格一致（$|y_1 - y_2| = 0$）。
2. **时间复杂度**: 查询耗时必须小于 500ns，杜绝任何磁盘 I/O 或复杂堆分配。
3. **线程安全性**: 允许多个后台工作线程并发调用 `GetHeight`，内部无共享可变状态。

