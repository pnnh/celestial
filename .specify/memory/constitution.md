# Celestial 混合世界架构项目宪章 (Project Constitution)

## 核心原则 (Core Principles)

### I. 模块化与清晰边界 (Modular Subsystems)
- 底层传统地形（Static Terrain）、上层体素引擎（Voxel Engine）、角色交互（Interaction/Player）和数据持久化（Save System）必须严格解耦。
- 体素引擎不直接依赖具体的游戏逻辑，只对外暴露标准 API：查询（Query）、挖掘（Dig）、放置（Place）、射线检测（Raycast）、碰撞体更新（Collision Sync）。

### II. 性能与异步优先 (Performance & Async First)
- 性能是沙盒体素系统的生命线：Chunk 网格重建（Meshing）必须与主渲染线程解耦，支持后台异步任务执行或队列分帧处理，杜绝主线程卡顿（Frame Drop）。
- 内存分配控制：高频运算（如体素顶点提取、光照计算、网格生成）严格避免每帧或每次操作产生垃圾回收（GC Allocations），推荐使用 `ArrayPool`、值类型结构体（`struct`）和原生数组。

### III. 混合衔接与物理确定性 (Hybrid Alignment & Deterministic Physics)
- 高度对齐确定性：体素层底部边界必须严格锚定传统 3D 地形的高度采样，保证挖掘到底部时具有明确的“基岩判定”（Bedrock Detection），禁止出现漂浮或穿模空隙。
- 碰撞一致性：体素动态网格与底层静态碰撞体在边缘过渡区域必须平滑衔接，使用统一的物理层级（Collision Layers）和无缝缝合算法，防止角色卡缝或误跌入虚空。

### IV. 渐进式算法演进 (Iterative Algorithm Evolution)
- 表面网格重建遵循三阶段演进：
  1. MVP 阶段：标准立方体渲染（Cubic Voxel）+ 简单面剔除 / 贪婪网格化（Greedy Meshing）。
  2. 优化阶段：高度插值与边缘平滑材质过渡。
  3. 进阶阶段：Surface Nets / Dual Contouring 拟合平滑地形。
- 绝不在原型未跑通前过早实现极其复杂的高级平滑算法。

### V. 资产规范与轻量化 (Asset Hygiene & Git LFS)
- 严防仓库体积膨胀：所有二进制资源（.blend, .fbx, .glb, .png, .wav 等）必须纳入 Git LFS。
- 临时产物与烘焙缓存（如 .godot、C# bin/obj、Blender .blend1）严禁入库。

### VI. 视觉与艺术风格宪章 (Stylized Anime Fantasy / 赛璐璐手绘幻想风)
- **核心基调**：借鉴吉卜力与日系清爽手绘幻想风格（Stylized Anime Cel-Shading）。追求明媚开阔的暖阳蓝天、翠绿草甸、古铜与风化石质感；光影采用二阶平滑卡通着色（Toon Shading），杜绝高噪点写实材质。
- **版权规避原则 (Anti-Infringement Guardrails)**：
  - 严格禁止直接模仿或借用《塞尔达传说：旷野之息》的专有侵权符号（如希卡之眼、希卡石板、神庙橙蓝电路、大师剑造型、特定角色发型面孔）。
  - 确立 Celestial 独有世界观视觉符号：**星穹古阵（Celestial Runes）**、**以太幽蓝晶石（Aether Crystals）**、**天界漫游者（Celestial Wanderer）**装束。
- **AI 资产提示词约束**：所有通过 Ludo AI 或 Blender MCP 调用的提示词，必须注入赛璐璐动漫风与正负词典约束，保证角色、地表、装备三者美术语言高度统一。

## 开发工作流与治理 (Development & Governance)

- **开发范式**：遵循 Spec-Kit 流程（Specification -> Plan -> Tasks -> Implement -> Verification）。
- **质量门禁**：所有提交需确保 `dotnet build` 零编译错误；核心数据结构与算法需具备单元测试覆盖。
- **提交控制**：AI Agent 严禁在任务结束后自动执行 Git 提交，由主开发者核对改动后手动提交。

**版本**: 1.0.0 | **确立日期**: 2026-11-06 | **状态**: 有效
