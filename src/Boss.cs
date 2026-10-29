using Godot;

namespace The7thRing;

public partial class Boss : Enemy
{
	private static readonly Color VolleyColour = new(1f, 0.85f, 0.3f);
	private static readonly Color ChargeColour = new(1f, 0.5f, 0.25f);
	private static readonly Color SummonColour = new(0.65f, 0.4f, 0.9f);
	private static readonly Color DazedColour = new(0.55f, 0.55f, 0.6f);

	private enum Phase
	{
		Rest,
		WindUp,
		Charge,
		Dazed,
	}

	private enum Attack
	{
		Volley,
		Summon,
		Charge,
	}

	private const float MuzzleOffset = 22f;

	private const float AddClearance = 18f;

	[Export] public PackedScene BulletScene { get; set; } = null!;
	[Export] public PackedScene AddScene { get; set; } = null!;
	[Export] public PackedScene[] AddScenes { get; set; } = System.Array.Empty<PackedScene>();
	[Export] public int MaxAdds { get; set; } = 16;
	[Export] public float SummonCooldown { get; set; } = 6f;
	[Export] public int AddsPerSummon { get; set; } = 4;
	[Export] public float AddSpawnRadius { get; set; } = 46f;
	[Export] public float Speed { get; set; } = 32f;
	[Export] public float PreferredRange { get; set; } = 200f;

	[Export] public float RangeTolerance { get; set; } = 30f;

	[Export] public float RestSeconds { get; set; } = 1.3f;

	[Export] public float WindUpSeconds { get; set; } = 0.85f;

	[Export] public int VolleyShots { get; set; } = 5;
	[Export] public float VolleySpreadDegrees { get; set; } = 44f;
	[Export] public float ChargeSpeed { get; set; } = 235f;

	[Export] public float MaxChargeSeconds { get; set; } = 3f;

	[Export] public float DazedSeconds { get; set; } = 1.5f;
	[Export] public int ContactDamage { get; set; } = 1;

	protected override Color Tint => _phase switch
	{
		Phase.WindUp => Colors.White.Lerp(AttackColour, 1f - (_timer / WindUpSeconds)),
		Phase.Charge => ChargeColour,
		Phase.Dazed => DazedColour,
		_ => Colors.White,
	};

	private Color AttackColour => _attack switch
	{
		Attack.Volley => VolleyColour,
		Attack.Summon => SummonColour,
		_ => ChargeColour,
	};

	private Phase _phase = Phase.Rest;
	private Attack _attack = Attack.Volley;
	private float _timer;
	private Vector2 _chargeDirection;
	private float _summonLeft;
	private int _nextAdd;

	public override void _Ready()
	{
		base._Ready();
		_timer = RestSeconds;
	}

	protected override void Act(float delta)
	{
		_summonLeft -= delta;
		DealContactDamage(ContactDamage);

		switch (_phase)
		{
			case Phase.Rest:
				Drift();
				// Only the volley waits for line of sight, so hiding can't stall the fight.
				if (Advance(delta) && (_attack != Attack.Volley || HasLineOfSight()))
				{
					Velocity = Vector2.Zero;
					Enter(Phase.WindUp, WindUpSeconds);
				}
				break;

			case Phase.WindUp:
				Velocity = Vector2.Zero;
				if (Advance(delta))
				{
					Unleash();
				}
				break;

			case Phase.Charge:
				Velocity = _chargeDirection * ChargeSpeed;
				if (IsOnWall() || Advance(delta))
				{
					Velocity = Vector2.Zero;
					Enter(Phase.Dazed, DazedSeconds);
				}
				break;

			case Phase.Dazed:
				Velocity = Vector2.Zero;
				if (Advance(delta))
				{
					Rest();
				}
				break;
		}
	}

	private void Unleash()
	{
		switch (_attack)
		{
			case Attack.Volley:
				FireVolley();
				Rest();
				break;

			case Attack.Summon:
				Summon();
				_summonLeft = SummonCooldown;
				Rest();
				break;

			case Attack.Charge:
				_chargeDirection = (Target!.GlobalPosition - GlobalPosition).Normalized();
				Enter(Phase.Charge, MaxChargeSeconds);
				break;
		}
	}

	private void Rest()
	{
		_attack = _attack switch
		{
			Attack.Volley when _summonLeft <= 0f && AddsPerSummon > 0 && CountAdds() < MaxAdds => Attack.Summon,
			Attack.Volley or Attack.Summon => Attack.Charge,
			_ => Attack.Volley,
		};

		Enter(Phase.Rest, RestSeconds);
	}

	private void Drift()
	{
		Vector2 toPlayer = Target!.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();

		if (!HasLineOfSight())
		{
			Velocity = DirectionTo(Target.GlobalPosition) * Speed;
			return;
		}

		Vector2 direction = Vector2.Zero;
		if (distance > PreferredRange + RangeTolerance)
		{
			direction = toPlayer / distance;
		}
		else if (distance < PreferredRange - RangeTolerance)
		{
			direction = -toPlayer / distance;
		}

		Velocity = direction * Speed;
	}

	private void FireVolley()
	{
		SoundEffects.Play(this, Sound.BossVolley);
		float aim = (Target!.GlobalPosition - GlobalPosition).Angle();
		float spread = Mathf.DegToRad(VolleySpreadDegrees);

		for (int i = 0; i < VolleyShots; i++)
		{
			float offset = VolleyShots == 1 ? 0f : (((float)i / (VolleyShots - 1)) - 0.5f) * spread;
			float angle = aim + offset;

			Bullet bullet = BulletScene.Instantiate<Bullet>();
			GetTree().CurrentScene.AddChild(bullet);
			bullet.GlobalPosition = GlobalPosition + (Vector2.Right.Rotated(angle) * MuzzleOffset);
			bullet.GlobalRotation = angle;
		}
	}

	private int CountAdds()
	{
		int count = 0;
		foreach (Node child in GetParent().GetChildren())
		{
			if (child is Enemy enemy && enemy != this && !enemy.IsQueuedForDeletion()) count++;
		}
		return count;
	}

	private void Summon()
	{
		SoundEffects.Play(this, Sound.Summon);
		int count = Mathf.Min(AddsPerSummon, Mathf.Max(0, MaxAdds - CountAdds()));
		for (int i = 0; i < count; i++)
		{
			float angle = Mathf.Tau * i / count;
			PackedScene scene = AddScenes.Length > 0 ? AddScenes[_nextAdd++ % AddScenes.Length] : AddScene;
			Node2D add = scene.Instantiate<Node2D>();
			GetParent().AddChild(add);
			add.GlobalPosition = SpawnSpot(GlobalPosition + (Vector2.Right.Rotated(angle) * AddSpawnRadius));
		}
	}

	// Keep summons inside walls so trapped enemies cannot block room completion.
	private Vector2 SpawnSpot(Vector2 wanted)
	{
		using PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
			GlobalPosition, wanted, WorldLayer);
		query.CollideWithAreas = true;

		Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);

		return hit.Count == 0
			? wanted
			: (Vector2)hit["position"] + ((Vector2)hit["normal"] * AddClearance);
	}

	private void Enter(Phase phase, float seconds)
	{
		if (phase == Phase.WindUp && _attack == Attack.Charge) SoundEffects.Play(this, Sound.Charge);
		_phase = phase;
		_timer = seconds;
	}

	private bool Advance(float delta)
	{
		_timer -= delta;
		return _timer <= 0f;
	}
}
