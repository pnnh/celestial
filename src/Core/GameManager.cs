using Godot;

namespace Celestial.Core;

public partial class GameManager : Node3D
{
    public override void _Ready()
    {
        GD.Print("Celestial - Hybrid 3D Terrain + Voxel Engine initialized.");
    }
}

