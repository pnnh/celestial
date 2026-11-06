using Celestial.Voxel;
using Godot;

namespace Celestial.Player;

/// <summary>
/// 管理角色手部道具挂接（支持第一人称独立手臂与第三人称骨骼挂接）
/// </summary>
public partial class ToolAttachmentManager : Node
{
    [Export] public NodePath SkeletonPath { get; set; } = new("../CharacterVisual/celestial_hero/Skeleton3D");

    public BoneAttachment3D? RightHandAttachment { get; private set; }
    public Node3D? FirstPersonHand { get; private set; }

    private Node3D? _thirdPersonProp;
    private Node3D? _firstPersonProp;
    private Node3D? _firstPersonArm;
    private float _swingTimer = 0.0f;
    private Vector3 _firstPersonBasePos = new(0.28f, -0.22f, -0.44f);

    public override void _Ready()
    {
        InitializeAttachments();
    }

    public void InitializeAttachments()
    {
        // 1. 初始化第三人称骨骼挂载点（优先挂接至手掌中心 bone_17，杜绝小臂手腕穿模）
        Skeleton3D? skeleton = GetNodeOrNull<Skeleton3D>(SkeletonPath)
            ?? FindSkeletonRecursive(GetParent() ?? this);

        if (skeleton != null)
        {
            int palmBoneIdx = FindBoneIndex(skeleton, ["bone_17", "RightHand", "Hand.R", "hand.r", "mixamorig:RightHand"]);
            if (palmBoneIdx != -1)
            {
                string boneName = skeleton.GetBoneName(palmBoneIdx);
                RightHandAttachment = skeleton.GetNodeOrNull<BoneAttachment3D>($"BoneAttachment_{boneName}")
                    ?? new BoneAttachment3D { Name = $"BoneAttachment_{boneName}", BoneName = boneName };

                if (RightHandAttachment.GetParent() == null)
                {
                    skeleton.AddChild(RightHandAttachment);
                }
            }
        }

        // 2. 初始化第一人称相机前置手持点与一体化流畅风格手臂
        Camera3D? camera = GetParent()?.GetNodeOrNull<Camera3D>("CameraPivot/SpringArm3D/MainCamera")
            ?? FindCameraRecursive(GetParent() ?? this);

        if (camera != null)
        {
            FirstPersonHand = camera.GetNodeOrNull<Node3D>("FirstPersonHand");
            if (FirstPersonHand == null)
            {
                FirstPersonHand = new Node3D
                {
                    Name = "FirstPersonHand",
                    Position = _firstPersonBasePos,
                    RotationDegrees = new Vector3(8, -14, 0)
                };
                camera.AddChild(FirstPersonHand);
            }

            // 构建/重构第一人称专属连续统一手臂模型（一体化圆润小臂 + 深色腕带 + 自然手掌）
            Node? oldArm = FirstPersonHand.GetNodeOrNull("FirstPersonArm");
            if (oldArm != null)
            {
                oldArm.QueueFree();
                FirstPersonHand.RemoveChild(oldArm);
            }

            _firstPersonArm = BuildFirstPersonArm();
            FirstPersonHand.AddChild(_firstPersonArm);
        }
    }

    public void UpdateEquippedItem(VoxelType selectedType)
    {
        if (RightHandAttachment == null || FirstPersonHand == null)
        {
            InitializeAttachments();
        }

        // 清理旧道具
        if (_thirdPersonProp != null && IsInstanceValid(_thirdPersonProp))
        {
            _thirdPersonProp.QueueFree();
            _thirdPersonProp = null;
        }

        if (_firstPersonProp != null && IsInstanceValid(_firstPersonProp))
        {
            _firstPersonProp.QueueFree();
            _firstPersonProp = null;
        }

        // 生成第三人称道具 (大小适中，握于掌心 bone_17，与髋部和衣角保持安全间距)
        if (RightHandAttachment != null)
        {
            _thirdPersonProp = CreateProp(selectedType, false);
            RightHandAttachment.AddChild(_thirdPersonProp);
        }

        // 生成第一人称手持物 (置于第一人称手臂的手掌前方)
        if (FirstPersonHand != null)
        {
            _firstPersonProp = CreateProp(selectedType, true);
            FirstPersonHand.AddChild(_firstPersonProp);
        }
    }

