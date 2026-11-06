using Godot;

namespace Celestial.Player;

/// <summary>
/// 第一人称角色控制器（支持 WASD 移动、跳跃、重力与视角旋转）
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed = 6.0f;
    [Export] public float JumpVelocity = 5.5f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float Gravity = 18.0f;

    public Camera3D Camera { get; private set; } = null!;
    private float _cameraPitch = 0.0f;

    public override void _Ready()
    {
        Camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // 水平旋转角色 Body
            RotateY(-motion.Relative.X * MouseSensitivity);

            // 垂直俯仰旋转相机
            _cameraPitch = Mathf.Clamp(_cameraPitch - motion.Relative.Y * MouseSensitivity, -Mathf.Pi * 0.48f, Mathf.Pi * 0.48f);
            Camera.Rotation = new Vector3(_cameraPitch, 0.0f, 0.0f);
        }

        // 按 ESC 切换鼠标捕获状态
        if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // 应用重力
        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }
        else if (Input.IsKeyPressed(Key.Space))
        {
            velocity.Y = JumpVelocity;
        }

        // 读取键盘移动方向
        Vector2 inputDir = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
        if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
        if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
        inputDir = inputDir.Normalized();

        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (moveDir != Vector3.Zero)
        {
            velocity.X = moveDir.X * MoveSpeed;
            velocity.Z = moveDir.Z * MoveSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}

