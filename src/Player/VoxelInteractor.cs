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

    private readonly BedrockPhysicsDetector _detector = new();

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

    private bool CastInteractionRay(out InteractionRaycastResult hitResult)
    {
        hitResult = InteractionRaycastResult.Empty;
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
            hitResult = _detector.AnalyzeHit(result, World?.CurrentSampler);
            return hitResult.Type != HitType.None;
        }

        return false;
    }

    /// <summary>
    /// 挖掘操作：沿射线命中位置向内采样方块，并严格判定底层传统地形阻隔
    /// </summary>
    public void PerformDig()
    {
        if (World == null || !CastInteractionRay(out var hit))
        {
            return;
        }

        // 检查触底阻隔（基岩 / 传统 3D 实体）
        if (hit.Type == HitType.Bedrock)
        {
            GD.Print($"[基岩阻隔] 命中 3D 静态实体或触底大地形 ({hit.TargetVoxelCoord})，属于不可破坏基岩！");
            return;
        }

        if (!World.CanDig(hit.TargetVoxelCoord.X, hit.TargetVoxelCoord.Y, hit.TargetVoxelCoord.Z))
        {
            GD.Print($"[挖掘阻隔] 坐标 {hit.TargetVoxelCoord} 不可挖掘！");
            return;
        }

        World.SetVoxel(hit.TargetVoxelCoord.X, hit.TargetVoxelCoord.Y, hit.TargetVoxelCoord.Z, VoxelType.Air);
        GD.Print($"[挖掘成功] 移除方块 {hit.TargetVoxelCoord}");
    }

    /// <summary>
    /// 放置操作：沿射线命中法线向外放置，并避免与角色自身胶囊体产生重叠卡人
    /// 支持在传统 3D 实体或地形表面法线外侧吸附搭建
    /// </summary>
    public void PerformPlace()
    {
        if (World == null || Player == null || !CastInteractionRay(out var hit))
        {
            return;
        }

        Vector3I placeCoord = hit.PlaceVoxelCoord;

        // 检查是否与玩家当前位置/胶囊体发生碰撞重叠（防止卡人）
        Aabb blockAabb = new(new Vector3(placeCoord.X, placeCoord.Y, placeCoord.Z), Vector3.One);
        Aabb playerAabb = new(
            Player.GlobalPosition - new Vector3(0.4f, 0.0f, 0.4f),
            new Vector3(0.8f, 1.8f, 0.8f)
        );

        if (blockAabb.Intersects(playerAabb))
        {
            GD.Print("[放置受阻] 无法在角色所处空间内部放置方块，防止卡模！");
            return;
        }

        World.SetVoxel(placeCoord.X, placeCoord.Y, placeCoord.Z, SelectedBlockType);
        GD.Print($"[放置成功] 在 {placeCoord} (命中类型: {hit.Type}) 附着放置 {SelectedBlockType}");
    }
}
