using System;
using System.IO;
using System.Text.Json;
using Celestial.Voxel;
using Godot;

namespace Celestial.Persistence;

/// <summary>
/// 轻量级差异存档与读取管理器 (按 F5 快捷保存，按 F9 快捷载入)
/// </summary>
public partial class SimpleSaveManager : Node
{
    [Export] public string SaveFilePath = "user://celestial_delta_save.json";
    [Export] public VoxelWorld? World;

    public override void _Ready()
    {
        if (World == null)
        {
            World = GetTree().Root.FindChild("VoxelWorld", true, false) as VoxelWorld;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.F5)
            {
                Save();
            }
            else if (key.Keycode == Key.F9)
            {
                Load();
            }
        }
    }

    /// <summary>
    /// 执行差异保存
    /// </summary>
    public bool Save()
    {
        if (World == null)
        {
            GD.PrintErr("[SaveManager] 存档失败: VoxelWorld 实例未找到！");
            return false;
        }

        WorldSaveData saveData = World.ExportDeltaSaveData();
        string globalPath = ProjectSettings.GlobalizePath(SaveFilePath);

        try
        {
            string? dir = Path.GetDirectoryName(globalPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(globalPath, json);

            int totalChanges = 0;
            foreach (ChunkDelta cd in saveData.Chunks)
            {
                totalChanges += cd.Diffs.Count;
            }

            GD.Print($"[SaveManager] 差异存档成功！已保存 {saveData.Chunks.Count} 个修改 Chunk，共 {totalChanges} 处方块改动 -> {SaveFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveManager] 存档写入异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行差异读取与世界还原
    /// </summary>
    public bool Load()
    {
        if (World == null)
        {
            GD.PrintErr("[SaveManager] 读档失败: VoxelWorld 实例未找到！");
            return false;
        }

        string globalPath = ProjectSettings.GlobalizePath(SaveFilePath);
        if (!File.Exists(globalPath))
        {
            GD.Print($"[SaveManager] 读档跳过: 未找到存档文件 {SaveFilePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(globalPath);
            WorldSaveData? saveData = JsonSerializer.Deserialize<WorldSaveData>(json);

            if (saveData == null)
            {
                GD.PrintErr("[SaveManager] 读档失败: 存档反序列化为空！");
                return false;
            }

            World.ImportDeltaSaveData(saveData);

            int totalChanges = 0;
            foreach (ChunkDelta cd in saveData.Chunks)
            {
                totalChanges += cd.Diffs.Count;
            }

            GD.Print($"[SaveManager] 差异读档成功！已还原 {saveData.Chunks.Count} 个 Chunk，共 {totalChanges} 处方块改动！");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveManager] 读档异常: {ex.Message}");
            return false;
        }
    }
}

