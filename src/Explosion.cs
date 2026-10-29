using Godot;

namespace The7thRing;

public static class Explosion
{
	public static void Detonate(Node2D source, float radius, int damage, float knockback,
		Vector2 fallbackDirection, uint collisionMask = 70, int? playerDamage = null)
	{
		SoundEffects.Play(source, Sound.Explosion);
		ShowRadius(source, radius);

		PhysicsShapeQueryParameters2D query = new()
		{
			Shape = new CircleShape2D { Radius = radius },
			Transform = new Transform2D(0f, source.GlobalPosition),
			CollisionMask = collisionMask,
			CollideWithAreas = false,
			CollideWithBodies = true,
		};

		foreach (Godot.Collections.Dictionary hit in source.GetWorld2D().DirectSpaceState.IntersectShape(query))
		{
			if (hit["collider"].AsGodotObject() is not Node2D body || body == source)
			{
				continue;
			}

			Vector2 direction = (body.GlobalPosition - source.GlobalPosition).Normalized();
			DamageBody(body, body is Player ? playerDamage ?? damage : damage, knockback,
				direction == Vector2.Zero ? fallbackDirection : direction);
		}
	}

	private static void ShowRadius(Node2D source, float radius)
	{
		const int points = 32;
		Vector2[] circle = new Vector2[points];
		for (int i = 0; i < points; i++)
		{
			circle[i] = Vector2.Right.Rotated(Mathf.Tau * i / points) * radius;
		}

		Polygon2D visual = new()
		{
			Name = "ExplosionRadius",
			Color = new Color(1f, 0.2f, 0.03f, 0.4f),
			Polygon = circle,
		};
		source.GetTree().CurrentScene.AddChild(visual);
		visual.GlobalPosition = source.GlobalPosition;

		Tween fade = visual.CreateTween();
		fade.TweenProperty(visual, "modulate:a", 0f, 0.2);
		fade.TweenCallback(Callable.From(visual.QueueFree));
	}

	private static void DamageBody(Node2D body, int damage, float knockback, Vector2 direction)
	{
		body.GetNodeOrNull<HealthComponent>("HealthComponent")?.TakeDamage(damage);

		if (knockback <= 0f)
		{
			return;
		}

		switch (body)
		{
			case Enemy enemy:
				enemy.Shove(direction * knockback);
				break;
			case Player player:
				player.Shove(direction * knockback);
				break;
		}
	}
}
