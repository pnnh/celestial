using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

namespace Celestial.Terrain;

/// <summary>
/// 开放世界 64m 瓦片流式调度器与视距 LOD 管理中心
/// 负责基于观察者坐标异步调度切片网格生成、主线程限流挂载与对象池回收
/// </summary>
[Tool]
public partial class TiledTerrainManager : Node3D
{
    [Export] public float TileSize { get; set; } = 64.0f;
    [Export] public int ViewDistanceTiles { get; set; } = 5; // 切比雪夫半径 5 (覆盖约 640m 视野)
    [Export] public int MaxMountPerFrame { get; set; } = 2;   // 每帧最多提交瓦片数
    [Export] public float MaxFrameTimeBudgetMs { get; set; } = 3.0f; // 单帧网格提交上限 3.0ms

    public IHeightSampler HeightSampler { get; set; } = null!;
    [Export] public Material? TerrainMaterial { get; set; }

    public int ActiveTileCount => _activeTiles.Count;
    public int PendingJobCount => _buildingTiles.Count + _readyQueue.Count;

    private readonly Dictionary<TerrainTileCoord, TerrainTile> _activeTiles = new();
    private readonly Queue<TerrainTile> _tilePool = new();
    private readonly ConcurrentDictionary<TerrainTileCoord, LODLevel> _buildingTiles = new();
    private readonly ConcurrentQueue<RawTileMeshData> _readyQueue = new();

    private Node3D? _observer;
    private TerrainTileCoord _lastObserverTile = new(int.MinValue, int.MinValue);
    private Vector3 _lastObserverPos = new(float.MaxValue, 0, float.MaxValue);
    private double _scheduleTimer = 0.0;

    public override void _Ready()
    {
        // 1. 若外部未注入采样器，默认创建千米海岛采样器
        HeightSampler ??= new IslandHeightSampler();

        // 2. 加载赛璐璐三平面地表着色材质并绑定手绘贴图
        var shader = GD.Load<Shader>("res://shaders/terrain_triplanar.gdshader");
        if (shader != null)
        {
            var mat = new ShaderMaterial { Shader = shader };

            var grassTex = GD.Load<Texture2D>("res://assets/textures/ludo/lush_grass_soil.png")
                           ?? GD.Load<Texture2D>("res://assets/textures/grass_top.png");
            var rockTex = GD.Load<Texture2D>("res://assets/textures/stone.png");
            var sandTex = GD.Load<Texture2D>("res://assets/textures/sand.png");

            if (grassTex != null) mat.SetShaderParameter("grass_texture", grassTex);
            if (rockTex != null) mat.SetShaderParameter("rock_texture", rockTex);
            if (sandTex != null) mat.SetShaderParameter("sand_texture", sandTex);
            mat.SetShaderParameter("uv_scale", 0.25f);
            mat.SetShaderParameter("rock_uv_scale", 0.20f);
            mat.SetShaderParameter("slope_threshold", 0.65f);

            TerrainMaterial = mat;
        }

        // 3. 若处于 Godot 编辑器内，直接生成 3x3 核心地形与碰撞体供预览与地面吸附
        if (Engine.IsEditorHint())
        {
            BuildEditorPreview();
            return;
        }

        // 4. 运行时：预热初始瓦片池
        for (int i = 0; i < 32; i++)
        {
            var tile = new TerrainTile();
            tile.Recycle();
            AddChild(tile);
            _tilePool.Enqueue(tile);
        }
    }

    private void BuildEditorPreview()
    {
        // 编辑器预览全岛地貌：覆盖半径 460m (以原点为中心的完整海岛陆地与浅滩)
        int maxTileRadius = (int)MathF.Ceiling(460.0f / TileSize);
        for (int dz = -maxTileRadius; dz <= maxTileRadius; dz++)
        {
            for (int dx = -maxTileRadius; dx <= maxTileRadius; dx++)
            {
                float dist = MathF.Sqrt(dx * dx + dz * dz) * TileSize;
                if (dist > 460.0f) continue; // 超出海岸线的外围深海大洋底面在编辑器内无需装载

                var coord = new TerrainTileCoord(dx, dz);
                // 陆地地表 (<= 360m) 使用 LOD0 (含完整精确物理碰撞体供吸附)，浅滩外缘使用 LOD1
                var lod = dist <= 360.0f ? LODLevel.LOD0_Near : LODLevel.LOD1_Mid;
                var meshData = RawTileMeshData.Build(coord, lod, HeightSampler, TileSize);
                MountTile(meshData);
            }
        }
    }

    public void SetObserver(Node3D observer)
    {
        _observer = observer;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        // 1. 主线程限流提交就绪网格 (保证单帧耗时 <= MaxFrameTimeBudgetMs)
        ProcessReadyQueue();

        // 2. 观察者位移检测与视距调度
        _scheduleTimer += delta;
        if (_scheduleTimer >= 0.2) // 每 0.2 秒评估一次视野环
        {
            _scheduleTimer = 0.0;
            EvaluateObserverPosition();
        }
    }

