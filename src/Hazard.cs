using Godot;

namespace The7thRing;

public partial class Hazard : Area2D
{
	public enum Phase
	{
		Rest,
		Telegraph,
		Active,
	}

	[Export] public int Damage { get; set; } = 1;
	[Export] public float DamageInterval { get; set; } = 0.4f;
	[Export] public bool Periodic { get; set; }

	// Waits at rest until Erupt() is called, then telegraphs, goes active and rests again. For hazards driven by
	// something else, like a GeyserCycle. Overrides Periodic.
	[Export] public bool Manual { get; set; }
	[Export] public float RestSeconds { get; set; } = 1f;
	[Export] public float TelegraphSeconds { get; set; } = 0.6f;
	[Export] public float ActiveSeconds { get; set; } = 0.2f;
	[Export] public Color RestColour { get; set; } = new(0.25f, 0.12f, 0.12f, 0.35f);
	[Export] public Color TelegraphColour { get; set; } = new(1f, 0.45f, 0.15f, 0.55f);
	[Export] public Color ActiveColour { get; set; } = new(0.85f, 0.08f, 0.03f, 0.85f);

	public Phase CurrentPhase => _phase;

	public float PhaseProgress => 1f - _phaseLeft / Mathf.Max(PhaseLength(_phase), 0.001f);

	private Polygon2D? _visual;
	private Phase _phase;
	private float _phaseLeft;
	private float _damageLeft;

	public override void _Ready()
	{
		_visual = GetNodeOrNull<Polygon2D>("Visual");
		if (_visual is not null)
		{
			// Two pixels per texel, even when a room stretches the hazard.
			_visual.TextureScale = Scale / 2f;
		}

		Enter(Periodic || Manual ? Phase.Rest : Phase.Active);
	}

	public override void _PhysicsProcess(double delta)
	{
		float elapsed = (float)delta;

		if (_phase == Phase.Active)
		{
			_damageLeft -= elapsed;
			if (_damageLeft <= 0f)
			{
				DamageBodies();
				_damageLeft = Mathf.Max(DamageInterval, 0.01f);
			}
		}

		bool cycling = Manual ? _phase != Phase.Rest : Periodic;
		if (!cycling)
		{
			return;
		}

		_phaseLeft -= elapsed;
		if (_phaseLeft <= 0f)
		{
			Enter(_phase switch
			{
				Phase.Rest => Phase.Telegraph,
				Phase.Telegraph => Phase.Active,
				_ => Phase.Rest,
			});
		}
	}

	public void Erupt()
	{
		if (_phase == Phase.Rest)
		{
			Enter(Phase.Telegraph);
		}
	}

	public float EruptionSeconds => TelegraphSeconds + ActiveSeconds;

	private void Enter(Phase phase)
	{
		_phase = phase;
		_phaseLeft = PhaseLength(phase);
		_visual?.Color = phase switch
		{
			Phase.Rest => RestColour,
			Phase.Telegraph => TelegraphColour,
			_ => ActiveColour,
		};
		// Only on the World layer while dangerous, so enemies path around it then. Bullets ignore areas, so this does not block shots.
		CollisionLayer = phase == Phase.Rest ? 0u : 1u;

		if (phase == Phase.Active)
		{
			if (Periodic || Manual) SoundEffects.Play(this, Sound.Fire);
			_damageLeft = 0f;
		}
	}

	private float PhaseLength(Phase phase) => phase switch
	{
		Phase.Rest => RestSeconds,
		Phase.Telegraph => TelegraphSeconds,
		_ => ActiveSeconds,
	};

	private void DamageBodies()
	{
		foreach (Node2D body in GetOverlappingBodies())
		{
			DamageBody(body);
		}
	}

	internal void DamageBody(Node2D body)
	{
		body.GetNodeOrNull<HealthComponent>("HealthComponent")?.TakeDamage(Damage, ignoreHitImmunity: true);
	}
}
