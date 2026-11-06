# Celestial 美术风格与视觉资产规范 (Art Style Guide)

**文档版本**：v1.0  
**适用范围**：AI 生成资产 (Ludo AI / Blender MCP)、3D 建模、纹理绘制、着色器配置与场景渲染  
**确立日期**：2026-11-06  

---

## 1. 核心视觉基调与艺术定位

Celestial 的视觉风格定位为**“日系清爽手绘赛璐璐幻想风”（Stylized Anime Cel-Shading / Ghibli & BotW Inspired Fantasy）**。

### 1.1 核心美学关键词
- **清爽与辽阔**：高明度、中等饱和度的暖调日光，开阔的蔚蓝天空与无垠草甸。
- **手绘笔触感**：纹理避免写实机械噪点与粗糙凹凸贴图，保留大色块涂抹与手绘笔触质感。
- **阶梯光影（Toon / Cel Shading）**：受光面与背光面过渡清晰柔和，呈现动画剧场版的温润光影。
- **鲜明剪影（Silhouette）**：角色服装、古代图腾柱与手持工具具有清晰的辨识外轮廓。

---

## 2. 版权边界与原创防侵权规避清单 (Anti-Infringement)

为确保本项目商业与开源安全，严格划定与任天堂《塞尔达传说：旷野之息》的法律与版权边界：

| 领域 | 严格禁止的侵权要素 (Forbidden) | Celestial 专属原创设计规范 (Allowed & Mandated) |
| :--- | :--- | :--- |
| **古代科技与文明** | 希卡之眼（单眼加泪滴）、希卡石板、神庙橙/蓝电路导线 | **星穹古阵（Celestial Runes）**：几何八面体/切面图腾、幽蓝与青绿交织的以太晶石（Aether Glow）、粗犷风化花岗岩 |
| **主角造型** | 蓝色英杰服饰纹样、林克尖耳/金色侧马尾/面孔特征 | **天界漫游者（Celestial Wanderer）**：亚麻浅色探险短风衣、皮革多功能腰带/挂包、旅行半指手套、深棕短靴、中性青年旅者形象 |
| **装备与武器** | 大师剑、海利亚盾牌、希卡滑翔伞造型 | **远古水晶镐（Crystal Pickaxe）**、以太能量滑翔翼、多功能地质勘探杖 |
| **交互与 UI** | 复制塞尔达特定字形排版与标志性音效 | 现代化极简 HUD：细线框圆角毛玻璃微质感，青色与琥珀色状态反馈 |

---

## 3. 标准色彩调色板 (Color Palette)

- **天空与以太**：
  - Cerulean Sky (`#6CB8EE`)
  - Aether Crystal Glow (`#1BE3CF` / `#00FFE1`)
- **地表与植被**：
  - Lush Meadow Grass (`#6BBF59`)
  - Warm Sunlit Earth (`#8C6239`)
  - Bedrock Granite (`#2C3038`)
- **角色服饰**：
  - Explorer Linen (`#E8E0D2`)
  - Adventure Leather (`#5C3A21`)
  - Bronze Accents (`#C28D47`)

---

## 4. AI 提示词规范词典 (Prompt Dictionary for Ludo AI & MCP)

当调用 Ludo AI 生成 2D 概念图、3D 模型与纹理时，必须使用以下标准化提示词结构：

### 4.1 正向通用修饰词 (Positive Modifiers)
```text
stylized 3d model, anime cel-shaded aesthetic, vibrant warm colors, ghibli inspired fantasy, clean silhouette, hand-painted texture, soft ambient occlusion, high quality game asset, neutral A-pose
```

### 4.2 负向过滤词 (Negative Modifiers)
```text
photorealistic, noisy textures, dirty realism, dark gritty, sheikah eye, zelda master sword, deformed hands, asymmetry, blurry details, complex modern military gear
```

### 4.3 角色正面立绘标准化模板
```text
Full body hero adventurer, front view, standing neutral A-pose on pure white background, stylized anime cel-shaded aesthetic inspired by Ghibli fantasy, young explorer in light linen tunic, leather utility belt with pouches, fingerless gloves, sturdy brown adventure boots, clean silhouette, bright warm lighting, studio 3d render quality
```

---

## 5. 3D 资产建模与绑定技术规范

- **面数控制**：主角角色控制在 25,000 ~ 35,000 三角面，兼顾第一人称近景手臂细节与第三人称运行帧率。
- **骨骼规范**：采用标准 Godot 人形骨骼命名（`joint_naming: "godot"`），必须包含完整的头部（`Head`）、胸腔（`Chest`）、双臂双腿及左右手腕骨（`Hand.R`, `Hand.L`）。
- **手持插槽**：使用 Godot 原生 `BoneAttachment3D` 动态挂接武器与工具，禁止将工具与角色网格合并。
- **动画循环**：所有行进与待移动态必须以 `in_place: true` 导出，由引擎驱动位移。

