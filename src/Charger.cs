using Godot;

namespace The7thRing;

public partial class Charger : Enemy
{
	private static readonly Color WindUpColour = new(1f, 0.5f, 0.25f);
	private static readonly Color DazedColour = new(0.55f, 0.55f, 0.6f);

	private enum Phase
	{
		Approach,
		WindUp,
		Charge,
		Dazed,
	}

	[Export] public float Speed { get; set; } = 38f;
	[Export] public float ChargeSpeed { get; set; } = 260f;
	[Export] public float ChargeRange { get; set; } = 210f;
	[Export] public float WindUpSeconds { get; set; } = 0.75f;

	[Export] public float MaxChargeSeconds { get; set; } = 3f;

	[Export] public float DazedSeconds { get; set; } = 1.4f;
	[Export] public int ContactDamage { get; set; } = 1;

	protected override Color Tint => _phase switch
	{
		Phase.WindUp => Colors.White.Lerp(WindUpColour, 1f - (_timer / WindUpSeconds)),
		Phase.Charge => WindUpColour,
		Phase.Dazed => DazedColour,
		_ => Colors.White,
	};

	private Phase _phase = Phase.Approach;
	private float _timer;
	private Vector2 _chargeDirection;

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (_phase != Phase.Charge)
		{
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is CollisionObject2D wall
				&& (wall.CollisionLayer & WorldLayer) != 0)
			{
				Velocity = Vector2.Zero;
				Enter(Phase.Dazed, DazedSeconds);
				// Player only: barrels must not trigger friendly-fire chain explosions.
				Explosion.Detonate(this, 48f, 1, 20f, _chargeDirection, collisionMask: 2);
				break;
			}
		}
	}

	protected override void Act(float delta)
	{
		DealContactDamage(ContactDamage);

		switch (_phase)
		{
			case Phase.Approach:
				Vector2 toPlayer = Target!.GlobalPosition - GlobalPosition;
				if (toPlayer.Length() <= ChargeRange)
				{
					Velocity = Vector2.Zero;
					Enter(Phase.WindUp, WindUpSeconds);
					break;
				}

				Velocity = DirectionTo(Target.GlobalPosition) * Speed;
				break;

			case Phase.WindUp:
				Velocity = Vector2.Zero;
				if (Advance(delta))
				{
					_chargeDirection = (Target!.GlobalPosition - GlobalPosition).Normalized();
					Enter(Phase.Charge, MaxChargeSeconds);
				}
				break;

			case Phase.Charge:
				Velocity = _chargeDirection * ChargeSpeed;

				if (Advance(delta))
				{
					Velocity = Vector2.Zero;
					Enter(Phase.Dazed, DazedSeconds);
				}
				break;

			case Phase.Dazed:
				Velocity = Vector2.Zero;
				if (Advance(delta))
				{
					Enter(Phase.Approach, 0f);
				}
				break;
		}
	}

	private void Enter(Phase phase, float seconds)
	{
		if (phase == Phase.WindUp) SoundEffects.Play(this, Sound.Charge);
		_phase = phase;
		_timer = seconds;
	}

	private bool Advance(float delta)
	{
		_timer -= delta;
		return _timer <= 0f;
	}
}
