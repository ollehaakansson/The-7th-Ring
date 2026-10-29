using Godot;

namespace The7thRing;

// Run headless: ./scripts/run.sh --headless scenes/tests/HealthCheck.tscn
public partial class HealthCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		HealthComponent h = new() { MaxHealth = 3, MaxArmor = 2 };
		AddChild(h);
		HUD hud = GD.Load<PackedScene>("res://scenes/HUD.tscn").Instantiate<HUD>();
		AddChild(hud);
		hud.Track(h);
		HBoxContainer armorRow = hud.GetNode<HBoxContainer>("Armor");
		Check(!armorRow.Visible, "the shield row stays hidden without armor");

		Check(h.Current == 3 && h.Armor == 0, "starts full, with no armor");
		Check(!h.Heal(1), "healing a full pool gives nothing");

		h.TakeDamage(1);
		Check(h.Current == 2, "damage without armor hits health");
		Check(h.Heal(1) && h.Current == 3, "healing restores it");

		Check(h.AddArmor(1) && h.Armor == 1, "armor can be picked up");
		Pickup armorPickup = GD.Load<PackedScene>("res://scenes/ArmorPickup.tscn").Instantiate<Pickup>();
		Check(armorRow.Visible && armorRow.GetChildCount() == 2
			&& armorRow.GetChild<TextureRect>(0).Texture == armorPickup.GetNode<Sprite2D>("Visual").Texture
			&& armorRow.GetChild<TextureRect>(0).Modulate == Colors.White
			&& armorRow.GetChild<TextureRect>(1).Modulate != Colors.White,
			"armor uses the pickup shield and distinguishes filled and empty slots");
		armorPickup.Free();
		h.TakeDamage(2);
		Check(h.Armor == 0 && h.Current == 3, "armor swallows the whole hit");

		h.AddArmor(5);
		Check(h.Armor == 2, "armor stops at the maximum");
		Check(!h.AddArmor(1), "and gives nothing once there");

		h.Reset();
		Check(h.Current == 3 && h.Armor == 0, "a restart clears armor too");

		h.TakeDamage(3);
		Check(!h.IsAlive && !h.Heal(1), "the dead do not heal");

		// Time is stepped by hand from here.
		HealthComponent d = new() { MaxHealth = 3, InvulnerableSeconds = 0.6f };
		AddChild(d);

		d.GrantInvulnerability(0.05f);
		d.TakeDamage(1);
		Check(d.Current == 3, "a granted window blocks damage");

		d._PhysicsProcess(0.2);
		d.TakeDamage(1);
		Check(d.Current == 2, "and lets it through once the window is over");

		d.GrantInvulnerability(0.05f);
		d._PhysicsProcess(0.2);
		d.TakeDamage(1);
		Check(d.Current == 2, "a short roll cannot cut the hit window short");

		Chaser enemy = new() { CollisionLayer = 4, CollisionMask = 5 };
		enemy.AddChild(new Node2D { Name = "Body" });
		enemy.AddChild(new CollisionShape2D
		{
			Name = "CollisionShape2D",
			Shape = new RectangleShape2D { Size = new Vector2(14f, 14f) },
		});
		enemy.AddChild(new HealthComponent { Name = "HealthComponent" });
		AddChild(enemy);

		Bullet bullet = new() { Damage = 1, Knockback = 12f, GlobalRotation = Mathf.Pi / 2f };
		AddChild(bullet);
		bullet.Hit(enemy);
		enemy._PhysicsProcess(0);

		Check(enemy.Position.IsEqualApprox(new Vector2(0f, 12f)), "a bullet shoves along its shot direction");
		enemy.Position = new Vector2(400f, 400f);

		Node2D victim = new();
		HealthComponent victimHealth = new()
		{
			Name = "HealthComponent",
			MaxHealth = 2,
			InvulnerableSeconds = 0.6f,
		};
		victim.AddChild(victimHealth);
		AddChild(victim);

		Color rest = new(0.1f, 0.1f, 0.1f);
		Color warning = new(0.5f, 0.5f, 0.1f);
		Color active = new(1f, 0.1f, 0.1f);
		Hazard hazard = new()
		{
			Damage = 1,
			Periodic = true,
			RestSeconds = 0.1f,
			TelegraphSeconds = 0.1f,
			RestColour = rest,
			TelegraphColour = warning,
			ActiveColour = active,
		};
		Polygon2D hazardVisual = new() { Name = "Visual" };
		hazard.AddChild(hazardVisual);
		AddChild(hazard);

		Check(hazardVisual.Color == rest && hazard.CollisionLayer == 0,
			"a periodic hazard starts safe and walkable");
		hazard._PhysicsProcess(0.11);
		Check(hazardVisual.Color == warning && hazard.CollisionLayer == 1,
			"its warning makes the area unsafe to path through");
		hazard._PhysicsProcess(0.11);
		Check(hazardVisual.Color == active && hazard.CollisionLayer == 1,
			"it stays unsafe while active");

		hazard.DamageBody(victim);
		hazard.DamageBody(victim);

		Check(victimHealth.Current == 0, "hazard ticks are not blocked by hit immunity");

		victimHealth.Reset();
		victimHealth.GrantInvulnerability(1f);
		hazard.DamageBody(victim);
		Check(victimHealth.Current == 2, "a dodge still protects from a hazard");

		hazard._PhysicsProcess(hazard.ActiveSeconds + 0.01f);
		Check(hazardVisual.Color == rest && hazard.CollisionLayer == 0,
			"the area becomes walkable again when the rain ends");

		Hazard pathHazard = GD.Load<PackedScene>("res://scenes/BoilingBlood.tscn").Instantiate<Hazard>();
		pathHazard.Position = new Vector2(80f, 64f);
		AddChild(pathHazard);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		Vector2[] safePath = new RoomNavigation(pathHazard.GetWorld2D(), new Vector2(160f, 128f))
			.FindPath(new Vector2(16f, 64f), new Vector2(144f, 64f));
		bool avoidsHazard = safePath.Length > 0;
		foreach (Vector2 point in safePath)
		{
			avoidsHazard &= Mathf.Abs(point.X - 80f) >= 48f || Mathf.Abs(point.Y - 64f) >= 36f;
		}
		Check(avoidsHazard, "navigation routes around a hazard instead of through it");
		pathHazard.QueueFree();

		StaticBody2D pathWall = new() { Position = new Vector2(80f, 64f), CollisionLayer = 1 };
		pathWall.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(16f, 64f) },
		});
		AddChild(pathWall);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		Vector2[] nearWallPath = new RoomNavigation(pathWall.GetWorld2D(), new Vector2(160f, 128f))
			.FindPath(new Vector2(64f, 64f), new Vector2(144f, 64f));
		Check(nearWallPath.Length > 0 && nearWallPath[0].X < 72f,
			"navigation escapes a start cell blocked only by wall clearance");
		pathWall.QueueFree();

		PackedScene pistolScene = GD.Load<PackedScene>("res://scenes/Weapon.tscn");
		PackedScene shotgunScene = GD.Load<PackedScene>("res://scenes/Shotgun.tscn");
		PackedScene bazookaScene = GD.Load<PackedScene>("res://scenes/Bazooka.tscn");
		PackedScene weaponPickupScene = GD.Load<PackedScene>("res://scenes/WeaponPickup.tscn");

		Player armed = new()
		{
			StartingWeaponScene = pistolScene,
			WeaponPickupScene = weaponPickupScene,
		};
		Node2D pivot = new() { Name = "WeaponPivot" };
		Weapon pistol = pistolScene.Instantiate<Weapon>();
		pistol.Name = "Weapon";
		pivot.AddChild(pistol);
		armed.AddChild(pivot);
		HealthComponent armedHealth = new() { Name = "HealthComponent", MaxHealth = 3 };
		armed.AddChild(armedHealth);
		armed.CollisionLayer = 2;
		armed.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 4f } });
		AddChild(armed);
		pistol.Rotation = Mathf.Pi;
		pistol._PhysicsProcess(0);
		Check(pistol.Scale.Y < 0f, "aiming left keeps the weapon upright");
		pistol.Rotation = 0f;
		pistol._PhysicsProcess(0);
		Check(pistol.Scale.Y > 0f, "aiming right restores the weapon orientation");

		WeaponPickup shotgunPickup = weaponPickupScene.Instantiate<WeaponPickup>();
		shotgunPickup.WeaponScene = shotgunScene;
		AddChild(shotgunPickup);
		Check(shotgunPickup.GetNode<Sprite2D>("Visual").Texture.ResourcePath.EndsWith("/shotgun.png"),
			"a weapon pickup uses its weapon's sprite");
		Check(armed.TryCollectWeapon(shotgunPickup, replace: false) && armed.WeaponCount == 2,
			"a free weapon slot collects automatically");

		armed.SwitchWeapon();
		Check(armed.ActiveWeaponPath == shotgunScene.ResourcePath, "Q switches the active weapon");

		int childrenBeforeShot = GetChildCount();
		Vector2 positionBeforeShot = armed.Position;
		armed.ActiveWeapon.Fire();
		armed._PhysicsProcess(0);
		Check(GetChildCount() == childrenBeforeShot + 5, "the shotgun fires five pellets at once");
		Check(armed.Position.X < positionBeforeShot.X, "the shotgun pushes its wielder backwards");
		for (int i = childrenBeforeShot; i < GetChildCount(); i++)
		{
			GetChild(i).QueueFree();
		}

		WeaponPickup bazookaPickup = weaponPickupScene.Instantiate<WeaponPickup>();
		bazookaPickup.WeaponScene = bazookaScene;
		AddChild(bazookaPickup);
		Check(!armed.TryCollectWeapon(bazookaPickup, replace: false), "a full inventory leaves a weapon on the ground");
		Check(bazookaPickup.GetNode<Sprite2D>("Visual").Texture.ResourcePath.EndsWith("/bazooka.png"),
			"the bazooka pickup uses the custom bazooka sprite");
		Check(armed.TryCollectWeapon(bazookaPickup, replace: true)
			&& armed.ActiveWeaponPath == bazookaScene.ResourcePath,
			"interact replaces the active weapon");
		bool droppedSpriteMatches = false;
		foreach (Node child in GetChildren())
		{
			if (child is WeaponPickup dropped && !dropped.IsQueuedForDeletion()
				&& dropped.WeaponScene == shotgunScene)
			{
				droppedSpriteMatches = dropped.GetNode<Sprite2D>("Visual").Texture.ResourcePath.EndsWith("/shotgun.png");
			}
		}
		Check(droppedSpriteMatches, "a dropped weapon keeps its own sprite after a swap");

		CharacterBody2D blastVictim = new() { Position = new Vector2(30f, 0f), CollisionLayer = 4 };
		HealthComponent blastHealth = new() { Name = "HealthComponent", MaxHealth = 3 };
		blastVictim.AddChild(blastHealth);
		blastVictim.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 4f } });
		AddChild(blastVictim);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		int childrenBeforeRocket = GetChildCount();
		armed.ActiveWeapon.Fire();
		Bullet rocket = (Bullet)GetChild(GetChildCount() - 1);
		Check(GetChildCount() == childrenBeforeRocket + 1 && rocket.ExplosionRadius > 0f,
			"the bazooka fires one explosive projectile");
		rocket.Speed = 0f;
		rocket.GlobalPosition = Vector2.Zero;
		rocket.Hit(blastVictim);
		Check(armedHealth.Current == 1 && blastHealth.Current == 0,
			"a rocket kills a nearby enemy while player splash damage stays at two");
		Check(GetNodeOrNull<Polygon2D>("ExplosionRadius") is { } radius
			&& radius.Color.A == 0.4f,
			"a rocket explosion shows its radius");

		armed.StartDodge(Vector2.Right);
		Line2D dodgeTrail = GetNode<Line2D>("DodgeTrail");
		armed._PhysicsProcess(0);
		Check(armed.Modulate == Player.DodgeColour && dodgeTrail.DefaultColor.A == 0.3f,
			"a dodge turns the player blue and leaves a faint trail");
		armed._PhysicsProcess(armed.DodgeSeconds + 0.01f);
		armed._PhysicsProcess(0);
		Check(armed.Modulate == Colors.White,
			"the player returns to normal after dodging");

		PackedScene barrelScene = GD.Load<PackedScene>("res://scenes/ExplosiveBarrel.tscn");
		ExplosiveBarrel timedBarrel = barrelScene.Instantiate<ExplosiveBarrel>();
		timedBarrel.Position = new Vector2(400f, 400f);
		AddChild(timedBarrel);
		HealthComponent timedBarrelHealth = timedBarrel.GetNode<HealthComponent>("HealthComponent");
		timedBarrelHealth.TakeDamage(1);
		Check(Mathf.IsEqualApprox(timedBarrel.FuseLeft, 2f),
			"one damage starts a two-second barrel fuse");
		timedBarrel._PhysicsProcess(0.05);
		ShaderMaterial fuseMaterial = (ShaderMaterial)timedBarrel.GetNode<Sprite2D>("Body").Material;
		float firstFlash = (float)fuseMaterial.GetShaderParameter("flash_strength");
		timedBarrel._PhysicsProcess(0.15);
		Check(firstFlash == 0.9f && (float)fuseMaterial.GetShaderParameter("flash_strength") == 0f,
			"an armed barrel alternates its original sprite with a bright flash");
		timedBarrel._PhysicsProcess(0.15);
		timedBarrel._PhysicsProcess(0.15);
		timedBarrelHealth.TakeDamage(1);
		Check(Mathf.IsEqualApprox(timedBarrel.FuseLeft, 1f),
			"more damage shortens the barrel fuse");
		timedBarrel._PhysicsProcess(1.1);
		Check(timedBarrel.HasExploded,
			"barrel explodes when its shortened fuse expires");

		ExplosiveBarrel minimumFuseBarrel = barrelScene.Instantiate<ExplosiveBarrel>();
		minimumFuseBarrel.Position = new Vector2(550f, 400f);
		AddChild(minimumFuseBarrel);
		HealthComponent minimumFuseHealth = minimumFuseBarrel.GetNode<HealthComponent>("HealthComponent");
		minimumFuseHealth.TakeDamage(1);
		minimumFuseHealth.TakeDamage(1);
		minimumFuseHealth.TakeDamage(1);
		Check(!minimumFuseBarrel.HasExploded
			&& Mathf.IsEqualApprox(minimumFuseBarrel.FuseLeft, 0.1f),
			"three damage arms the minimum fuse instead of exploding immediately");
		minimumFuseBarrel._PhysicsProcess(0.11);
		Check(minimumFuseBarrel.HasExploded,
			"barrel explodes after its minimum fuse");

		ExplosiveBarrel chainBarrel = barrelScene.Instantiate<ExplosiveBarrel>();
		chainBarrel.Position = new Vector2(700f, 400f);
		AddChild(chainBarrel);
		Node2D chainSource = new() { Position = new Vector2(650f, 400f) };
		AddChild(chainSource);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Explosion.Detonate(chainSource, 70f, 3, 0f, Vector2.Right);
		Check(!chainBarrel.HasExploded && Mathf.IsEqualApprox(chainBarrel.FuseLeft, 0.1f),
			"an explosion starts a nearby barrel's minimum fuse");
		chainBarrel._PhysicsProcess(0.11);
		Check(chainBarrel.HasExploded,
			"chain explosions keep their short delay");

		string[] fullRun = Main.GetRooms(sandbox: false);
		Check(fullRun.Length == 7 && fullRun[^1].EndsWith("Geryon.tscn"),
			"the normal run contains the safe start, five combat rooms and the boss");
		Check(Main.GetRooms(sandbox: true) is { Length: 1 } sandboxRooms
			&& sandboxRooms[0].EndsWith("Sandbox.tscn"),
			"the sandbox flag selects only the test room");

		Room chargerRoom = GD.Load<PackedScene>("res://scenes/rooms/Room06.tscn").Instantiate<Room>();
		AddChild(chargerRoom);
		foreach (Node child in chargerRoom.GetChildren())
		{
			if (child is Hazard { Periodic: true } rain)
			{
				rain._PhysicsProcess(rain.RestSeconds + 0.01f);
				rain._PhysicsProcess(rain.TelegraphSeconds + 0.01f);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		Vector2 playerAcrossRoom = new(250f, 270f);
		bool allChargersCanApproach = true;
		foreach (Node child in chargerRoom.GetNode("Enemies").GetChildren())
		{
			if (child is Charger charger)
			{
				int pathLength = chargerRoom.Navigation
					.FindPath(charger.GlobalPosition, playerAcrossRoom).Length;
				allChargersCanApproach &= pathLength > 0;
			}
		}
		Check(allChargersCanApproach,
			"every charger can path across room six while all rain areas warn");
		chargerRoom.QueueFree();

		Room lavaRoom = GD.Load<PackedScene>("res://scenes/rooms/Room08.tscn").Instantiate<Room>();
		AddChild(lavaRoom);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Vector2 lavaRoomPlayer = new(50f, 235f);
		PhysicsPointQueryParameters2D hazardPoint = new()
		{
			CollisionMask = 1,
			CollideWithAreas = true,
			CollideWithBodies = false,
		};
		bool allEnemiesAvoidLava = true;
		foreach (Node wave in lavaRoom.GetNode("Waves").GetChildren())
		{
			foreach (Node child in wave.GetChildren())
			{
				if (child is not SpawnPoint spawn)
				{
					continue;
				}

				Vector2[] path = lavaRoom.Navigation.FindPath(spawn.GlobalPosition, lavaRoomPlayer);
				allEnemiesAvoidLava &= path.Length > 0;
				foreach (Vector2 point in path)
				{
					hazardPoint.Position = point;
					allEnemiesAvoidLava &= lavaRoom.GetWorld2D().DirectSpaceState
						.IntersectPoint(hazardPoint, 1).Count == 0;
				}
			}
		}
		Check(allEnemiesAvoidLava,
			"every room eight spawn can reach the player without crossing lava");
		lavaRoom.QueueFree();

		Door door = GD.Load<PackedScene>("res://scenes/Door.tscn").Instantiate<Door>();
		door.Position = new Vector2(900f, 400f);
		AddChild(door);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(!door.IsOpen && door.GetNode<Sprite2D>("Visual").Texture == door.ClosedTexture
			&& !door.GetNode<CollisionShape2D>("Blocker/CollisionShape2D").Disabled,
			"a locked door shows its closed sprite and blocks passage");
		door.Unlock();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(door.IsOpen && door.GetNode<Sprite2D>("Visual").Texture == door.OpenTexture
			&& door.GetNode<CollisionShape2D>("Blocker/CollisionShape2D").Disabled
			&& door.GetNode<Area2D>("Trigger").Monitoring,
			"an unlocked door shows its open sprite and enables passage");
		door.Lock();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(!door.IsOpen && door.GetNode<Sprite2D>("Visual").Texture == door.ClosedTexture
			&& !door.GetNode<CollisionShape2D>("Blocker/CollisionShape2D").Disabled
			&& !door.GetNode<Area2D>("Trigger").Monitoring,
			"locking a door again restores its sprite and collision");
		door.QueueFree();

		GD.Print(_failures == 0 ? "all checks passed" : $"{_failures} FAILED");
		Callable.From(() => GetTree().Quit(_failures)).CallDeferred();
	}

	private void Check(bool ok, string what)
	{
		GD.Print(ok ? "  ok   " : "  FAIL ", what);
		if (!ok)
		{
			_failures++;
		}
	}
}
