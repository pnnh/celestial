using System;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 全局海平面与大洋水体渲染节点
/// 尺寸 2048m x 2048m，锚定于 Y = SeaLevel，水平坐标吸附玩家摄像机平移（营造无垠大洋视感）
/// </summary>
[Tool]
public partial class OceanWaterPlane : MeshInstance3D
{
    [Export] public float SeaLevel { get; set; } = 0.0f;
    [Export] public float PlaneSize { get; set; } = 2048.0f;
    [Export] public int Subdivisions { get; set; } = 64;
    [Export] public float SnapGridSize { get; set; } = 16.0f;

    private Node3D? _observer;

    public override void _Ready()
    {
        // 1. 构建水平 PlaneMesh 网格
        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(PlaneSize, PlaneSize),
            SubdivideWidth = Subdivisions,
            SubdivideDepth = Subdivisions
        };
        Mesh = planeMesh;

        // 2. 加载并绑定赛璐璐手绘水体着色器
        var shader = GD.Load<Shader>("res://shaders/ocean_water.gdshader");
        if (shader != null)
        {
            var mat = new ShaderMaterial { Shader = shader };
            MaterialOverride = mat;
        }

        // 3. 初始高度固定在海平面
        GlobalPosition = new Vector3(0.0f, SeaLevel, 0.0f);
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
    }

    public void SetObserver(Node3D observer)
    {
        _observer = observer;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            GlobalPosition = new Vector3(0.0f, SeaLevel, 0.0f);
            return;
        }

        Vector3 targetPos = Vector3.Zero;
        if (_observer != null && IsInstanceValid(_observer))
        {
            targetPos = _observer.GlobalPosition;
        }
        else
        {
            var viewport = GetViewport();
            var camera = viewport?.GetCamera3D();
            if (camera != null)
            {
                targetPos = camera.GlobalPosition;
            }
        }

        // 吸附移动，避免顶点波浪随移动出现高频锯齿
        float snappedX = MathF.Floor(targetPos.X / SnapGridSize) * SnapGridSize;
        float snappedZ = MathF.Floor(targetPos.Z / SnapGridSize) * SnapGridSize;
        GlobalPosition = new Vector3(snappedX, SeaLevel, snappedZ);
    }
}

