using Godot;

namespace Celestial.Core;

public partial class GameManager : Node3D
{
    public override void _Ready()
    {
        GD.Print("Celestial - Hybrid 3D Terrain + Voxel Engine initialized.");

        var player = GetNodeOrNull<Node3D>("Player");
        var terrain = GetNodeOrNull<Terrain.TiledTerrainManager>("TiledTerrainManager");
        var ocean = GetNodeOrNull<Terrain.OceanWaterPlane>("OceanWaterPlane");

        if (player != null)
        {
            terrain?.SetObserver(player);
            ocean?.SetObserver(player);
        }

        TestRunner.RunAllTests();
    }
}
