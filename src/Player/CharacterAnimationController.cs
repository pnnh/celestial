using Godot;

namespace Celestial.Player;

/// <summary>
/// 角色机动骨骼动画控制器（管理 Idle/Walk/Run/Crouch 动画加载与状态平滑过渡）
/// </summary>
public partial class CharacterAnimationController : Node
{
    [Export] public NodePath VisualRootPath { get; set; } = new("../CharacterVisual");

    public AnimationPlayer? AnimPlayer { get; private set; }
    private string _currentAnim = "";

    public override void _Ready()
    {
        InitializeAnimations();
    }

    public void InitializeAnimations()
    {
        Node? visualRoot = GetNodeOrNull(VisualRootPath) ?? GetParent()?.GetNodeOrNull("CharacterVisual");
        if (visualRoot == null) return;

        AnimPlayer = visualRoot.GetNodeOrNull<AnimationPlayer>("celestial_hero/AnimationPlayer")
            ?? visualRoot.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
            ?? FindAnimationPlayerRecursive(visualRoot);

        if (AnimPlayer == null)
        {
            AnimPlayer = new AnimationPlayer { Name = "AnimationPlayer" };
            visualRoot.AddChild(AnimPlayer);
        }

        AnimationLibrary lib = AnimPlayer.HasAnimationLibrary("")
            ? AnimPlayer.GetAnimationLibrary("")
            : new AnimationLibrary();

        // 载入 4 组动画并设置循环播放与骨骼重定向
        LoadAndRegisterAnimation(lib, "idle", "res://assets/models/character/animations/idle.glb", "idle_neutral");
        LoadAndRegisterAnimation(lib, "walk", "res://assets/models/character/animations/walk.glb", "walk_forward");
        LoadAndRegisterAnimation(lib, "run", "res://assets/models/character/animations/run.glb", "run_forward");
        LoadAndRegisterAnimation(lib, "crouch", "res://assets/models/character/animations/crouch.glb", "crouch_walk");

        if (!AnimPlayer.HasAnimationLibrary(""))
        {
            AnimPlayer.AddAnimationLibrary("", lib);
        }

        // 默认播放待机
        PlayAnimation("idle", 0.1f);
    }