    public void PlaySwing()
    {
        _swingTimer = 0.22f;
    }

    public override void _Process(double delta)
    {
        // 第一人称手部与工具挥击动画效果
        if (FirstPersonHand != null && _swingTimer > 0.0f)
        {
            _swingTimer -= (float)delta;
            float progress = Mathf.Clamp(1.0f - (_swingTimer / 0.22f), 0.0f, 1.0f);
            float swingOffset = Mathf.Sin(progress * Mathf.Pi);

            FirstPersonHand.Position = _firstPersonBasePos + new Vector3(-0.06f * swingOffset, -0.08f * swingOffset, -0.1f * swingOffset);
            FirstPersonHand.RotationDegrees = new Vector3(10 - 35 * swingOffset, -15 + 20 * swingOffset, -25 * swingOffset);
        }
        else if (FirstPersonHand != null)
        {
            FirstPersonHand.Position = _firstPersonBasePos;
            FirstPersonHand.RotationDegrees = new Vector3(10, -15, 0);
        }
    }

    private static Node3D BuildFirstPersonArm()
    {
        Node3D armRoot = new() { Name = "FirstPersonArm" };

        // 统一手臂骨骼架（共享轴向，杜绝高低不一的截断阶梯感）
        Node3D rig = new()
        {
            Name = "ArmRig",
            Position = new Vector3(0.02f, -0.06f, 0.14f),
            RotationDegrees = new Vector3(-66f, 20f, -12f)
        };
        armRoot.AddChild(rig);

        // 1. 肘部白色袍服袖口（仅在右上/下边缘处进入视野）
        MeshInstance3D sleeve = new()
        {
            Name = "Sleeve",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.048f,
                BottomRadius = 0.053f,
                Height = 0.11f,
                RadialSegments = 16
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.95f, 0.95f, 0.98f),
                Roughness = 0.7f
            },
            Position = new Vector3(0.0f, -0.11f, 0.0f)
        };
        rig.AddChild(sleeve);

        // 2. 连续完整裸臂小臂（日系二次元动漫肤色，微锥形流畅圆润几何体）
        StandardMaterial3D skinMat = new()
        {
            AlbedoColor = new Color(0.96f, 0.82f, 0.72f),
            Roughness = 0.45f
        };

        MeshInstance3D forearm = new()
        {
            Name = "Forearm",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.038f,
                BottomRadius = 0.045f,
                Height = 0.22f,
                RadialSegments = 16
            },
            MaterialOverride = skinMat,
            Position = new Vector3(0.0f, 0.04f, 0.0f)
        };
        rig.AddChild(forearm);

        // 3. 纤细深黑皮革腕带（避免任何与泥土方块相似的浅褐色矩形块感）
        MeshInstance3D wrist = new()
        {
            Name = "WristBand",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.040f,
                BottomRadius = 0.041f,
                Height = 0.028f,
                RadialSegments = 16
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.18f, 0.14f, 0.12f),
                Roughness = 0.6f
            },
            Position = new Vector3(0.0f, 0.155f, 0.0f)
        };
        rig.AddChild(wrist);

        // 4. 自然手掌（托举平面）
        MeshInstance3D hand = new()
        {
            Name = "Hand",
            Mesh = new BoxMesh { Size = new Vector3(0.066f, 0.048f, 0.072f) },
            MaterialOverride = skinMat,
            Position = new Vector3(0.0f, 0.198f, 0.0f)
        };
        rig.AddChild(hand);

        // 5. 拇指侧弯支撑
        MeshInstance3D thumb = new()
        {
            Name = "Thumb",
            Mesh = new BoxMesh { Size = new Vector3(0.022f, 0.030f, 0.048f) },
            MaterialOverride = skinMat,
            Position = new Vector3(-0.030f, 0.202f, -0.012f),
            RotationDegrees = new Vector3(0f, -20f, 15f)
        };
        rig.AddChild(thumb);

        return armRoot;
    }

    private static Node3D CreateProp(VoxelType type, bool isFirstPerson)
    {
        Node3D root = new() { Name = isFirstPerson ? "FP_Prop" : "TP_Prop" };

        if (type == VoxelType.Air)
        {
            // 水晶双刃镐
            float scale = isFirstPerson ? 0.78f : 0.62f;
            root.AddChild(BuildPickaxeMesh(scale));

            if (!isFirstPerson)
            {
                // 第三人称握持：镐柄穿过 palm (bone_17)，镐尖朝前下
                root.RotationDegrees = new Vector3(-35, 15, -75);
                root.Position = new Vector3(-0.01f, -0.015f, 0.02f);
            }
            else
            {
                root.RotationDegrees = new Vector3(0, 75, -20);
                root.Position = new Vector3(0.01f, 0.035f, -0.05f);
            }
        }
        else
        {
            // 紧凑迷你方块 (掌心微缩体素模型，唯一定位在第一人称手掌中心)
            float blockSize = isFirstPerson ? 0.115f : 0.11f;
            BoxMesh mesh = new() { Size = new Vector3(blockSize, blockSize, blockSize) };
            StandardMaterial3D mat = new()
            {
                AlbedoColor = type.GetDebugColor(),
                Roughness = 0.65f
            };
            MeshInstance3D block = new()
            {
                Mesh = mesh,
                MaterialOverride = mat
            };
            root.AddChild(block);

            if (!isFirstPerson)
            {
                // 第三人称掌心托举位置 (置于手掌心 bone_17 正前方，绝不与小臂或腰部相切穿模)
                root.Position = new Vector3(-0.015f, -0.015f, 0.035f);
                root.RotationDegrees = new Vector3(15, -10, 15);
            }
            else
            {
                root.Position = new Vector3(0.005f, 0.04f, -0.065f);
                root.RotationDegrees = new Vector3(12, 26, 8);
            }
        }

        return root;
    }

    private static Node3D BuildPickaxeMesh(float scale)
    {
        Node3D root = new();

        // 镐柄
        CylinderMesh handleMesh = new()
        {
            TopRadius = 0.016f * scale,
            BottomRadius = 0.02f * scale,
            Height = 0.6f * scale
        };
        StandardMaterial3D handleMat = new()
        {
            AlbedoColor = new Color(0.48f, 0.32f, 0.18f),
            Roughness = 0.75f
        };
        MeshInstance3D handle = new()
        {
            Mesh = handleMesh,
            MaterialOverride = handleMat,
            Position = new Vector3(0, 0.15f * scale, 0)
        };
        root.AddChild(handle);

        // 水晶双刃镐头
        BoxMesh headMesh = new()
        {
            Size = new Vector3(0.36f * scale, 0.06f * scale, 0.07f * scale)
        };
        StandardMaterial3D headMat = new()
        {
            AlbedoColor = new Color(0.12f, 0.9f, 0.95f),
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.75f, 0.85f),
            EmissionEnergyMultiplier = 0.9f,
            Roughness = 0.2f
        };
        MeshInstance3D head = new()
        {
            Mesh = headMesh,
            MaterialOverride = headMat,
            Position = new Vector3(0, 0.42f * scale, 0)
        };
        root.AddChild(head);

        return root;
    }

    private static Skeleton3D? FindSkeletonRecursive(Node? node)
    {
        if (node == null) return null;
        if (node is Skeleton3D skel) return skel;
        foreach (Node child in node.GetChildren())
        {
            Skeleton3D? found = FindSkeletonRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    private static Camera3D? FindCameraRecursive(Node? node)
    {
        if (node == null) return null;
        if (node is Camera3D cam) return cam;
        foreach (Node child in node.GetChildren())
        {
            Camera3D? found = FindCameraRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    private static int FindBoneIndex(Skeleton3D skeleton, string[] candidateNames)
    {
        foreach (string name in candidateNames)
        {
            int idx = skeleton.FindBone(name);
            if (idx != -1) return idx;
        }
        return -1;
    }
}
