# 基本规则

使用中文回复和输出项目文档。

# 规划与设计文档说明

如果需要写一些规划或者设计文档，可以写在docs/plan目录下。

# 关于源码提交

执行完任务之后不要提交Git，我需要先查看改动的内容，再决定是否提交。

# 项目技术栈与工程约定

1. **引擎与运行环境**：
   - 引擎：Godot Engine 4.7+ Mono (.NET 8.0)
   - Godot 可执行路径：`/Applications/Godot_mono.app/Contents/MacOS/Godot`
   - C# 语言特性：C# 12 / .NET 8，开启可空检查与隐式引用。
2. **构建与运行**：
   - 构建命令：`DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj`
   - 依赖还原：`DOTNET_CLI_HOME=/tmp/dotnet_home dotnet restore celestial.csproj`
3. **Spec-Driven Development (Speckit) 流程**：
   - 规范与任务驱动开发：遵循 Spec-Kit 流程（`/speckit-specify` -> `/speckit-plan` -> `/speckit-tasks` -> `/speckit-implement`）。
   - 规范存储：`.specify/` 与 `specs/` 目录。
4. **外部工具与生成资产 (MCP)**：
   - Blender MCP：可用于程序化生成 3D 模型、关卡测试网格、碰撞体。
   - Ludo AI MCP：用于概念图、纹理及原型参考生成。
   - 大体积资源（3D 网格、模型、高分辨率贴图、音频）受 Git LFS 追踪管理，严格避免将临时中间构建产物直接提交到仓库。
