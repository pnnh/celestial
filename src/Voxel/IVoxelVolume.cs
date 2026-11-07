using System.Collections.Generic;
using Godot;

namespace Celestial.Voxel;

/// <summary>
/// 局部体素卷契约接口
/// 支持在特定区域（农田、采矿坑、塌方山洞）按需激活体素地幔
/// </summary>
public interface IVoxelVolume
{
    /// <summary>
    /// 体素卷唯一标识符（用于差异化持久化存档）
    /// </summary>
    string VolumeId { get; }

    /// <summary>
    /// 该体素卷覆盖的世界空间 AABB 包围盒
    /// </summary>
    Aabb WorldBounds { get; }

    /// <summary>
    /// 判定给定的世界坐标是否位于本局部体素卷的管理范围之内
    /// </summary>
    bool ContainsWorldPosition(Vector3 worldPos);

    /// <summary>
    /// 查询指定世界坐标处的体素类型
    /// </summary>
    VoxelType GetVoxelAtWorld(Vector3 worldPos);

    /// <summary>
    /// 在指定世界坐标处尝试挖掘方块
    /// </summary>
    bool TryDigAtWorld(Vector3 worldPos, out VoxelType dugType);

    /// <summary>
    /// 在指定世界坐标处尝试放置方块
    /// </summary>
    bool TryPlaceAtWorld(Vector3 worldPos, VoxelType type);

    /// <summary>
    /// 导出当前体素卷被玩家修改的增量数据
    /// </summary>
    Dictionary<Vector3I, byte> ExportModifications();

    /// <summary>
    /// 导入增量修改数据并刷新相关网格
    /// </summary>
    void ImportModifications(Dictionary<Vector3I, byte> modifications);
}

