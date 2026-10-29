using Godot;

namespace The7thRing;

public partial class CombatCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		foreach (string name in new[] { "Weapon", "Shotgun", "Bazooka" })
		{
			Weapon weapon = GD.Load<PackedScene>($"res://scenes/{name}.tscn").Instantiate<Weapon>();
			Sprite2D sprite = weapon.GetNode<Sprite2D>("Barrel");
			Vector2 muzzle = weapon.GetNode<Marker2D>("Muzzle").Position;
			Check(sprite.Scale == Vector2.One && muzzle.X >= sprite.Position.X + sprite.Texture.GetWidth() / 2f,
				$"{name}: native sprite size and muzzle outside the barrel");
			weapon.Free();
		}
		Player knight = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
		Check(knight.GetNode<Node2D>("WeaponPivot").Position.Y == -9f, "weapons attach at hand height");
		knight.Free();

		Weapon pistol = GD.Load<PackedScene>("res://scenes/Weapon.tscn").Instantiate<Weapon>();
		pistol.MagazineSize = 2;
		pistol.ReloadSeconds = 0.1f;
		AddChild(pistol);
		pistol.Fire();
		Check(pistol.Loaded == 1 && !pistol.Reloading, "a shot spends one round without reloading");
		pistol.Fire();
		Check(pistol.Reloading, "an empty magazine reloads by itself");
		await ToSignal(GetTree().CreateTimer(0.2f, processAlways: true, processInPhysics: true), SceneTreeTimer.SignalName.Timeout);
		Check(!pistol.Reloading && pistol.Loaded == 2, "a finished reload refills the magazine");
		pistol.QueueFree();

		Node2D ford = new() { Position = new Vector2(-2000f, -2000f) };
		Hazard river = GD.Load<PackedScene>("res://scenes/River.tscn").Instantiate<Hazard>();
		river.GetNode<Line2D>("Path").Points = new[] { new Vector2(0f, -150f), new Vector2(0f, 150f) };
		Bridge bridge = GD.Load<PackedScene>("res://scenes/Bridge.tscn").Instantiate<Bridge>();
		bridge.Size = new Vector2(200f, 40f);
		ford.AddChild(river);
		ford.AddChild(bridge);
		AddChild(ford);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(InArea(ford.ToGlobal(new Vector2(0f, 100f))), "the river hurts beside the bridge");
		Check(!InArea(ford.ToGlobal(Vector2.Zero)), "the bridge deck is dry ground");
		bridge.FloodSeconds = 0.05f;
		bridge.Flood(true);
		await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(InArea(ford.ToGlobal(Vector2.Zero)), "a flooded bridge deck hurts");
		bridge.Flood(false);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(!InArea(ford.ToGlobal(Vector2.Zero)), "a drained bridge deck is dry again");
		ford.QueueFree();

		Hazard moat = GD.Load<PackedScene>("res://scenes/River.tscn").Instantiate<Hazard>();
		moat.Position = new Vector2(-3000f, -3000f);
		Line2D ring = moat.GetNode<Line2D>("Path");
		ring.Points = new[] { new Vector2(-100f, 0f), new Vector2(0f, -100f), new Vector2(100f, 0f), new Vector2(0f, 100f) };
		ring.Closed = true;
		ring.Width = 30f;
		AddChild(moat);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(InArea(moat.ToGlobal(new Vector2(-100f, 0f))) && InArea(moat.ToGlobal(new Vector2(100f, 0f))), "a closed river hurts all the way round");
		Check(!InArea(moat.ToGlobal(Vector2.Zero)), "the island inside a closed river is dry");
		moat.QueueFree();

		Room arena = GD.Load<PackedScene>("res://scenes/Room.tscn").Instantiate<Room>();
		arena.Position = new Vector2(-4000f, -4000f);
		Node2D ambush = new();
		ambush.AddChild(new SpawnPoint { Enemy = GD.Load<PackedScene>("res://scenes/Chaser.tscn"), Delay = 0.2f });
		arena.AddChild(ambush);
		AddChild(arena);
		arena.Spawn(ambush);
		Check(!arena.IsCleared && !arena.GetNode<Door>("Door").IsOpen, "a delayed spawn keeps the room shut before it arrives");
		await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
		Check(arena.GetNode("Enemies").GetChildCount() == 1, "a delayed spawn arrives after its delay");
		arena.QueueFree();

		Room held = GD.Load<PackedScene>("res://scenes/Room.tscn").Instantiate<Room>();
		held.Position = new Vector2(-6000f, -6000f);
		held.WavesWaitForAmbush = true;
		for (int i = 0; i < 2; i++)
		{
			Node2D wave = new();
			wave.AddChild(new SpawnPoint { Enemy = GD.Load<PackedScene>("res://scenes/Chaser.tscn") });
			if (i == 1)
			{
				wave.AddChild(new SpawnPoint
				{
					Pickup = GD.Load<PackedScene>("res://scenes/HealthPickup.tscn"),
					Position = new Vector2(40f, 0f),
				});
			}

			held.GetNode("Waves").AddChild(wave);
		}

		AddChild(held);
		Node heldEnemies = held.GetNode("Enemies");
		Check(heldEnemies.GetChildCount() == 0 && !held.IsCleared, "held waves wait, with the room shut, until the ambush");
		held.StartWaves();
		Check(heldEnemies.GetChildCount() == 1, "the ambush starts the first wave");
		Check(held.GetNodeOrNull<Pickup>("HealthPickup") is null, "wave supplies wait for their wave");
		heldEnemies.GetChild(0).Free();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(heldEnemies.GetChildCount() == 1 && !held.IsCleared, "clearing a wave brings the next one, not the exit");
		Check(held.GetNodeOrNull<Pickup>("HealthPickup") is { } supply
			&& supply.GlobalPosition == held.GlobalPosition + new Vector2(40f, 0f),
			"wave supplies appear at their marker outside the enemy container");
		heldEnemies.GetChild(0).Free();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(held.IsCleared, "the room clears after its last wave");
		held.QueueFree();

		Hazard geyser = GD.Load<PackedScene>("res://scenes/Geyser.tscn").Instantiate<Hazard>();
		geyser.Position = new Vector2(-5000f, -5000f);
		geyser.Manual = true;
		geyser.TelegraphSeconds = 0.05f;
		geyser.ActiveSeconds = 0.1f;
		AddChild(geyser);
		await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		Check(geyser.CurrentPhase == Hazard.Phase.Rest, "a manual geyser waits instead of keeping its own rhythm");
		geyser.Erupt();
		Check(geyser.CurrentPhase == Hazard.Phase.Telegraph, "erupting a manual geyser warns first");
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		Check(geyser.CurrentPhase == Hazard.Phase.Active, "a manual geyser erupts after its warning");
		await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
		Check(geyser.CurrentPhase == Hazard.Phase.Rest, "a manual geyser settles back to rest");
		geyser.QueueFree();

		foreach (string name in new[] { "Chaser", "Shooter", "Flanker", "Charger", "Boss" })
		{
			Enemy enemy = GD.Load<PackedScene>($"res://scenes/{name}.tscn").Instantiate<Enemy>();
			enemy.Position = new Vector2(1000f, 1000f);
			enemy.ProcessMode = ProcessModeEnum.Disabled;
			enemy.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
			enemy.GetNode<Area2D>("Hurtbox").DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
			AddChild(enemy);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			AnimatedSprite2D sprite = enemy.GetNode<AnimatedSprite2D>("Body");
			bool covered = true;
			PhysicsPointQueryParameters2D point = new()
			{
				CollisionMask = 128,
				CollideWithAreas = true,
				CollideWithBodies = false,
			};
			Vector2 head = Vector2.Zero;
			foreach (StringName animation in sprite.SpriteFrames.GetAnimationNames())
			{
				for (int frame = 0; frame < sprite.SpriteFrames.GetFrameCount(animation); frame++)
				{
					Image image = sprite.SpriteFrames.GetFrameTexture(animation, frame).GetImage();
					for (int y = 0; y < image.GetHeight(); y++)
					{
						for (int x = 0; x < image.GetWidth(); x++)
						{
							if (image.GetPixel(x, y).A == 0f) continue;
							Vector2 local = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(image.GetWidth(), image.GetHeight()) / 2f + sprite.Offset) * sprite.Scale;
							if (head == Vector2.Zero) head = local;
							foreach (float facing in new[] { -1f, 1f })
							{
								point.Position = enemy.GlobalPosition + new Vector2(local.X * facing, local.Y);
								covered &= enemy.GetWorld2D().DirectSpaceState.IntersectPoint(point, 1).Count == 1;
							}
						}
					}
				}
			}
			Check(covered, $"{name}: every visible pixel in idle/run is covered in both directions");
			point.Position = enemy.GlobalPosition + new Vector2(100f, 0f);
			Check(enemy.GetWorld2D().DirectSpaceState.IntersectPoint(point, 1).Count == 0,
				$"{name}: empty space outside the sprite is not a target");
			HealthComponent health = enemy.GetNode<HealthComponent>("HealthComponent");
			int before = health.Current;
			Bullet bullet = GD.Load<PackedScene>("res://scenes/Bullet.tscn").Instantiate<Bullet>();
			bullet.Position = enemy.GlobalPosition + head;
			bullet.Speed = 0f;
			AddChild(bullet);
			for (int i = 0; i < 3; i++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(health.Current == before - 1, $"{name}: an actual bullet hitting the head deals damage");
			Bullet duplicate = new() { Damage = 1 };
			AddChild(duplicate);
			before = health.Current;
			duplicate.Hit(enemy);
			duplicate.Hit(enemy);
			Check(health.Current == before - 1, $"{name}: one projectile cannot deal duplicate damage");
			if (name is "Boss" or "Charger")
			{
				if (name == "Charger") health.Reset();
				bool killed = false;
				health.Died += () => killed = true;
				Bullet rocket = GD.Load<PackedScene>("res://scenes/Rocket.tscn").Instantiate<Bullet>();
				rocket.Position = enemy.GlobalPosition + head;
				rocket.Speed = 0f;
				before = health.Current;
				AddChild(rocket);
				for (int i = 0; i < 3; i++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				Check(name == "Charger" ? killed : health.Current == before - 8,
					name == "Charger" ? "one rocket kills a full-health Charger" : "a rocket hitting the boss head deals eight damage");
			}
			if (IsInstanceValid(enemy)) enemy.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		CharacterBody2D player = new() { Position = new Vector2(2060f, 2010f), CollisionLayer = 2, CollisionMask = 0 };
		player.AddToGroup("player");
		player.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 4f } });
		HealthComponent playerHealth = new() { Name = "HealthComponent", MaxHealth = 10 };
		player.AddChild(playerHealth);
		AddChild(player);
		StaticBody2D wall = new() { Position = new Vector2(2040f, 2000f), CollisionLayer = 1, CollisionMask = 0 };
		wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(8f, 100f) } });
		AddChild(wall);
		Enemy friend = GD.Load<PackedScene>("res://scenes/Chaser.tscn").Instantiate<Enemy>();
		friend.Position = new Vector2(2020f, 2030f);
		friend.ProcessMode = ProcessModeEnum.Disabled;
		friend.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		friend.GetNode<Area2D>("Hurtbox").DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		AddChild(friend);
		ExplosiveBarrel barrel = GD.Load<PackedScene>("res://scenes/ExplosiveBarrel.tscn").Instantiate<ExplosiveBarrel>();
		barrel.Position = new Vector2(2020f, 1970f);
		AddChild(barrel);
		ExplosiveBarrel otherBarrel = GD.Load<PackedScene>("res://scenes/ExplosiveBarrel.tscn").Instantiate<ExplosiveBarrel>();
		otherBarrel.Position = new Vector2(1900f, 1970f);
		AddChild(otherBarrel);
		Check(barrel.GetNode<Sprite2D>("Body").Material != otherBarrel.GetNode<Sprite2D>("Body").Material,
			"barrel warning materials are independent");
		Charger charger = GD.Load<PackedScene>("res://scenes/Charger.tscn").Instantiate<Charger>();
		charger.Position = new Vector2(2000f, 2000f);
		charger.ProcessMode = ProcessModeEnum.Disabled;
		charger.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		charger.GetNode<Area2D>("Hurtbox").DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		charger.WindUpSeconds = 0.01f;
		charger.ContactDamage = 0;
		AddChild(charger);
		for (int i = 0; i < 25; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			charger._PhysicsProcess(1.0 / 60.0);
		}
		Check(playerHealth.Current == 9, "a charge into a wall damages a nearby player exactly once");
		Check(charger.GetNode<HealthComponent>("HealthComponent").Current == 6
			&& friend.GetNode<HealthComponent>("HealthComponent").Current == 3
			&& barrel.FuseLeft < 0f,
			"the impact spares the charger, nearby enemies and barrels");
		charger.QueueFree();

		player.Position = new Vector2(2440f, 2000f);
		Charger timeout = GD.Load<PackedScene>("res://scenes/Charger.tscn").Instantiate<Charger>();
		timeout.Position = new Vector2(2400f, 2000f);
		timeout.ProcessMode = ProcessModeEnum.Disabled;
		timeout.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		timeout.GetNode<Area2D>("Hurtbox").DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		timeout.WindUpSeconds = 0.01f;
		timeout.MaxChargeSeconds = 0.02f;
		timeout.ContactDamage = 0;
		AddChild(timeout);
		for (int i = 0; i < 10; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			timeout._PhysicsProcess(1.0 / 60.0);
		}
		Check(playerHealth.Current == 9, "a charge timing out without a wall causes no explosion");
		timeout.QueueFree();
		player.Position = new Vector2(2060f, 2010f);
		Charger walking = GD.Load<PackedScene>("res://scenes/Charger.tscn").Instantiate<Charger>();
		walking.Position = new Vector2(2025f, 2000f);
		walking.ProcessMode = ProcessModeEnum.Disabled;
		walking.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		walking.GetNode<Area2D>("Hurtbox").DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
		walking.ChargeRange = 0f;
		walking.ContactDamage = 0;
		AddChild(walking);
		for (int i = 0; i < 10; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			walking._PhysicsProcess(1.0 / 60.0);
		}
		Check(playerHealth.Current == 9, "walking into a wall without charging causes no explosion");
		GD.Print(_failures == 0 ? "all combat checks passed" : $"{_failures} combat checks FAILED");
		GetTree().Quit(_failures);
	}

	private bool InArea(Vector2 point) => GetViewport().World2D.DirectSpaceState.IntersectPoint(new PhysicsPointQueryParameters2D
	{
		Position = point,
		CollideWithAreas = true,
		CollideWithBodies = false,
	}).Count > 0;

	private void Check(bool ok, string what)
	{
		GD.Print(ok ? "  ok   " : "  FAIL ", what);
		if (!ok) _failures++;
	}
}

