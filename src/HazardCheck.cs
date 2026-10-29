using Godot;

namespace The7thRing;

public partial class HazardCheck : Node2D
{
	private const int Frames = 1200;

	public override async void _Ready()
	{
		int failures = 0;

		foreach (string path in new[] { "Room02", "Room03", "Room04", "Room05", "Room06", "Room07", "Room08", "Sandbox" })
		foreach (bool atDoor in new[] { false, true })
		{
			Room room = GD.Load<PackedScene>($"res://scenes/rooms/{path}.tscn").Instantiate<Room>();
			// Remove barrels, they'd explode from enemy shots.
			foreach (Node child in room.GetChildren())
			{
				if (child is ExplosiveBarrel)
				{
					child.Free();
				}
			}

			Node2D player = new();
			player.AddToGroup("player");
			AddChild(player);
			AddChild(room);
			player.GlobalPosition = atDoor ? ClearSpotNearDoor(room) : room.EntryPosition;

			int hits = 0;
			int enemies = 0;
			foreach (Node enemy in room.GetNode("Enemies").GetChildren())
			{
				enemies++;
				enemy.GetNode<HealthComponent>("HealthComponent").Damaged += _ => hits++;
			}

			for (int i = 0; i < Frames; i++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}

			GD.Print($"  {(hits == 0 ? "ok  " : "FAIL")} {path} {(atDoor ? "door " : "entry")}: {enemies} enemies took {hits} hazard hits");
			failures += hits == 0 ? 0 : 1;

			room.Free();
			player.Free();
		}

		GD.Print(failures == 0 ? "all checks passed" : $"{failures} FAILED");
		GetTree().Quit(failures);
	}

	private static Vector2 ClearSpotNearDoor(Room room)
	{
		Vector2 spot = room.GetNode<Node2D>("Door").GlobalPosition + new Vector2(-40f, 0f);
		for (; spot.X > 0f; spot.X -= 10f)
		{
			bool clear = true;
			foreach (Node child in room.GetChildren())
			{
				if (child is Hazard hazard)
				{
					Vector2 size = ((RectangleShape2D)hazard.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Size * hazard.Scale;
					clear &= !new Rect2(hazard.GlobalPosition - size / 2f, size).Grow(30f).HasPoint(spot);
				}
			}

			if (clear)
			{
				break;
			}
		}

		return spot;
	}
}
