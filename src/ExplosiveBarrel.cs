using Godot;

namespace The7thRing;

public partial class ExplosiveBarrel : StaticBody2D
{
	[Export] public int InstantDamageThreshold { get; set; } = 3;
	[Export] public float SecondsPerRemainingHit { get; set; } = 1f;
	[Export] public float MinimumFuseDelay { get; set; } = 0.1f;
	[Export] public float ExplosionRadius { get; set; } = 70f;
	[Export] public int ExplosionDamage { get; set; } = 3;
	[Export] public float ExplosionKnockback { get; set; } = 55f;

	private int _damageTaken;
	private float _fuseLeft = -1f;
	private float _flashPhase;
	private bool _exploded;
	private ShaderMaterial _flash = null!;

	internal float FuseLeft => _fuseLeft;
	internal bool HasExploded => _exploded;

	public override void _Ready()
	{
		_flash = (ShaderMaterial)GetNode<Sprite2D>("Body").Material;
		HealthComponent health = GetNode<HealthComponent>("HealthComponent");
		health.Damaged += OnDamaged;
		health.Died += ArmMinimumFuse;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_fuseLeft < 0f || _exploded)
		{
			return;
		}

		_fuseLeft -= (float)delta;
		float flashesPerSecond = 2f + _damageTaken + 4f * (1f - Mathf.Clamp(_fuseLeft, 0f, 1f));
		_flashPhase += (float)delta * flashesPerSecond;
		bool bright = ((int)(_flashPhase * 2f) & 1) == 0;
		_flash.SetShaderParameter("flash_strength", bright ? 0.9f : 0f);

		if (_fuseLeft <= 0f)
		{
			Detonate();
		}
	}

	private void OnDamaged(int amount)
	{
		_damageTaken += amount;
		float delay = _damageTaken >= InstantDamageThreshold
			? MinimumFuseDelay
			: (InstantDamageThreshold - _damageTaken) * SecondsPerRemainingHit;
		_fuseLeft = _fuseLeft < 0f ? delay : Mathf.Min(_fuseLeft, delay);
	}

	private void ArmMinimumFuse()
	{
		_fuseLeft = _fuseLeft < 0f ? MinimumFuseDelay : Mathf.Min(_fuseLeft, MinimumFuseDelay);
	}

	private void Detonate()
	{
		if (_exploded)
		{
			return;
		}

		_exploded = true;
		Explosion.Detonate(this, ExplosionRadius, ExplosionDamage, ExplosionKnockback, Vector2.Right);
		QueueFree();
	}
}
