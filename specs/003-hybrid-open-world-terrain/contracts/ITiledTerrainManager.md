# Interface Contract: ITiledTerrainManager

**Namespace**: `Celestial.Terrain`  
**Purpose**: 负责开放世界 64m 瓦片流式调度、LOD 视距分级、后台多线程分块网格计算与主线程节流挂载。

---

## C# 接口契约定义

```csharp
namespace Celestial.Terrain;

public interface ITiledTerrainManager
{
    /// <summary>
    /// 更新观察者（通常为玩家或主摄像机）当前的世界坐标，触发瓦片视距计算与流式调度。
    /// 应在主线程每帧或每隔固定间隔调用。
    /// </summary>
    /// <param name="observerPosition">观察者世界坐标</param>
    void UpdateObserverPosition(Vector3 observerPosition);

    /// <summary>
    /// 强制重新构建指定瓦片坐标的几何网格。
    /// </summary>
    void InvalidateTile(TerrainTileCoord coord);

    /// <summary>
    /// 获取当前处于活动状态（已挂载到场景树）的瓦片总数。
    /// </summary>
    int ActiveTileCount { get; }

    /// <summary>
    /// 获取当前等待后台线程计算或等待主线程挂载的队列长度。
    /// </summary>
    int PendingJobCount { get; }

    /// <summary>
    /// 单个瓦片的边长尺寸（默认为 64.0 米）。
    /// </summary>
    float TileSize { get; }

    /// <summary>
    /// 底层使用的高度场采样器接口。
    /// </summary>
    IHeightSampler HeightSampler { get; }
}
```

---

## 性能与帧预算限制

1. **主线程耗时上限**: 每帧在 `_Process` 中挂载瓦片（`AddSurfaceFromArrays`）的耗时累计不得超过 **3.0ms**。超过上限的就绪瓦片顺延至下一帧挂载。
2. **后台线程池并发**: 使用系统线程池，最大并发工作任务数不超过 `Environment.ProcessorCount - 1`，保证不挤占主渲染线程 CPU。

