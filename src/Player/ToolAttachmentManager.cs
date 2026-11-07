using System.Collections.Generic;
using Celestial.Voxel;
using Godot;

namespace Celestial.Player;

/// <summary>
/// 管理角色手部道具挂接（支持第一人称独立写实贴合手臂与第三人称骨骼挂接）
/// </summary>
public partial class ToolAttachmentManager : Node
{
    [Export] public NodePath SkeletonPath { get; set; } = new("../CharacterVisual/celestial_hero/Skeleton3D");

    public BoneAttachment3D? RightHandAttachment { get; private set; }
    public Node3D? FirstPersonHand { get; private set; }

    private Node3D? _thirdPersonProp;
    private Node3D? _firstPersonProp;
    private Node3D? _firstPersonArm;
    private Node3D? _firstPersonMount;
    private float _swingTimer = 0.0f;
    private Vector3 _firstPersonBasePos = new(0.24f, -0.21f, -0.40f);

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

            // 构建第一人称专属流畅手臂模型（一体化圆润小臂 + 护腕 + 自然内扣手掌 + 道具锚点）
            Node? oldArm = FirstPersonHand.GetNodeOrNull("FirstPersonArm");
            if (oldArm != null)
            {
                oldArm.QueueFree();
                FirstPersonHand.RemoveChild(oldArm);
            }

            _firstPersonArm = BuildFirstPersonArm();
            FirstPersonHand.AddChild(_firstPersonArm);
            _firstPersonMount = _firstPersonArm.FindChild("FP_ItemMount", true, false) as Node3D;
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

        // 生成第三人称道具 (大小适中 0.14m，握于掌心 bone_17，与髋部和衣角保持安全间距)
        if (RightHandAttachment != null)
        {
            _thirdPersonProp = CreateProp(selectedType, false);
            RightHandAttachment.AddChild(_thirdPersonProp);
        }

