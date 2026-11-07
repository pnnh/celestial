# Interface Contract: IVoxelVolume & IBedrockPhysicsDetector

**Namespace**: `Celestial.Voxel` / `Celestial.Interaction`  
**Purpose**: 规范局部体素卷接口与普适基岩射线检测，实现 3D 静态实体与体素方块的统一交互。

---

## 1. IVoxelVolume 接口契约

```csharp
namespace Celestial.Voxel;

public interface IVoxelVolume
{
    /// <summary>
    /// 体素卷唯一标识符（用于差异化持久化存档）。
    /// </summary>
    string VolumeId { get; }

    /// <summary>
    /// 该体素卷覆盖的世界空间 AABB 包围盒。
    /// </summary>
    Aabb WorldBounds { get; }

    /// <summary>
    /// 判定给定的世界坐标是否位于本局部体素卷的管理范围之内。
    /// </summary>
    bool ContainsWorldPosition(Vector3 worldPos);

    /// <summary>
    /// 查询指定世界坐标处的体素类型。
    /// 若超出本卷范围返回 VoxelType.Air；若低于 3D 地形高度返回 VoxelType.Bedrock。
    /// </summary>
    VoxelType GetVoxelAtWorld(Vector3 worldPos);

    /// <summary>
    /// 在指定世界坐标处尝试挖掘方块。
    /// 若目标属于不可破坏的基岩或 3D 实体内部，返回 false。
    /// </summary>
    bool TryDigAtWorld(Vector3 worldPos, out VoxelType dugType);

    /// <summary>
    /// 在指定世界坐标处尝试放置方块。
    /// </summary>
    bool TryPlaceAtWorld(Vector3 worldPos, VoxelType type);

    /// <summary>
    /// 导出当前体素卷被玩家修改的增量数据。
    /// </summary>
    Dictionary<Vector3I, byte> ExportModifications();

    /// <summary>
    /// 导入增量修改数据并刷新相关 Chunk 网格。
    /// </summary>
    void ImportModifications(Dictionary<Vector3I, byte> modifications);
}
```

---

## 2. IBedrockPhysicsDetector 接口契约

```csharp
namespace Celestial.Interaction;

public interface IBedrockPhysicsDetector
{
    /// <summary>
    /// 从摄像机向视线前方发射射线，综合判定命中的是可破坏体素还是不可破坏的基岩。
    /// </summary>
    /// <param name="origin">射线起点</param>
    /// <param name="direction">视线方向向量</param>
    /// <param name="maxDistance">最大探测距离（如 6.0 米）</param>
    /// <returns>标准化交互判定结果</returns>
    InteractionRaycastResult CastRay(Vector3 origin, Vector3 direction, float maxDistance = 6.0f);
}
```

