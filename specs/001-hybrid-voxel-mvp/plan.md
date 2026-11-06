# Implementation Plan: Hybrid 3D Terrain and Voxel World MVP

**Branch**: `001-hybrid-voxel-mvp`  
**Spec**: `specs/001-hybrid-voxel-mvp/spec.md`  
**Architecture Document**: `docs/plan/hybrid_voxel_mvp_feasibility_and_architecture.md`  

## Proposed Changes

### 1. Core Architecture & Project Layout (`src/` & `assets/`)

```
src/
├── Core/
│   ├── GameManager.cs               # 游戏主入口与场景协调
│   ├── Constants.cs                 # Chunk 尺寸 (16)、世界网格全局常量
│   ├── DebugOverlay.cs              # 3D 瞄准线框与性能实时监视器 (F3)
│   ├── HUDOverlay.cs                # 屏幕快捷操作指引、方块指示器与装备栏图标
│   └── TestRunner.cs                # 游戏初始化核心数学与序列化自测套件
├── Terrain/
│   ├── IHeightSampler.cs            # 传统地形高度采样接口
│   ├── StaticTerrain.cs             # 传统地形节点封装、采样器实现与草地贴图绑定
│   └── SimpleProceduralHeightmap.cs # 基于数学曲线/噪声的小型测试地形生成器
├── Voxel/
│   ├── VoxelType.cs                 # 方块枚举定义 (Air, Grass, Dirt, Stone, Bedrock, Wood)
│   ├── ChunkCoord.cs                # 结构体：三维 Chunk 坐标与索引哈希
│   ├── VoxelChunk.cs                # 核心 Chunk 数据容器 (16x16x16)
│   ├── VoxelWorld.cs                # Chunk 字典、区域触发、Texture Atlas 材质绑定
│   └── ChunkMesher.cs               # 面剔除、定向 UV 贴图与 ArrayMesh 生成器
├── Player/
│   ├── PlayerController.cs          # 第一人称移动、跳跃、重力控制与数字键 1-5 切换方块
│   └── VoxelInteractor.cs           # 视线射线检测、挖掘触底阻隔、方块放置
└── Persistence/
    ├── ChunkDelta.cs                # Chunk 差异数据模型
    └── SimpleSaveManager.cs         # JSON/二进制轻量差异存读档管理器 (F5/F9)

assets/
└── textures/
    ├── voxel_atlas.png              # 3x2 高性能体素纹理图集 (LFS 纳管)
    └── ludo/                        # 生成的原始高分辨率贴图库与装备图标
```

---

## Technical Approach & Key Contracts

### 2.1 高度采样与对齐接口 (`IHeightSampler`)
```csharp
namespace Celestial.Terrain;

public interface IHeightSampler
{
    /// <summary>
    /// 查询世界坐标 (worldX, worldZ) 处传统 3D 地形的表面高度
    /// </summary>
    float GetHeight(float worldX, float worldZ);

    /// <summary>
    /// 判断给定的世界坐标 (x, y, z) 是否处于传统地形实体内部或表面以下
    /// </summary>
    bool IsBelowSurface(float worldX, float worldY, float worldZ);
}
```

### 2.2 体素数据结构、面剔除网格与纹理图集管线 (`ChunkMesher`)
- **存储**：`byte[16 * 16 * 16]` 扁平化连续数组存储方块 ID。
- **面剔除（Face Culling）**：
  - 遍历 Chunk 内每个有效非空方块，检查其 6 个相邻方向（$X \pm 1, Y \pm 1, Z \pm 1$）。
  - 若邻居方块为空气（Air）或超出当前 Chunk 且邻近 Chunk 为空气，则生成该方向的 2 个三角形面（4 个顶点、6 个索引）。
  - 若底部接触传统地形表面，则通过采样高度生成贴合底面。
- **纹理图集与多面定向 UV 映射（Texture Atlas UVs）**：
  - 统一采用 3×2 网格图集 (`res://assets/textures/voxel_atlas.png`)。
  - 草方块（Grass）顶面映射草皮 (0, 0)，底面映射泥土 (1, 0)，四周侧面映射垂草泥土截面 (2, 1)。
  - 泥土 (1, 0)、木板 (2, 0)、石砖 (0, 1)、远古符文基岩 (1, 1) 全表面各向同性映射。
  - UV 边界增加微小抗漏色偏移（$\epsilon = 0.002$），材质过滤器设为 `NearestWithMipmaps`。
  - 结合面朝向引入简易方向性环境遮蔽（Directional AO）：顶面亮度 1.0、侧面 0.88/0.78、底面 0.55。
- **网格输出**：`Godot.ArrayMesh`，包含 `Vector3[]` 顶点、`Vector3[]` 法线、`Vector2[]` UV、`Color[]` 明暗顶点色、`int[]` 索引。
- **碰撞输出**：赋值给 `ConcavePolygonShape3D.Faces`。

### 2.3 挖掘触底机制
- 射线检测击中体素后：
  $$\text{voxelY} = \lfloor \text{hitPoint.Y} - 0.5 \times \text{hitNormal.Y} \rfloor$$
- 检查 `sampler.GetHeight(voxelX, voxelZ)`：
  - 若 $\text{voxelY} \le \lfloor \text{terrainHeight} \rfloor$，判定为**基岩/传统地形边界**。
  - 拒绝修改，播放触底不可破坏音效/控制台警告。
  - 否则，设置该体素为 `VoxelType.Air`，标记当前 Chunk 及边界邻居 Chunk 为 `Dirty`。

---

## Verification Plan

### Automated / Tooling Tests
- **编译检查**：`DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj` 持续保证 0 错误、0 警告。
- **单元测试**：针对坐标转换（WorldPos <-> ChunkCoord <-> LocalVoxelPos）进行单元测试验证。

### Manual / Visual Verification
1. **测试场景运行**：启动 `scenes/main.tscn`，摄像机或第一人称角色能够观察到清晰的起伏传统地形与上层嵌入的体素泥土层。
2. **边缘无漏缝验证**：环绕体素边界观察，体素与传统地面完美吸附。
3. **连续挖掘测试**：垂直向下挖洞，测试挖出 3~5 格方块后直达坚硬的不可挖掘地面，无法继续向下开挖。
4. **建造与碰撞测试**：右键堆叠 3 块石头方块，角色跳上并站在上面。
5. **跨边界测试**：在两个 16×16×16 Chunk 的交接缝处挖掘，两侧网格同步刷新无空洞。
