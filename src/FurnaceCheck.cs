using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class FurnaceCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
		AddChild(player);
		player.SetPhysicsProcess(false);
		player.GetNode<HealthComponent>("HealthComponent").GrantInvulnerability(100f);
		Room room = GD.Load<PackedScene>("res://scenes/rooms/design/Furnace.tscn").Instantiate<Room>();
		AddChild(room);
		player.GlobalPosition = room.EntryPosition;
		Node enemies = room.GetNode("Enemies");
		enemies.ChildEnteredTree += enemy => enemy.ProcessMode = ProcessModeEnum.Disabled;
		Bridge[] bridges =
		{
			room.GetNode<Bridge>("NorthCrossing"),
			room.GetNode<Bridge>("SouthCrossing"),
			room.GetNode<Bridge>("EastCrossing"),
		};
		Bridge exit = room.GetNode<Bridge>("ExitCauseway");

		await Wait(1f);
		Check(enemies.GetChildCount() == 0 && !room.IsCleared, "waves wait for the player to leave the landing");
		Check(InBlood(exit.GlobalPosition), "exit causeway starts underwater");
		Check(!InBlood(bridges[0].GlobalPosition) && !InBlood(bridges[1].GlobalPosition)
			&& !InBlood(bridges[2].GlobalPosition), "all three crossings start dry");
		CheckRoutes(room);

		player.GlobalPosition = new Vector2(180f, 180f);
		await Wait(0.15f);
		Check(enemies.GetChildCount() == 8, "entering the arena starts eight enemies immediately");
		Check(bridges[0].Modulate.G < 0.95f && !InBlood(bridges[0].GlobalPosition),
			"the first bridge warns before becoming dangerous");
		await Wait(2.5f);
		Check(enemies.GetChildCount() == 14, "first wave has fourteen enemies including reinforcements");
		Check(room.GetNodeOrNull<Pickup>("HealthPickup") is null, "last-wave supplies wait for wave two");
		CheckClosed(bridges, 0);
		CheckRoutes(room);
		await Wait(6f);
		CheckClosed(bridges, 1);
		CheckRoutes(room);
		await Wait(6f);
		CheckClosed(bridges, 2);
		CheckRoutes(room);

		FreeEnemies(enemies);
		await Wait(3.1f);
		Check(enemies.GetChildCount() == 14 && !room.IsCleared, "second wave has fourteen enemies");
		Check(room.GetNodeOrNull<Pickup>("HealthPickup") is { } health
			&& health.GlobalPosition == new Vector2(90f, 190f)
			&& room.GetNodeOrNull<Pickup>("ArmorPickup") is { } armor
			&& armor.GlobalPosition == new Vector2(425f, 280f), "last-wave supplies spawn on different platforms");
		FreeEnemies(enemies);
		await Wait(0.2f);
		Check(room.IsCleared && room.GetNode<Door>("Door").IsOpen, "last kill opens the door");
		Check(!InBlood(exit.GlobalPosition) && !bridges[0].Flooded && !bridges[1].Flooded && !bridges[2].Flooded,
			"clearing the room drains every crossing and the exit causeway");
		Check(room.Navigation.FindPath(player.GlobalPosition, new Vector2(600f, 180f)).Length > 0,
			"the exit is reachable after clearing");
		await Wait(6.5f);
		Check(!bridges[0].Flooded && !bridges[1].Flooded && !bridges[2].Flooded && !InBlood(exit.GlobalPosition),
			"tides stay stopped after clearing");

		room.QueueFree();
		player.QueueFree();
		GD.Print(_failures == 0 ? "all furnace checks passed" : $"{_failures} furnace checks FAILED");
		GetTree().Quit(_failures);
	}

	private void CheckClosed(Bridge[] bridges, int closed)
	{
		bool correct = true;
		for (int i = 0; i < bridges.Length; i++)
		{
			correct &= bridges[i].Flooded == (i == closed)
				&& InBlood(bridges[i].GlobalPosition) == (i == closed);
		}
		Check(correct, $"only crossing {closed + 1} is flooded, with matching damage collision");
	}

	private void CheckRoutes(Room room)
	{
		bool connected = true;
		bool safe = true;
		PhysicsShapeQueryParameters2D query = new()
		{
			Shape = new CircleShape2D { Radius = 13f },
			CollisionMask = 1,
			CollideWithAreas = true,
		};
		foreach (Node wave in room.GetNode("Waves").GetChildren())
		foreach (Node child in wave.GetChildren())
		{
			if (child is not SpawnPoint point) continue;
			query.Transform = new Transform2D(0f, point.GlobalPosition);
			bool clear = room.GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
			safe &= clear;
			if (!clear) GD.Print($"blocked spawn: {point.GetPath()} {point.GlobalPosition}");
			Vector2[] path = room.Navigation.FindPath(point.GlobalPosition, room.EntryPosition);
			connected &= path.Length > 0;
			if (path.Length == 0) GD.Print($"no route: {point.GetPath()} {point.GlobalPosition}; viewport {room.GetViewportRect()}");
			foreach (Vector2 step in path) safe &= !InBlood(step);
		}
		Check(connected, "every spawn has a route to the landing in this tide state");
		Check(safe, "spawns and their routes stay clear of blood and obstacles");
	}

	private bool InBlood(Vector2 position) => GetViewport().World2D.DirectSpaceState.IntersectPoint(new PhysicsPointQueryParameters2D
	{
		Position = position,
		CollisionMask = 1,
		CollideWithAreas = true,
		CollideWithBodies = false,
	}, 1).Count > 0;

	private static void FreeEnemies(Node enemies)
	{
		foreach (Node enemy in enemies.GetChildren()) enemy.QueueFree();
	}

	private async Task Wait(float seconds) =>
		await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);

	private void Check(bool ok, string label)
	{
		GD.Print(ok ? "  ok   " : "  FAIL ", label);
		if (!ok) _failures++;
	}
}