        // 生成第一人称手持物 (精准挂载至手掌道具锚点，手心向内自然托举，绝不穿透小臂)
        Node3D mountTarget = _firstPersonMount ?? FirstPersonHand!;
        if (mountTarget != null)
        {
            _firstPersonProp = CreateProp(selectedType, true);
            mountTarget.AddChild(_firstPersonProp);
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
            FirstPersonHand.RotationDegrees = new Vector3(8 - 35 * swingOffset, -14 + 20 * swingOffset, -25 * swingOffset);
        }
        else if (FirstPersonHand != null)
        {
            FirstPersonHand.Position = _firstPersonBasePos;
            FirstPersonHand.RotationDegrees = new Vector3(8, -14, 0);
        }
    }

    private static Node3D BuildFirstPersonArm()
    {
        Node3D armRoot = new() { Name = "FirstPersonArm" };

        // 统一手臂骨骼架（共享轴向，杜绝高低不一的截断阶梯感）
        Node3D rig = new()
        {
            Name = "ArmRig",
            Position = new Vector3(0.04f, -0.06f, 0.16f),
            RotationDegrees = new Vector3(-62f, 22f, -14f)
        };
        armRoot.AddChild(rig);

        // 1. 肘部白色袍服袖口（仅在右上/下边缘处进入视野）
        MeshInstance3D sleeve = new()
        {
            Name = "Sleeve",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.048f,
                BottomRadius = 0.054f,
                Height = 0.13f,
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
                BottomRadius = 0.046f,
                Height = 0.22f,
                RadialSegments = 16
            },
            MaterialOverride = skinMat,
            Position = new Vector3(0.0f, 0.04f, 0.0f)
        };
        rig.AddChild(forearm);

        // 3. 纤细深黑皮革腕带
        MeshInstance3D wrist = new()
        {
            Name = "WristBand",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.040f,
                BottomRadius = 0.041f,
                Height = 0.032f,
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

        // 4. 手掌关节组（带有手腕内旋角度，掌心朝向身体左前方）
        Node3D handRig = new()
        {
            Name = "HandRig",
            Position = new Vector3(0.0f, 0.172f, 0.0f),
            RotationDegrees = new Vector3(12f, -6f, 34f) // 手腕内扣翻掌，手心朝向屏幕内侧
        };
        rig.AddChild(handRig);

        // 手掌掌心
        MeshInstance3D palm = new()
        {
            Name = "Palm",
            Mesh = new BoxMesh { Size = new Vector3(0.066f, 0.046f, 0.060f) },
            MaterialOverride = skinMat,
            Position = new Vector3(0.0f, 0.024f, 0.0f)
        };
        handRig.AddChild(palm);

        // 自然微弯托底的四指（在方块底部形成写实托衬）
        MeshInstance3D fingers = new()
        {
            Name = "Fingers",
            Mesh = new BoxMesh { Size = new Vector3(0.060f, 0.026f, 0.040f) },
            MaterialOverride = skinMat,
            Position = new Vector3(-0.008f, 0.052f, -0.016f),
            RotationDegrees = new Vector3(-14f, 0f, 8f)
        };
        handRig.AddChild(fingers);

        // 侧后方大拇指支撑
        MeshInstance3D thumb = new()
        {
            Name = "Thumb",
            Mesh = new BoxMesh { Size = new Vector3(0.024f, 0.032f, 0.044f) },
            MaterialOverride = skinMat,
            Position = new Vector3(0.032f, 0.035f, 0.012f),
            RotationDegrees = new Vector3(6f, -22f, -18f)
        };
        handRig.AddChild(thumb);

        // 第一人称道具挂载锚点（精准置于掌心与指尖内前侧，确保方块完美贴合掌面）
        Node3D itemMount = new()
        {
            Name = "FP_ItemMount",
            Position = new Vector3(-0.040f, 0.082f, -0.045f),
            RotationDegrees = new Vector3(14f, 32f, -8f) // 经典等轴测展示角
        };
        handRig.AddChild(itemMount);

        return armRoot;
    }

    private static Node3D CreateProp(VoxelType type, bool isFirstPerson)
    {
        Node3D root = new() { Name = isFirstPerson ? "FP_Prop" : "TP_Prop" };

        if (type == VoxelType.Air)
        {
            // 水晶双刃镐
            float scale = isFirstPerson ? 0.72f : 0.60f;
            root.AddChild(BuildPickaxeMesh(scale));

            if (!isFirstPerson)
            {
                // 第三人称握持：镐柄穿过 palm (bone_17)，镐尖朝前下
                root.RotationDegrees = new Vector3(-35, 15, -75);
                root.Position = new Vector3(-0.01f, -0.015f, 0.02f);
            }
            else
            {
                root.RotationDegrees = new Vector3(-10, 65, -15);
                root.Position = new Vector3(0.01f, -0.02f, 0.01f);
            }
        }
        else
        {
            // 方块手持道具（第一人称 0.20m，贴合掌心且降低与 1m 方块视觉落差；第三人称 0.14m 避免碰撞衣摆）
            float blockSize = isFirstPerson ? 0.20f : 0.14f;
            MeshInstance3D blockMeshInst = CreateVoxelBlockMesh(type, blockSize);
            root.AddChild(blockMeshInst);

            if (!isFirstPerson)
            {
                // 第三人称掌心托举位置 (置于手掌心 bone_17 正前方，绝不与小臂或腰部相切穿模)
                root.Position = new Vector3(-0.015f, -0.015f, 0.038f);
                root.RotationDegrees = new Vector3(15, -10, 15);
            }
            else
            {
                // 第一人称直接挂在 FP_ItemMount 原点，方块中心向前外微偏，使右下底角紧贴掌心
                root.Position = new Vector3(0.0f, 0.0f, 0.0f);
                root.RotationDegrees = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }

        return root;
    }

    /// <summary>
    /// 为手持方块生成带纹理图集 UV 映射的高质感立方体 Mesh
    /// </summary>
    private static MeshInstance3D CreateVoxelBlockMesh(VoxelType type, float size)
    {
        StandardMaterial3D? material = null;
        if (ResourceLoader.Exists("res://assets/textures/voxel_atlas.png"))
        {
            Texture2D atlas = ResourceLoader.Load<Texture2D>("res://assets/textures/voxel_atlas.png");
            material = new StandardMaterial3D
            {
                AlbedoTexture = atlas,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
                VertexColorUseAsAlbedo = true,
                Roughness = 0.85f,
                Metallic = 0.05f
            };
        }

        if (material == null)
        {
            return new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(size, size, size) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = type.GetDebugColor(),
                    Roughness = 0.65f
                }
            };
        }

        float half = size * 0.5f;
        Vector3[] baseVerts =
        [
            new(-half, -half, -half), // 0
            new( half, -half, -half), // 1
            new( half,  half, -half), // 2
            new(-half,  half, -half), // 3
            new(-half, -half,  half), // 4
            new( half, -half,  half), // 5
            new( half,  half,  half), // 6
            new(-half,  half,  half)  // 7
        ];

        int[][] faceIndices =
        [
            [1, 5, 6, 2], // +X
            [4, 0, 3, 7], // -X
            [3, 2, 6, 7], // +Y
            [4, 5, 1, 0], // -Y
            [5, 4, 7, 6], // +Z
            [0, 1, 2, 3]  // -Z
        ];

        Vector3[] normals =
        [
            Vector3.Right,
            Vector3.Left,
            Vector3.Up,
            Vector3.Down,
            Vector3.Back,
            Vector3.Forward
        ];

        List<Vector3> verts = [];
        List<Vector3> norms = [];
        List<Vector2> uvs = [];
        List<Color> colors = [];
        List<int> idxs = [];

        const float totalCols = 3.0f;
        const float totalRows = 2.0f;
        const float eps = 0.002f;

        for (int face = 0; face < 6; face++)
        {
            var (col, row) = GetAtlasTile(type, face);
            float u0 = (col + eps) / totalCols;
            float u1 = (col + 1.0f - eps) / totalCols;
            float v0 = (row + eps) / totalRows;
            float v1 = (row + 1.0f - eps) / totalRows;

            Vector2[] tileUvs =
            [
                new(u0, v1),
                new(u1, v1),
                new(u1, v0),
                new(u0, v0)
            ];

            // 简易方向光照阴影，增强 3D 立体质感
            float shade = face switch
            {
                2 => 1.0f,  // Top
                3 => 0.6f,  // Bottom
                4 or 5 => 0.88f,
                _ => 0.78f
            };
            Color shadeColor = new(shade, shade, shade, 1.0f);

            int start = verts.Count;
            for (int i = 0; i < 4; i++)
            {
                verts.Add(baseVerts[faceIndices[face][i]]);
                norms.Add(normals[face]);
                uvs.Add(tileUvs[i]);
                colors.Add(shadeColor);
            }

            idxs.Add(start + 0); idxs.Add(start + 1); idxs.Add(start + 2);
            idxs.Add(start + 0); idxs.Add(start + 2); idxs.Add(start + 3);
        }

        Godot.Collections.Array arrays = [];
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = norms.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = idxs.ToArray();

        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);

        return new MeshInstance3D { Mesh = mesh };
    }

    private static (int col, int row) GetAtlasTile(VoxelType type, int faceIndex)
    {
        return type switch
        {
            VoxelType.Grass => faceIndex switch
            {
                2 => (0, 0), // Top: 草坪
                3 => (1, 0), // Bottom: 泥土
                _ => (2, 1)  // Sides: 草边
            },
            VoxelType.Dirt => (1, 0),
            VoxelType.Wood => (2, 0),
            VoxelType.Stone => (0, 1),
            VoxelType.Bedrock => (1, 1),
            VoxelType.Planks => (2, 0),
            VoxelType.Sand => (1, 0),
            _ => (0, 0)
        };
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
