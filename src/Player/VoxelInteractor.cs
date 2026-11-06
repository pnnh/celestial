using Celestial.Voxel;
using Godot;

namespace Celestial.Player;

/// <summary>
/// 玩家视线体素交互器（射线拾取、方块挖掘触底阻隔、放置防卡）
/// </summary>
public partial class VoxelInteractor : Node3D
{
    [Export] public float ReachDistance = 6.0f;
    [Export] public VoxelWorld? World;
    [Export] public PlayerController? Player;

    public VoxelType SelectedBlockType { get; private set; } = VoxelType.Dirt;

    public override void _Ready()
    {
        if (World == null)
        {
            World = GetTree().Root.FindChild("VoxelWorld", true, false) as VoxelWorld;
        }

        if (Player == null)
        {
            Player = GetOwner<PlayerController>() ?? GetParent()?.GetParent<PlayerController>() ?? GetNodeOrNull<PlayerController>("../../");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            return;
        }

        // 数字键切换放置方块种类
        if (@event is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Key1: SelectedBlockType = VoxelType.Dirt; GD.Print("选中方块: 泥土 (Dirt)"); break;
                case Key.Key2: SelectedBlockType = VoxelType.Stone; GD.Print("选中方块: 石头 (Stone)"); break;
                case Key.Key3: SelectedBlockType = VoxelType.Wood; GD.Print("选中方块: 原木 (Wood)"); break;
                case Key.Key4: SelectedBlockType = VoxelType.Planks; GD.Print("选中方块: 木板 (Planks)"); break;
                case Key.Key5: SelectedBlockType = VoxelType.Sand; GD.Print("选中方块: 沙子 (Sand)"); break;
            }
        }

        // 鼠标左键：挖掘
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left)
            {
                Player?.ToolManager?.PlaySwing();
                PerformDig();
            }
            else if (mouseBtn.ButtonIndex == MouseButton.Right)
            {
                Player?.ToolManager?.PlaySwing();
                PerformPlace();
            }
        }
    }

    private bool CastInteractionRay(out Vector3 hitPos, out Vector3 hitNormal)
    {
        hitPos = Vector3.Zero;
        hitNormal = Vector3.Zero;

        if (Player?.Camera == null)
        {
            return false;
        }

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        Vector3 from = Player.Camera.GlobalPosition;
        Vector3 to = from - Player.Camera.GlobalTransform.Basis.Z * ReachDistance;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to, 1); // Collision Layer 1 (Environment)
        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            hitPos = (Vector3)result["position"];
            hitNormal = (Vector3)result["normal"];
            return true;
        }

        return false;
    }

    /// <summary>
    /// 挖掘操作：沿射线命中位置向内采样方块，并严格判定底层传统地形阻隔
    /// </summary>
    public void PerformDig()
    {
        if (World == null || !CastInteractionRay(out Vector3 hitPos, out Vector3 hitNormal))
        {
            return;
        }

        // 挖掘目标坐标：沿法线向内步进 0.5 格
        Vector3 targetPoint = hitPos - hitNormal * 0.4f;
        int vx = Mathf.FloorToInt(targetPoint.X);
        int vy = Mathf.FloorToInt(targetPoint.Y);
        int vz = Mathf.FloorToInt(targetPoint.Z);

        // 检查触底阻隔（基岩 / 传统 3D 地形）
        if (!World.CanDig(vx, vy, vz))
        {
            GD.Print($"[挖掘阻隔] 坐标 ({vx}, {vy}, {vz}) 触底传统地形，属于不可破坏基岩！");
            return;
        }

        World.SetVoxel(vx, vy, vz, VoxelType.Air);
        GD.Print($"[挖掘成功] 移除方块 ({vx}, {vy}, {vz})");
    }

    /// <summary>
    /// 放置操作：沿射线命中法线向外放置，并避免与角色自身胶囊体产生重叠卡人
    /// </summary>
    public void PerformPlace()
    {
        if (World == null || Player == null || !CastInteractionRay(out Vector3 hitPos, out Vector3 hitNormal))
        {
            return;
        }

        // 放置目标坐标：沿法线向外步进 0.5 格
        Vector3 targetPoint = hitPos + hitNormal * 0.4f;
        int vx = Mathf.FloorToInt(targetPoint.X);
        int vy = Mathf.FloorToInt(targetPoint.Y);
        int vz = Mathf.FloorToInt(targetPoint.Z);

        // 检查是否与玩家当前位置/胶囊体发生碰撞重叠（防止卡人）
        Aabb blockAabb = new(new Vector3(vx, vy, vz), Vector3.One);
        Aabb playerAabb = new(
            Player.GlobalPosition - new Vector3(0.4f, 0.0f, 0.4f),
            new Vector3(0.8f, 1.8f, 0.8f)
        );

        if (blockAabb.Intersects(playerAabb))
        {
            GD.Print("[放置受阻] 无法在角色所处空间内部放置方块，防止卡模！");
            return;
        }

        World.SetVoxel(vx, vy, vz, SelectedBlockType);
        GD.Print($"[放置成功] 在 ({vx}, {vy}, {vz}) 放置 {SelectedBlockType}");
    }
}
