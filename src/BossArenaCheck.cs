using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class BossArenaCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		Main main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
		main.Rooms = new[] { "res://scenes/rooms/design/Geryon.tscn" };
		AddChild(main);
		Room room = main.CurrentRoom!;
		Player player = main.GetNode<Player>("Player");
		player.SetPhysicsProcess(false);
		player.GetNode<HealthComponent>("HealthComponent").GrantInvulnerability(100f);
		Boss boss = room.GetNode<Boss>("Enemies/Boss");
		HealthComponent health = boss.GetNode<HealthComponent>("HealthComponent");
		ProgressBar bar = room.GetNode<ProgressBar>("BossUI/Health");
		Node fire = room.GetNode("FireRing");
		await Wait(0.1f);
		Check(health.Current == 120 && bar.Value == 120 && bar.MaxValue == 120, "boss starts at 120 HP with matching health bar");
		Check(boss.ProcessMode == ProcessModeEnum.Disabled && !room.IsCleared, "boss waits and exit stays locked before entry");
		Check(AllInPhase(fire, Hazard.Phase.Rest), "outer arena starts safe");
		Check(Mathf.IsEqualApprox(boss.SummonCooldown, 6f), "phase one keeps the six-second summon cooldown");

		player.GlobalPosition = new Vector2(150f, 180f);
		await Wait(0.1f);
		Check(boss.ProcessMode == ProcessModeEnum.Inherit, "entry starts the boss fight");
		boss.SetPhysicsProcess(false);
		health.TakeDamage(59);
		await Wait(0.1f);
		Check(bar.Value == 61 && AllInPhase(fire, Hazard.Phase.Rest), "above half HP the full arena stays open");
		Check(room.GetNodeOrNull<Pickup>("HealthPickup") is null, "phase-two supplies wait for half health");
		health.TakeDamage(1);
		await Wait(0.1f);
		Check(bar.Value == 60 && AllInPhase(fire, Hazard.Phase.Telegraph), "half health warns before the fire starts");
		Check(boss.VolleyShots == 11 && boss.AddsPerSummon == 4
			&& Mathf.IsEqualApprox(boss.RestSeconds, 0.25f) && Mathf.IsEqualApprox(boss.WindUpSeconds, 0.3f), "second phase increases boss pressure");
		Check(Mathf.IsEqualApprox(boss.SummonCooldown, 7.5f), "phase two eases minion pressure with a 7.5-second summon cooldown");
		Check(room.GetNodeOrNull<Pickup>("HealthPickup") is { } supply
			&& supply.Position == new Vector2(180f, 230f)
			&& room.GetNodeOrNull<Pickup>("ArmorPickup") is { } armor
			&& armor.Position == new Vector2(460f, 120f), "phase supplies appear on opposite sides");
		health.Heal(1);
		health.TakeDamage(1);
		await Wait(0.1f);
		int pickups = 0;
		foreach (Node child in room.GetChildren()) if (child is Pickup) pickups++;
		Check(pickups == 5, "crossing half health again cannot duplicate supplies");
		await Wait(1.4f);
		Check(AllInPhase(fire, Hazard.Phase.Active), "outer arena burns after its warning");
		Check(DangerAt(new Vector2(70f, 180f)) && DangerAt(new Vector2(320f, 40f))
			&& !DangerAt(new Vector2(320f, 180f)), "fire closes the outside and leaves the centre safe");
		foreach (Vector2 point in new[] { new Vector2(180f, 230f), new Vector2(460f, 120f) })
		{
			Vector2[] path = room.Navigation.FindPath(player.GlobalPosition, point);
			bool safe = path.Length > 0;
			foreach (Vector2 step in path) safe &= !DangerAt(step);
			Check(safe && !DangerAt(point), "phase supplies have a safe route inside the fire ring");
		}

		Node enemies = room.GetNode("Enemies");
		Enemy[] blockers = new Enemy[3];
		Vector2[] blockerPositions = { new(130f, 180f), new(130f, 122f), new(130f, 238f) };
		for (int i = 0; i < blockers.Length; i++)
		{
			Enemy blocker = GD.Load<PackedScene>("res://scenes/Flanker.tscn").Instantiate<Enemy>();
			blocker.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
			blocker.ProcessMode = ProcessModeEnum.Disabled;
			enemies.AddChild(blocker);
			blocker.GlobalPosition = blockerPositions[i];
			blockers[i] = blocker;
		}
		boss.AddsPerSummon = 0;
		boss.SetPhysicsProcess(true);
		foreach (Vector2 position in new[] { new Vector2(72f, 180f), new Vector2(568f, 180f) })
		{
			boss.GlobalPosition = position;
			await Wait(2.2f);
			Check(!EnemyInDanger(boss) && boss.GlobalPosition.DistanceTo(position) > 60f,
				"boss escapes deep fire even with minions blocking the way");
		}

		foreach (Enemy blocker in blockers) blocker.QueueFree();
		await Wait(0.1f);
		int peakAdds = 0;
		int spawned = 0;
		enemies.ChildEnteredTree += enemy =>
		{
			enemy.ProcessMode = ProcessModeEnum.Disabled;
			spawned++;
			peakAdds = Mathf.Max(peakAdds, enemies.GetChildCount() - 1);
		};
		boss.GlobalPosition = new Vector2(320f, 180f);
		boss.Speed = 0f;
		boss.ChargeSpeed = 0f;
		boss.RestSeconds = 0.01f;
		boss.WindUpSeconds = 0.01f;
		boss.MaxChargeSeconds = 0.01f;
		boss.DazedSeconds = 0.01f;
		boss.AddsPerSummon = 4;
		for (int i = 0; i < 120 && enemies.GetChildCount() == 1; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(enemies.GetChildCount() == 5, "a summon produces four enemies");
		await Wait(1f);
		Check(enemies.GetChildCount() == 5, "summon cooldown prevents rapid repeat spawns despite fast attacks");
		boss.SummonCooldown = 0.05f;
		await Wait(7f);
		Check(peakAdds <= 16 && enemies.GetChildCount() == 17, "repeated summons stop at sixteen living minions plus the boss");
		int flankers = 0, chasers = 0, shooters = 0, chargers = 0;
		foreach (Node enemy in enemies.GetChildren())
		{
			if (enemy is Flanker) flankers++;
			if (enemy is Chaser) chasers++;
			if (enemy is Shooter) shooters++;
			if (enemy is Charger) chargers++;
		}
		Check(flankers > 8 && chasers > 0 && shooters > 0 && chargers > 0,
			"summons contain all four enemy types with a Flanker majority");
		int beforeRefill = spawned;
		enemies.GetChild(1).GetNode<HealthComponent>("HealthComponent").TakeDamage(1000);
		await Wait(0.5f);
		Check(enemies.GetChildCount() == 17 && spawned == beforeRefill + 1,
			"a four-enemy summon fills only one vacant slot without exceeding the cap");
		enemies.GetChild<Node2D>(1).GlobalPosition = new Vector2(700f, 180f);
		enemies.GetChild<Node2D>(2).GlobalPosition = new Vector2(72f, 180f);
		health.TakeDamage(120);
		await Wait(0.1f);
		Check(room.GetNode("Enemies").GetChildCount() == 0, "boss death removes every surviving add");
		Check(room.IsCleared && room.GetNode<Door>("Door").IsOpen && !room.HasNode("FireRing"),
			"boss death opens the exit and extinguishes the ring even with trapped adds");
		Check(!DangerAt(new Vector2(600f, 180f)), "exit becomes safe after combat");
		player.GlobalPosition = new Vector2(632f, 180f);
		for (int i = 0; i < 6; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(GetTree().Paused && main.CurrentRoom is null, "final exit ends the run");
		GetTree().Paused = false;
		main.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GD.Print(_failures == 0 ? "all boss arena checks passed" : $"{_failures} boss arena checks FAILED");
		GetTree().Quit(_failures);
	}

	private static bool AllInPhase(Node fire, Hazard.Phase phase)
	{
		foreach (Node child in fire.GetChildren())
		{
			if (child is Hazard hazard && hazard.CurrentPhase != phase) return false;
		}
		return true;
	}

	private static bool EnemyInDanger(Enemy enemy)
	{
		using PhysicsShapeQueryParameters2D query = new()
		{
			Shape = enemy.GetNode<CollisionShape2D>("CollisionShape2D").Shape,
			Transform = enemy.GlobalTransform,
			CollisionMask = 1,
			CollideWithAreas = true,
			CollideWithBodies = false,
		};
		return enemy.GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count > 0;
	}

	private bool DangerAt(Vector2 position) => GetViewport().World2D.DirectSpaceState.IntersectPoint(new PhysicsPointQueryParameters2D
	{
		Position = position,
		CollisionMask = 1,
		CollideWithAreas = true,
		CollideWithBodies = false,
	}, 1).Count > 0;

	private async Task Wait(float seconds) =>
		await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);

	private void Check(bool ok, string label)
	{
		GD.Print(ok ? "  ok   " : "  FAIL ", label);
		if (!ok) _failures++;
	}
}