    private void EvaluateObserverPosition()
    {
        Vector3 obsPos = Vector3.Zero;
        if (_observer != null && IsInstanceValid(_observer))
        {
            obsPos = _observer.GlobalPosition;
        }
        else
        {
            var camera = GetViewport()?.GetCamera3D();
            if (camera != null) obsPos = camera.GlobalPosition;
        }

        // 若位移小于 16 米且瓦片未变动，跳过重算
        if (obsPos.DistanceSquaredTo(_lastObserverPos) < 256.0f)
        {
            return;
        }
        _lastObserverPos = obsPos;

        var centerTile = TerrainTileCoord.FromWorld(obsPos, TileSize);
        _lastObserverTile = centerTile;

        // 收集当前视野所需的目标瓦片与 LOD
        var neededTiles = new Dictionary<TerrainTileCoord, LODLevel>();
        for (int dz = -ViewDistanceTiles; dz <= ViewDistanceTiles; dz++)
        {
            for (int dx = -ViewDistanceTiles; dx <= ViewDistanceTiles; dx++)
            {
                var coord = new TerrainTileCoord(centerTile.X + dx, centerTile.Z + dz);
                int chebyshev = Math.Max(Math.Abs(dx), Math.Abs(dz));

                LODLevel targetLOD = chebyshev switch
                {
                    <= 1 => LODLevel.LOD0_Near,
                    <= 3 => LODLevel.LOD1_Mid,
                    <= 5 => LODLevel.LOD2_Far,
                    _ => LODLevel.Unloaded
                };

                if (targetLOD != LODLevel.Unloaded)
                {
                    neededTiles[coord] = targetLOD;
                }
            }
        }

        // 卸载远离视野的活动瓦片
        var toRemove = new List<TerrainTileCoord>();
        foreach (var (coord, tile) in _activeTiles)
        {
            if (!neededTiles.ContainsKey(coord))
            {
                toRemove.Add(coord);
            }
        }
        foreach (var coord in toRemove)
        {
            RecycleTile(coord);
        }

        // 对需要生成或升级 LOD 的瓦片调度后台异步任务
        foreach (var (coord, targetLOD) in neededTiles)
        {
            // 如果已经在活动状态且 LOD 相同，无需变更
            if (_activeTiles.TryGetValue(coord, out var activeTile) && activeTile.CurrentLOD == targetLOD)
            {
                continue;
            }

            // 如果当前已经在后台生成该瓦片，跳过
            if (_buildingTiles.TryGetValue(coord, out var buildingLOD) && buildingLOD == targetLOD)
            {
                continue;
            }

            ScheduleBuildTask(coord, targetLOD);
        }
    }

    private void ScheduleBuildTask(TerrainTileCoord coord, LODLevel lod)
    {
        _buildingTiles[coord] = lod;
        var sampler = HeightSampler;
        float tileSize = TileSize;

        Task.Run(() =>
        {
            try
            {
                var meshData = RawTileMeshData.Build(coord, lod, sampler, tileSize);
                _readyQueue.Enqueue(meshData);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[TiledTerrainManager] 瓦片异步构建失败: {coord}, {ex.Message}");
            }
            finally
            {
                _buildingTiles.TryRemove(coord, out _);
            }
        });
    }

    private void ProcessReadyQueue()
    {
        if (_readyQueue.IsEmpty) return;

        var sw = Stopwatch.StartNew();
        int mountedCount = 0;

        while (mountedCount < MaxMountPerFrame && _readyQueue.TryDequeue(out var meshData))
        {
            MountTile(meshData);
            mountedCount++;

            // 严格检查单帧耗时预算
            if (sw.Elapsed.TotalMilliseconds >= MaxFrameTimeBudgetMs)
            {
                break;
            }
        }
    }

    private void MountTile(RawTileMeshData data)
    {
        // 检查是否已有此瓦片节点
        if (!_activeTiles.TryGetValue(data.Coord, out var tile))
        {
            tile = GetOrCreateTileNode();
            _activeTiles[data.Coord] = tile;
        }

        // 装载网格与碰撞数据
        tile.ApplyMeshData(data, TerrainMaterial);
    }

    private TerrainTile GetOrCreateTileNode()
    {
        if (_tilePool.TryDequeue(out var tile))
        {
            return tile;
        }

        var newTile = new TerrainTile();
        AddChild(newTile);
        return newTile;
    }

    private void RecycleTile(TerrainTileCoord coord)
    {
        if (_activeTiles.Remove(coord, out var tile))
        {
            tile.Recycle();
            _tilePool.Enqueue(tile);
        }
    }
}

