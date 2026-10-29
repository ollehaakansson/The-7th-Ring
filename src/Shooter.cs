using Godot;

namespace The7thRing;

public partial class Shooter : Enemy
{
	private static readonly Color WindUpColour = new(1f, 0.85f, 0.3f);

	private const float MuzzleOffset = 11f;

	[Export] public PackedScene BulletScene { get; set; } = null!;
	[Export] public float Speed { get; set; } = 45f;
	[Export] public float PreferredRange { get; set; } = 170f;

	[Export] public float RangeTolerance { get; set; } = 25f;

	[Export] public float ShotInterval { get; set; } = 2.2f;
	[Export] public float WindUpSeconds { get; set; } = 0.8f;

	protected override Color Tint =>
		_windUp > 0f ? Colors.White.Lerp(WindUpColour, 1f - (_windUp / WindUpSeconds)) : Colors.White;

	private float _cooldown;
	private float _windUp;

	public override void _Ready()
	{
		base._Ready();
		_cooldown = ShotInterval;
	}

	protected override void Act(float delta)
	{
		if (_windUp > 0f)
		{
			_windUp -= delta;
			Velocity = Vector2.Zero;

			if (_windUp <= 0f)
			{
				Fire();
				_cooldown = ShotInterval;
			}

			return;
		}

		_cooldown -= delta;

		if (_cooldown <= 0f && HasLineOfSight())
		{
			_windUp = WindUpSeconds;
			Velocity = Vector2.Zero;
		}
		else
		{
			Hold();
		}
	}

	private void Hold()
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

	private void Fire()
	{
		SoundEffects.Play(this, Sound.EnemyShot);
		Vector2 direction = (Target!.GlobalPosition - GlobalPosition).Normalized();

		Bullet bullet = BulletScene.Instantiate<Bullet>();
		// Add to the scene so the bullet outlives the shooter.
		GetTree().CurrentScene.AddChild(bullet);
		bullet.GlobalPosition = GlobalPosition + direction * MuzzleOffset;
		bullet.GlobalRotation = direction.Angle();
	}
}
