using Godot;

namespace The7thRing;

public partial class Bullet : Area2D
{
	[Export] public float Speed { get; set; } = 420f;

	[Export] public float Lifetime { get; set; } = 1.5f;

	[Export] public int Damage { get; set; } = 1;
	[Export] public float Knockback { get; set; }

	[Export] public float ExplosionRadius { get; set; }

	private float _age;
	private bool _hit;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		AreaEntered += area =>
		{
			if (area.GetParent() is Enemy enemy)
			{
				Hit(enemy);
			}
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Vector2.Right.Rotated(GlobalRotation) * Speed * (float)delta;

		_age += (float)delta;
		if (_age >= Lifetime)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		Hit(body);
	}

	internal void Hit(Node2D body)
	{
		if (_hit)
		{
			return;
		}
		_hit = true;

		if (ExplosionRadius > 0f)
		{
			Explode();
		}
		else
		{
			DamageBody(body, Vector2.Right.Rotated(GlobalRotation));
			if (body.GetNodeOrNull<HealthComponent>("HealthComponent") is null)
				SoundEffects.Play(this, Sound.Impact);
		}

		QueueFree();
	}

	private void Explode()
	{
		Explosion.Detonate(this, ExplosionRadius, Damage, Knockback,
			Vector2.Right.Rotated(GlobalRotation), playerDamage: 2);
	}

	private void DamageBody(Node2D body, Vector2 direction)
	{
		body.GetNodeOrNull<HealthComponent>("HealthComponent")?.TakeDamage(Damage);

		if (Knockback <= 0f)
		{
			return;
		}

		switch (body)
		{
			case Enemy enemy:
				enemy.Shove(direction * Knockback);
				break;
			case Player player:
				player.Shove(direction * Knockback);
				break;
		}
	}
}
