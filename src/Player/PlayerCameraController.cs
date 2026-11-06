using Godot;

namespace Celestial.Player;

public enum CameraPerspectiveMode
{
    FirstPerson,
    ThirdPerson
}

/// <summary>
/// 第一人称/第三人称过肩平滑双视角与防穿墙弹簧臂控制器
/// （实现第一人称视锥与第三人称全身模型彻底解耦，消除无头断口与道具重叠）
/// </summary>
public partial class PlayerCameraController : Node3D
{
    [Export] public float ThirdPersonArmLength = 2.8f;
    [Export] public float FirstPersonArmLength = 0.0f;
    [Export] public float TransitionSpeed = 14.0f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public Vector3 ShoulderOffset = new(0.35f, 0.15f, 0.0f);

    public Camera3D Camera { get; private set; } = null!;
    public SpringArm3D SpringArm { get; private set; } = null!;
    public CameraPerspectiveMode CurrentMode { get; private set; } = CameraPerspectiveMode.FirstPerson;

    private float _targetArmLength;
    private float _cameraPitch = 0.0f;
    private Node3D? _characterVisual;
    private ToolAttachmentManager? _toolManager;
    private Node3D? _firstPersonHand;

    public override void _Ready()
    {
        SpringArm = GetNodeOrNull<SpringArm3D>("SpringArm3D") ?? new SpringArm3D { Name = "SpringArm3D" };
        if (SpringArm.GetParent() == null)
        {
            AddChild(SpringArm);
        }

        // 配置弹簧臂防穿墙碰撞检测（检测静态地形与体素方块 Layer 1）
        SpringArm.CollisionMask = 1;
        SpringArm.Margin = 0.15f;
        SpringArm.Shape = new SphereShape3D { Radius = 0.15f };

        Camera = SpringArm.GetNodeOrNull<Camera3D>("MainCamera") ?? new Camera3D { Name = "MainCamera" };
        if (Camera.GetParent() == null)
        {
            SpringArm.AddChild(Camera);
        }

        _firstPersonHand = Camera.GetNodeOrNull<Node3D>("FirstPersonHand");

        Camera.Position = ShoulderOffset;
        _targetArmLength = FirstPersonArmLength;
        SpringArm.SpringLength = _targetArmLength;

        _characterVisual = GetNodeOrNull<Node3D>("../CharacterVisual") ?? GetParent()?.GetNodeOrNull<Node3D>("CharacterVisual");
        _toolManager = GetNodeOrNull<ToolAttachmentManager>("../ToolAttachmentManager") ?? GetParent()?.GetNodeOrNull<ToolAttachmentManager>("ToolAttachmentManager");

        UpdatePerspectiveVisuals();
    }

    public void HandleMouseMotion(Vector2 relative, CharacterBody3D playerBody)
    {
        // 水平旋转玩家本体
        playerBody.RotateY(-relative.X * MouseSensitivity);

        // 垂直俯仰旋转相机 Pivot
        _cameraPitch = Mathf.Clamp(_cameraPitch - relative.Y * MouseSensitivity, -Mathf.Pi * 0.45f, Mathf.Pi * 0.45f);
        Rotation = new Vector3(_cameraPitch, 0.0f, 0.0f);
    }

    public void TogglePerspective()
    {
        SetPerspectiveMode(CurrentMode == CameraPerspectiveMode.FirstPerson
            ? CameraPerspectiveMode.ThirdPerson
            : CameraPerspectiveMode.FirstPerson);
    }

    public void SetPerspectiveMode(CameraPerspectiveMode mode)
    {
        CurrentMode = mode;
        _targetArmLength = mode == CameraPerspectiveMode.ThirdPerson ? ThirdPersonArmLength : FirstPersonArmLength;
        UpdatePerspectiveVisuals();
    }

    public void UpdatePerspectiveVisuals()
    {
        if (_characterVisual == null)
        {
            _characterVisual = GetNodeOrNull<Node3D>("../CharacterVisual") ?? GetParent()?.GetNodeOrNull<Node3D>("CharacterVisual");
        }
        if (_toolManager == null)
        {
            _toolManager = GetNodeOrNull<ToolAttachmentManager>("../ToolAttachmentManager") ?? GetParent()?.GetNodeOrNull<ToolAttachmentManager>("ToolAttachmentManager");
        }
        if (_firstPersonHand == null && Camera != null)
        {
            _firstPersonHand = Camera.GetNodeOrNull<Node3D>("FirstPersonHand");
        }

        bool isThirdPerson = CurrentMode == CameraPerspectiveMode.ThirdPerson;

        // 1. 第三人称全身模型显示与阴影投影模式：
        // 第一人称下将全身模型设为 ShadowsOnly（仅在地面投射完整阴影，摄像机俯视低头绝不渲染空心颈部断口与身体穿模）
        // 第三人称下将全身模型设为 On（完整渲染第三人称外观）
        ApplyShadowModeRecursive(_characterVisual, isThirdPerson ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.ShadowsOnly);

        // 2. 第一人称专属手部与工具可见性
        if (_firstPersonHand != null)
        {
            _firstPersonHand.Visible = !isThirdPerson;
        }

        // 3. 第三人称骨骼挂接道具可见性（第一人称隐藏，彻底杜绝两个方块/工具同时出现在屏幕中）
        if (_toolManager?.RightHandAttachment != null)
        {
            _toolManager.RightHandAttachment.Visible = isThirdPerson;
        }

        // 4. 相机过肩偏移平滑过渡
        if (Camera != null)
        {
            Camera.Position = isThirdPerson ? ShoulderOffset : Vector3.Zero;
        }
    }

    public override void _Process(double delta)
    {
        if (SpringArm == null) return;
        SpringArm.SpringLength = Mathf.Lerp(SpringArm.SpringLength, _targetArmLength, (float)delta * TransitionSpeed);

        // 动态确保视角过渡临界点（0.4m）的手臂与模型阴影渲染模式同步
        bool isFps = SpringArm.SpringLength < 0.4f;

        if (_firstPersonHand != null && _firstPersonHand.Visible != isFps)
        {
            _firstPersonHand.Visible = isFps;
        }

        if (_toolManager?.RightHandAttachment != null && _toolManager.RightHandAttachment.Visible == isFps)
        {
            _toolManager.RightHandAttachment.Visible = !isFps;
        }

        if (_characterVisual != null)
        {
            GeometryInstance3D.ShadowCastingSetting targetSetting = isFps
                ? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly
                : GeometryInstance3D.ShadowCastingSetting.On;
            ApplyShadowModeRecursive(_characterVisual, targetSetting);
        }
    }

    private static void ApplyShadowModeRecursive(Node? node, GeometryInstance3D.ShadowCastingSetting setting)
    {
        if (node == null) return;
        if (node is GeometryInstance3D geom)
        {
            if (geom.CastShadow != setting)
            {
                geom.CastShadow = setting;
            }
        }
        foreach (Node child in node.GetChildren())
        {
            ApplyShadowModeRecursive(child, setting);
        }
    }
}