    private static void LoadAndRegisterAnimation(AnimationLibrary lib, string targetName, string glbPath, string sourceAnimName)
    {
        if (!ResourceLoader.Exists(glbPath)) return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(glbPath);
        if (scene == null) return;

        Node instance = scene.Instantiate();
        AnimationPlayer? sourcePlayer = instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
            ?? FindAnimationPlayerRecursive(instance);

        if (sourcePlayer != null && sourcePlayer.HasAnimation(sourceAnimName))
        {
            Animation? anim = sourcePlayer.GetAnimation(sourceAnimName);
            if (anim != null)
            {
                Animation clonedAnim = (Animation)anim.Duplicate();
                clonedAnim.LoopMode = Animation.LoopModeEnum.Linear;

                // 重定向轨道至 Skeleton3D:bone_name 并清洗根骨骼水平位移（防止 Root Motion 瞬移穿模）
                for (int t = 0; t < clonedAnim.GetTrackCount(); t++)
                {
                    string pathStr = clonedAnim.TrackGetPath(t).ToString();
                    int lastSlash = pathStr.LastIndexOf('/');
                    string boneName = lastSlash >= 0 ? pathStr[(lastSlash + 1)..] : pathStr;
                    clonedAnim.TrackSetPath(t, new NodePath("Skeleton3D:" + boneName));

                    // 若为 Hips 或 Root 的位移轨道，过滤掉 X/Z 水平位移，仅保留自然垂直颠簸 Y
                    if (clonedAnim.TrackGetType(t) == Animation.TrackType.Position3D &&
                        (boneName.Contains("Hips", System.StringComparison.OrdinalIgnoreCase) ||
                         boneName.Contains("root", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        int keyCount = clonedAnim.TrackGetKeyCount(t);
                        for (int k = 0; k < keyCount; k++)
                        {
                            Vector3 posVal = clonedAnim.TrackGetKeyValue(t, k).AsVector3();
                            clonedAnim.TrackSetKeyValue(t, k, new Vector3(0.0f, posVal.Y, 0.0f));
                        }
                    }

                    // 针对待机动画（idle）：稳定骨盆与双腿双脚姿态，消除钟摆式左右晃动与轮流抬脚悬空
                    if (targetName == "idle" && clonedAnim.TrackGetType(t) == Animation.TrackType.Rotation3D)
                    {
                        if (boneName.Equals("Hips", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // 消除骨盆左右侧倾 Roll 与偏转 Yaw（锁定到水平平衡的标准关键帧 0）
                            Quaternion q0 = clonedAnim.TrackGetKeyValue(t, 0).AsQuaternion();
                            int keyCount = clonedAnim.TrackGetKeyCount(t);
                            for (int k = 0; k < keyCount; k++)
                            {
                                clonedAnim.TrackSetKeyValue(t, k, q0);
                            }
                        }
                        else if (boneName.Contains("Leg", System.StringComparison.OrdinalIgnoreCase) ||
                                 boneName.Contains("Foot", System.StringComparison.OrdinalIgnoreCase) ||
                                 boneName.Contains("Toes", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // 双腿双脚锁定在稳固着地姿态，避免左右轮流脚跟离地，使上身自然呼吸而下身扎实扎根
                            Quaternion q0 = clonedAnim.TrackGetKeyValue(t, 0).AsQuaternion();
                            int keyCount = clonedAnim.TrackGetKeyCount(t);
                            for (int k = 0; k < keyCount; k++)
                            {
                                clonedAnim.TrackSetKeyValue(t, k, q0);
                            }
                        }
                    }
                }

                if (lib.HasAnimation(targetName))
                {
                    lib.RemoveAnimation(targetName);
                }
                lib.AddAnimation(targetName, clonedAnim);
            }
        }
        instance.QueueFree();
    }

    public void UpdateLocomotion(float horizontalSpeed, bool isCrouching, bool isOnFloor)
    {
        if (AnimPlayer == null) return;

        string desiredAnim;
        float playbackSpeed = 1.0f;

        if (!isOnFloor)
        {
            // 空中保持当前动作或轻微滞空
            return;
        }

        if (isCrouching)
        {
            desiredAnim = "crouch";
            playbackSpeed = horizontalSpeed > 0.2f ? Mathf.Clamp(horizontalSpeed / 2.5f, 0.6f, 1.4f) : 0.0f;
        }
        else if (horizontalSpeed > 4.2f)
        {
            desiredAnim = "run";
            playbackSpeed = Mathf.Clamp(horizontalSpeed / 7.0f, 0.8f, 1.3f);
        }
        else if (horizontalSpeed > 0.3f)
        {
            desiredAnim = "walk";
            playbackSpeed = Mathf.Clamp(horizontalSpeed / 3.5f, 0.7f, 1.3f);
        }
        else
        {
            desiredAnim = "idle";
            playbackSpeed = 1.0f;
        }

        if (_currentAnim != desiredAnim)
        {
            PlayAnimation(desiredAnim, 0.18f);
        }

        AnimPlayer.SpeedScale = playbackSpeed;
    }

    public void PlayAnimation(string animName, float crossFade = 0.15f)
    {
        if (AnimPlayer != null && AnimPlayer.HasAnimation(animName))
        {
            AnimPlayer.Play(animName, customBlend: crossFade);
            _currentAnim = animName;
        }
    }

    private static AnimationPlayer? FindAnimationPlayerRecursive(Node node)
    {
        if (node is AnimationPlayer ap) return ap;
        foreach (Node child in node.GetChildren())
        {
            AnimationPlayer? found = FindAnimationPlayerRecursive(child);
            if (found != null) return found;
        }
        return null;
    }
}
