using Celestial.Player;
using Celestial.Terrain;
using Celestial.Voxel;
using Godot;

namespace Celestial.Core;

/// <summary>
/// 调试可视化与性能监控覆盖层 (按 F3 切换)
/// 包含准心体素方块线框选择框、系统指标、高度采样与 Chunk 信息
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    private bool _isVisible = false;
    private Label _debugLabel = null!;
    private Panel _debugPanel = null!;

    // 3D 瞄准体素方块线框高亮器
    private MeshInstance3D _selectionBox = null!;
    private ImmediateMesh _wireframeMesh = null!;
    private StandardMaterial3D _selectionMaterial = null!;

    private PlayerController? _player;
    private VoxelInteractor? _interactor;
    private VoxelWorld? _world;

    public override void _Ready()
    {
        // 1. 创建调试文本面板
        _debugPanel = new Panel
        {
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            OffsetLeft = 20,
            OffsetTop = 150,
            OffsetRight = 380,
            OffsetBottom = 430,
            Visible = false
        };

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0.1f, 0.1f, 0.12f, 0.75f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8
        };
        _debugPanel.AddThemeStyleboxOverride("panel", panelStyle);

        _debugLabel = new Label
        {
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            OffsetLeft = 12,
            OffsetTop = 12,
            OffsetRight = -12,
            OffsetBottom = -12
        };
        _debugLabel.AddThemeFontSizeOverride("font_size", 13);
        _debugLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.95f, 0.85f));
        _debugPanel.AddChild(_debugLabel);
        AddChild(_debugPanel);

        // 2. 创建 3D 体素高亮选择框
        _selectionMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.2f, 0.8f, 1.0f, 0.8f),
            NoDepthTest = false,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };

        _wireframeMesh = new ImmediateMesh();
        _selectionBox = new MeshInstance3D
        {
            Name = "VoxelSelectionBox",
            Mesh = _wireframeMesh,
            Visible = false
        };
        GetTree().Root.CallDeferred("add_child", _selectionBox);

        _player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
        _interactor = GetTree().Root.FindChild("VoxelInteractor", true, false) as VoxelInteractor;
        _world = GetTree().Root.FindChild("VoxelWorld", true, false) as VoxelWorld;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.F3)
        {
            _isVisible = !_isVisible;
            _debugPanel.Visible = _isVisible;
            GD.Print($"[DebugOverlay] 调试面板状态: {(_isVisible ? "开启" : "关闭")}");
        }
    }

    public override void _Process(double delta)
    {
        UpdateTargetSelection();

        if (!_isVisible)
        {
            return;
        }

        double fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        double processTime = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double staticMem = Performance.GetMonitor(Performance.Monitor.MemoryStatic) / (1024.0 * 1024.0);

        Vector3 pPos = _player?.GlobalPosition ?? Vector3.Zero;
        ChunkCoord pChunk = ChunkCoord.FromWorldPosition(pPos);

        string targetBlockInfo = "无目标";
        if (GetAimVoxel(out int vx, out int vy, out int vz, out bool isBedrock, out VoxelType blockType))
        {
            float terrainH = _world?.TraditionalTerrain?.HeightSampler?.GetHeight(vx + 0.5f, vz + 0.5f) ?? 0.0f;
            targetBlockInfo = $"({vx}, {vy}, {vz}) [{blockType}]\n  底模高度: {terrainH:F2}m (基岩阻隔: {(isBedrock ? "是" : "否")})";
        }

        _debugLabel.Text =
            $"=== Celestial Debug Monitor [F3] ===\n" +
            $"FPS: {fps:F0} | 帧耗时: {processTime:F2} ms\n" +
            $"静态内存: {staticMem:F2} MB\n" +
            $"------------------------------------\n" +
            $"角色坐标: ({pPos.X:F2}, {pPos.Y:F2}, {pPos.Z:F2})\n" +
            $"角色Chunk: {pChunk}\n" +
            $"地面状态: {(_player?.IsOnFloor() == true ? "着地 (OnFloor)" : "空中 (Air)")}\n" +
            $"------------------------------------\n" +
            $"准心体素: {targetBlockInfo}";
    }

    private bool GetAimVoxel(out int vx, out int vy, out int vz, out bool isBedrock, out VoxelType blockType)
    {
        vx = vy = vz = 0;
        isBedrock = false;
        blockType = VoxelType.Air;

        if (_player?.Camera == null || _world == null)
        {
            return false;
        }

        PhysicsDirectSpaceState3D spaceState = _player.GetWorld3D().DirectSpaceState;
        Vector3 from = _player.Camera.GlobalPosition;
        Vector3 to = from - _player.Camera.GlobalTransform.Basis.Z * 6.0f;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to, 1);
        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 hitPos = (Vector3)result["position"];
            Vector3 hitNormal = (Vector3)result["normal"];
            Vector3 target = hitPos - hitNormal * 0.4f;

            vx = Mathf.FloorToInt(target.X);
            vy = Mathf.FloorToInt(target.Y);
            vz = Mathf.FloorToInt(target.Z);

            blockType = _world.GetVoxel(vx, vy, vz);
            isBedrock = !_world.CanDig(vx, vy, vz);
            return true;
        }

        return false;
    }

    private void UpdateTargetSelection()
    {
        if (GetAimVoxel(out int vx, out int vy, out int vz, out bool isBedrock, out VoxelType blockType) && blockType.IsSolid())
        {
            _selectionBox.Visible = true;
            _selectionBox.Position = new Vector3(vx, vy, vz);

            // 若触底不可破坏则显示红线框，否则显示天蓝色线框
            _selectionMaterial.AlbedoColor = isBedrock
                ? new Color(1.0f, 0.25f, 0.25f, 0.9f)
                : new Color(0.25f, 0.85f, 1.0f, 0.9f);

            RebuildWireframeMesh();
        }
        else
        {
            _selectionBox.Visible = false;
        }
    }

    private void RebuildWireframeMesh()
    {
        _wireframeMesh.ClearSurfaces();
        _wireframeMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _selectionMaterial);

        // 稍微外扩 0.005m，避免 Z-fighting
        const float min = -0.005f;
        const float max = 1.005f;

        // 12 条立方体边线
        Vector3[] corners =
        [
            new(min, min, min), new(max, min, min),
            new(max, min, max), new(min, min, max),
            new(min, max, min), new(max, max, min),
            new(max, max, max), new(min, max, max)
        ];

        int[][] edges =
        [
            // 底面四边
            [0, 1], [1, 2], [2, 3], [3, 0],
            // 顶面四边
            [4, 5], [5, 6], [6, 7], [7, 4],
            // 垂直四边
            [0, 4], [1, 5], [2, 6], [3, 7]
        ];

        foreach (int[] edge in edges)
        {
            _wireframeMesh.SurfaceAddVertex(corners[edge[0]]);
            _wireframeMesh.SurfaceAddVertex(corners[edge[1]]);
        }

        _wireframeMesh.SurfaceEnd();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_selectionBox))
        {
            _selectionBox.QueueFree();
        }
    }
}

