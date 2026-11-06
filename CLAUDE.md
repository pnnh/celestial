# Celestial 项目说明 (Claude Agent 指南)

## 基本规则

- 始终使用**中文**进行回复和输出所有项目文档。
- 规划与设计文档统一存放在 `docs/plan/` 目录下。
- **关于源码提交**：执行完任务之后**严禁**自动提交 Git，等待开发者查看改动内容后再决定是否提交。

## 项目架构与技术规范

- **引擎**：Godot Engine 4.7+ Mono Edition (C# / .NET 8)
- **Godot 路径**：`/Applications/Godot_mono.app/Contents/MacOS/Godot`
- **构建与测试**：
  - 构建：`DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj`
  - 依赖还原：`DOTNET_CLI_HOME=/tmp/dotnet_home dotnet restore celestial.csproj`
- **代码规范**：
  - C# 命名遵循标准 PascalCase（类、公共成员、方法）和 camelCase（局部变量、参数）。
  - Godot 场景与节点脚本解耦，保持模块化。
  - 体素网格计算与物理更新需考虑分块异步或基于任务调度，避免帧率抖动。

## Spec-Kit (Speckit) 流程

项目集成了 GitHub Spec Kit 规范驱动开发流程：
- 原则宪章：`.specify/memory/constitution.md`
- 规格制定：`/speckit-specify`
- 技术规划：`/speckit-plan`
- 任务拆解：`/speckit-tasks`
- 代码实现：`/speckit-implement`

## MCP 工具与资源生成

- 本地已配置 Blender MCP 与 Ludo AI MCP，按需调用生成 3D 模型、关卡网格与材质纹理。
- 遵循 `.gitattributes` 与 Git LFS 规范，模型与大贴图自动进入 LFS，避免仓库体积膨胀。

