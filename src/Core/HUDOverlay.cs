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
                   "[鼠标左键] 挖掘体素方块 (触底传统地形自动阻隔)\n" +
                   "[鼠标右键] 放置方块 (当前: 泥土)\n" +
                   "[数字键 1-5] 切换方块 (1:泥土 2:石头 3:原木 4:木板 5:沙子)"
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
                              "[鼠标左键] 挖掘体素方块 (触底传统地形自动阻隔)\n" +
                              $"[鼠标右键] 放置方块 (当前选中: {_interactor.SelectedBlockType})\n" +
                              "[数字键 1-5] 切换方块 (1:泥土 2:石头 3:原木 4:木板 5:沙子)";
        }
    }
}

