using Celestial.Player;
using Godot;

namespace Celestial.Core;

/// <summary>
/// 屏幕 HUD 准心与操作提示覆盖层
/// </summary>
public partial class HUDOverlay : CanvasLayer
{
    private Label _infoLabel = null!;
    private VoxelInteractor? _interactor;

    public override void _Ready()
    {
        // 准心 (+)
        Label crosshair = new()
        {
            Text = "+",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -20,
            OffsetRight = 20,
            OffsetTop = -20,
            OffsetBottom = 20
        };
        crosshair.AddThemeFontSizeOverride("font_size", 24);
        AddChild(crosshair);

        // 操作指南面板
        _infoLabel = new Label
        {
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            OffsetLeft = 20,
            OffsetTop = 20,
            Text = "Celestial: 传统3D地形 + 局部体素混合世界 (MVP)\n" +
                   "------------------------------------------\n" +
                   "[WASD] 移动 | [空格] 跳跃 | [鼠标] 视角 (ESC释放)\n" +
                   "[鼠标左键] 挖掘方块 (触底传统地形阻隔) | [鼠标右键] 放置\n" +
                   "[1-5] 切换方块 | [F3] 调试监控面板 | [F5] 快速存档 | [F9] 快速读档"
        };
        _infoLabel.AddThemeFontSizeOverride("font_size", 15);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        AddChild(_infoLabel);

        _interactor = GetTree().Root.FindChild("VoxelInteractor", true, false) as VoxelInteractor;
    }

    public override void _Process(double delta)
    {
        if (_interactor != null)
        {
            _infoLabel.Text = "Celestial: 传统3D地形 + 局部体素混合世界 (MVP)\n" +
                              "------------------------------------------\n" +
                              "[WASD] 移动 | [空格] 跳跃 | [鼠标] 视角 (ESC释放)\n" +
                              $"[鼠标左键] 挖掘 | [鼠标右键] 放置 (当前: {_interactor.SelectedBlockType})\n" +
                              "[1-5] 切换方块 | [F3] 调试监控面板 | [F5] 快速存档 | [F9] 快速读档";
        }
    }
}

