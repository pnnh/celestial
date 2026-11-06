using Celestial.Voxel;
using Godot;

namespace Celestial.Player;

/// <summary>
/// 角色控制器（整合双视角切换、机动物理、动画状态机与手持工具挂载）
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    [Export] public float WalkSpeed = 3.8f;
    [Export] public float RunSpeed = 7.2f;
    [Export] public float CrouchSpeed = 2.2f;
    [Export] public float JumpVelocity = 5.5f;
    [Export] public float Gravity = 18.0f;

    public VoxelType SelectedVoxelType { get; private set; } = VoxelType.Dirt;
    public Camera3D Camera => CameraController.Camera;
    public PlayerCameraController CameraController { get; private set; } = null!;
    public CharacterAnimationController AnimController { get; private set; } = null!;
    public ToolAttachmentManager ToolManager { get; private set; } = null!;
    public Node3D CharacterVisual { get; private set; } = null!;

    private bool _isCrouching = false;
    private bool _isSprinting = false;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        // 1. 初始化 CharacterVisual 与 3D 角色网格
        CharacterVisual = GetNodeOrNull<Node3D>("CharacterVisual") ?? new Node3D { Name = "CharacterVisual" };
        if (CharacterVisual.GetParent() == null)
        {
            AddChild(CharacterVisual);
        }

        InstantiateCharacterMesh();

        // 2. 初始化相机控制器
        CameraController = GetNodeOrNull<PlayerCameraController>("CameraPivot") ?? new PlayerCameraController { Name = "CameraPivot" };
        if (CameraController.GetParent() == null)
        {
            CameraController.Position = new Vector3(0, 1.55f, 0);
            AddChild(CameraController);
        }

        // 3. 初始化动画控制器
        AnimController = GetNodeOrNull<CharacterAnimationController>("CharacterAnimationController")
            ?? new CharacterAnimationController { Name = "CharacterAnimationController" };
        if (AnimController.GetParent() == null)
        {
            AddChild(AnimController);
        }

        // 4. 初始化装备工具挂载器
        ToolManager = GetNodeOrNull<ToolAttachmentManager>("ToolAttachmentManager")
            ?? new ToolAttachmentManager { Name = "ToolAttachmentManager" };
        if (ToolManager.GetParent() == null)
        {
            AddChild(ToolManager);
        }

        // 默认手持水晶镐或泥土
        ToolManager.UpdateEquippedItem(SelectedVoxelType);
    }

    private void InstantiateCharacterMesh()
    {
        Node3D? hero3D = CharacterVisual.GetNodeOrNull<Node3D>("celestial_hero");
        if (hero3D == null && CharacterVisual.GetChildCount() == 0)
        {
            string heroPath = "res://assets/models/character/celestial_hero.glb";
            if (ResourceLoader.Exists(heroPath))
            {
                PackedScene heroScene = ResourceLoader.Load<PackedScene>(heroPath);
                Node heroNode = heroScene.Instantiate();
                heroNode.Name = "celestial_hero";
                CharacterVisual.AddChild(heroNode);
                hero3D = heroNode as Node3D;
            }
        }

        if (hero3D != null)
        {
            // 角色模型在 1.0m 原始包围盒基础上缩放至 1.85m 成年冒险者比例，且脚掌精准对齐地面 Y=0（微调至 0.865m 消除胶囊体球形底部浮空缝隙）
            hero3D.Scale = new Vector3(1.85f, 1.85f, 1.85f);
            hero3D.Position = new Vector3(0, 0.865f, 0);
            hero3D.RotationDegrees = new Vector3(0, 180, 0); // 朝向 Z 轴前向
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            CameraController.HandleMouseMotion(motion.Relative, this);
        }

        // V 键切换第一人称 / 第三人称
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.V)
            {
                CameraController.TogglePerspective();
            }

            // 数字键 1-5 切换手持方块/工具
            if (key.Keycode is >= Key.Key1 and <= Key.Key5)
            {
                SelectedVoxelType = (key.Keycode - Key.Key1 + 1) switch
                {
                    1 => VoxelType.Dirt,
                    2 => VoxelType.Grass,
                    3 => VoxelType.Stone,
                    4 => VoxelType.Wood,
                    5 => VoxelType.Bedrock,
                    _ => VoxelType.Dirt
                };

                ToolManager.UpdateEquippedItem(SelectedVoxelType);
            }
        }

        // ESC 切换鼠标捕获
        if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey escKey && escKey.Pressed && escKey.Keycode == Key.Escape))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // 读取键盘移动方向
        Vector2 inputDir = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
        if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
        if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
        inputDir = inputDir.Normalized();

        // 状态检查（仅在产生移动输入时方可进入跑步状态，静止按 Shift 维持待机）
        _isCrouching = Input.IsKeyPressed(Key.Ctrl);
        _isSprinting = inputDir != Vector2.Zero && Input.IsKeyPressed(Key.Shift) && !_isCrouching;

        float currentSpeed = _isCrouching ? CrouchSpeed : (_isSprinting ? RunSpeed : WalkSpeed);

        // 应用重力与跳跃
        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }
        else if (Input.IsKeyPressed(Key.Space) && !_isCrouching)
        {
            velocity.Y = JumpVelocity;
        }

        // 平滑加减速机动物理计算（避免瞬发脉冲冲击穿透体素网格）
        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        Vector3 targetVelocity = moveDir * currentSpeed;
        float accel = moveDir != Vector3.Zero ? 14.0f : 22.0f;
        velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, accel * (float)delta);
        velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, accel * (float)delta);

        Velocity = velocity;
        MoveAndSlide();

        // 驱动动画机
        float horizontalSpeed = new Vector2(Velocity.X, Velocity.Z).Length();
        AnimController?.UpdateLocomotion(horizontalSpeed, _isCrouching, IsOnFloor());
    }
}
