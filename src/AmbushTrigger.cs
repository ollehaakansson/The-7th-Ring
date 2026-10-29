using Godot;

namespace The7thRing;

public partial class AmbushTrigger : Area2D
{
	[Export] public bool FloodsBridges { get; set; } = true;

	private Room _room = null!;
	private bool _triggered;

	public override void _Ready()
	{
		_room = GetParent<Room>();
		_room.Cleared += () => FloodBridges(false);
		BodyEntered += body =>
		{
			if (body is Player && !_triggered)
			{
				_triggered = true;
				// Deferred: bodies cannot be added during the physics callback that reported the player.
				Callable.From(Spring).CallDeferred();
			}
		};
	}

	private void Spring()
	{
		_room.Spawn(this);
		_room.StartWaves();
		if (FloodsBridges && !_room.IsCleared)
		{
			FloodBridges(true);
		}
	}

	private void FloodBridges(bool flooded)
	{
		if (!FloodsBridges)
		{
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup(Bridge.Group))
		{
			if (node is Bridge bridge && _room.IsAncestorOf(bridge))
			{
				bridge.Flood(flooded);
			}
		}
	}
}
