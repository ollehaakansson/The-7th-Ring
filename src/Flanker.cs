using Godot;

namespace The7thRing;

public partial class Flanker : Enemy
{
	private static readonly Color TellColour = new(1f, 0.95f, 0.8f);

	private enum Phase
	{
		Circle,
		Tell,
		Dart,
		Retreat,
	}

	[Export] public float Speed { get; set; } = 100f;
	[Export] public float DartSpeed { get; set; } = 215f;
	[Export] public float FlankOffset { get; set; } = 70f;
	[Export] public float DartRange { get; set; } = 85f;
	[Export] public float TellSeconds { get; set; } = 0.22f;
	[Export] public float DartSeconds { get; set; } = 0.4f;
	[Export] public float RetreatSeconds { get; set; } = 0.8f;
	[Export] public int ContactDamage { get; set; } = 1;

	protected override Color Tint => _phase switch
	{
		Phase.Tell => Colors.White.Lerp(TellColour, 1f - (_timer / TellSeconds)),
		Phase.Dart => TellColour,
		_ => Colors.White,
	};

	private Phase _phase = Phase.Circle;
	private float _timer;
	private Vector2 _dartDirection;
	private float _side = 1f;

	public override void _Ready()
	{
		base._Ready();
		// Random side, so two flankers do not stack up.
		_side = GD.Randf() < 0.5f ? -1f : 1f;
	}

	protected override void Act(float delta)
	{
		DealContactDamage(ContactDamage);

		switch (_phase)
		{
			case Phase.Circle:
				Circle();
				break;

			case Phase.Tell:
				Velocity = Vector2.Zero;
				if (Advance(delta))
				{
					_dartDirection = (Target!.GlobalPosition - GlobalPosition).Normalized();
					Enter(Phase.Dart, DartSeconds);
				}
				break;

			case Phase.Dart:
				Velocity = _dartDirection * DartSpeed;
				if (Advance(delta))
				{
					_side = -_side;
					Enter(Phase.Retreat, RetreatSeconds);
				}
				break;

			case Phase.Retreat:
				Velocity = (GlobalPosition - Target!.GlobalPosition).Normalized() * Speed;
				if (Advance(delta))
				{
					Enter(Phase.Circle, 0f);
				}
				break;
		}
	}

	private void Circle()
	{
		Vector2 toPlayer = Target!.GlobalPosition - GlobalPosition;

		if (toPlayer.Length() <= DartRange)
		{
			Velocity = Vector2.Zero;
			Enter(Phase.Tell, TellSeconds);
			return;
		}

		Vector2 flank = Target.GlobalPosition + (toPlayer.Orthogonal().Normalized() * _side * FlankOffset);
		Velocity = DirectionTo(flank) * Speed;
	}

	private void Enter(Phase phase, float seconds)
	{
		_phase = phase;
		_timer = seconds;
	}

	private bool Advance(float delta)
	{
		_timer -= delta;
		return _timer <= 0f;
	}
}
