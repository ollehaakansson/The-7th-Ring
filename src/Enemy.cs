using Godot;

namespace The7thRing;

public abstract partial class Enemy : CharacterBody2D
{
	private static readonly Color HitColour = new(1f, 0.35f, 0.35f);

	// Walls and obstacles.
	protected const uint WorldLayer = 1;

	[Export] public float FlashSeconds { get; set; } = 0.08f;
	[Export] public float RepathSeconds { get; set; } = 0.25f;
	[Export] public float EscapeSpeed { get; set; } = 70f;

	protected Node2D Body { get; private set; } = null!;

	protected Node2D? Target { get; private set; }

	protected virtual Color Tint => Colors.White;

	private const float HazardMargin = 3f;

	private const float WaypointRadius = 10f;

	private Area2D? _contact;
	private PhysicsShapeQueryParameters2D _hazardQuery = null!;
	private float _flash;
	private Room? _room;
	private Vector2[] _path = System.Array.Empty<Vector2>();
	private int _step;
	private float _repath;
	private Vector2 _shove;

	public override void _Ready()
	{
		Body = GetNode<Node2D>("Body");
		Target = GetTree().GetFirstNodeInGroup("player") as Node2D;
		_contact = GetNodeOrNull<Area2D>("ContactDamage");
		_room = GetTree().GetFirstNodeInGroup("room") as Room;
		_hazardQuery = new PhysicsShapeQueryParameters2D
		{
			Shape = GetNode<CollisionShape2D>("CollisionShape2D").Shape,
			CollisionMask = WorldLayer,
			CollideWithAreas = true,
			CollideWithBodies = false,
		};

		HealthComponent health = GetNode<HealthComponent>("HealthComponent");
		health.Damaged += OnDamaged;
		health.Died += () => QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_flash > 0f)
		{
			_flash -= (float)delta;
		}

		_repath -= (float)delta;

		if (Target is null)
		{
			Velocity = Vector2.Zero;
		}
		else
		{
			Act((float)delta);
		}

		Body.Modulate = _flash > 0f ? HitColour : Tint;

		if (Body is AnimatedSprite2D sprite)
		{
			sprite.Play(Velocity.LengthSquared() > 1f ? "run" : "idle");
			if (Target is not null)
			{
				sprite.FlipH = Target.GlobalPosition.X < GlobalPosition.X;
			}
		}

		if (InHazard(GlobalPosition))
		{
			Velocity = EscapeDirection() * EscapeSpeed;
		}
		else if (Velocity != Vector2.Zero)
		{
			Velocity = AvoidHazards(Velocity, (float)delta);
		}

		if (_shove != Vector2.Zero)
		{
			MoveAndCollide(_shove);
			_shove = Vector2.Zero;
		}

		MoveAndSlide();
	}

	public void Shove(Vector2 motion)
	{
		_shove += motion;
	}

	protected abstract void Act(float delta);

	protected bool HasLineOfSight() => Target is not null && CanSee(Target.GlobalPosition);

	protected bool CanSee(Vector2 point)
	{
		using PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
			GlobalPosition, point, WorldLayer);

		return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
	}

	private bool InHazard(Vector2 position)
	{
		_hazardQuery.Transform = new Transform2D(0f, position);
		_hazardQuery.Motion = Vector2.Zero;
		return GetWorld2D().DirectSpaceState.IntersectShape(_hazardQuery, 1).Count > 0;
	}

	private bool HazardBetween(Vector2 point)
	{
		_hazardQuery.Transform = new Transform2D(0f, GlobalPosition);
		_hazardQuery.Motion = point - GlobalPosition;
		return GetWorld2D().DirectSpaceState.CastMotion(_hazardQuery)[0] < 1f;
	}

	private Vector2 EscapeDirection()
	{
		for (float distance = 8f; distance <= 256f; distance += 8f)
		{
			for (int i = 0; i < 8; i++)
			{
				Vector2 direction = Vector2.Right.Rotated(i * Mathf.Tau / 8f);
				Vector2 spot = GlobalPosition + direction * distance;
				if (!InHazard(spot) && !TestMove(GlobalTransform, direction * distance))
				{
					return direction;
				}
			}
		}

		return Vector2.Zero;
	}

	private Vector2 AvoidHazards(Vector2 velocity, float delta)
	{
		foreach (Vector2 option in new[] { velocity, new Vector2(velocity.X, 0f), new Vector2(0f, velocity.Y) })
		{
			if (option == Vector2.Zero)
			{
				continue;
			}

			Vector2 ahead = GlobalPosition + option * delta + option.Normalized() * HazardMargin;
			if (!InHazard(ahead))
			{
				return option;
			}
		}

		return Vector2.Zero;
	}

	// Charges and darts skip this on purpose: running into a wall is their tell.
	protected Vector2 DirectionTo(Vector2 destination)
	{
		if (CanSee(destination) && !HazardBetween(destination))
		{
			_path = System.Array.Empty<Vector2>();
			return (destination - GlobalPosition).Normalized();
		}

		if (_repath <= 0f || _step >= _path.Length)
		{
			_path = _room?.Navigation.FindPath(GlobalPosition, destination) ?? System.Array.Empty<Vector2>();
			_step = 0;
			_repath = RepathSeconds;
		}

		while (_step < _path.Length && GlobalPosition.DistanceTo(_path[_step]) < WaypointRadius)
		{
			_step++;
		}

		return _step < _path.Length
			? (_path[_step] - GlobalPosition).Normalized()
			: (destination - GlobalPosition).Normalized();
	}

	protected void DealContactDamage(int amount)
	{
		if (_contact is null)
		{
			return;
		}

		foreach (Node2D body in _contact.GetOverlappingBodies())
		{
			if (body.GetNodeOrNull<HealthComponent>("HealthComponent") is { } health)
			{
				health.TakeDamage(amount);
			}
		}
	}

	private void OnDamaged(int amount)
	{
		_flash = FlashSeconds;
	}
}
