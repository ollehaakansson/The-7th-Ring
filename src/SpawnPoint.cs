using Godot;

namespace The7thRing;

public partial class SpawnPoint : Marker2D
{
	[Export] public PackedScene? Enemy { get; set; }
	[Export] public PackedScene? Pickup { get; set; }

	[Export] public float Delay { get; set; }
}
