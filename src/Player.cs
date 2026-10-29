using Godot;
using System.Collections.Generic;

namespace The7thRing;

public partial class Player : CharacterBody2D
{
	internal static readonly Color DodgeColour = new(0.35f, 0.75f, 1f);

	[Export] public float Speed { get; set; } = 130f;
	[Export] public float Acceleration { get; set; } = 1600f;
	[Export] public float DodgeSpeed { get; set; } = 250f;
	[Export] public float DodgeSeconds { get; set; } = 0.07f;

	// 0.033 is two physics frames at 60 Hz.
	[Export] public float DodgeInvulnerableSeconds { get; set; } = 0.033f;

	[Export] public float DodgeCooldown { get; set; } = 0.5f;
	[Export] public PackedScene StartingWeaponScene { get; set; } = null!;
	[Export] public PackedScene WeaponPickupScene { get; set; } = null!;

	private Node2D _weaponPivot = null!;
	private HealthComponent _health = null!;
	private readonly List<Weapon> _weapons = new();
	private readonly List<PackedScene> _weaponScenes = new();
	private int _activeWeapon;
	private Vector2 _dodgeDirection;
	private Vector2 _dodgeStart;
	private Line2D? _dodgeTrail;
	private Vector2 _shove;
	private float _dodgeLeft;
	private float _dodgeCooldown;

	internal int WeaponCount => _weapons.Count;
	internal string ActiveWeaponPath => _weaponScenes[_activeWeapon].ResourcePath;
	internal Weapon ActiveWeapon => _weapons[_activeWeapon];

	public override void _Ready()
	{
		_weaponPivot = GetNode<Node2D>("WeaponPivot");
		_health = GetNode<HealthComponent>("HealthComponent");
		_weapons.Add(GetNode<Weapon>("WeaponPivot/Weapon"));
		_weaponScenes.Add(StartingWeaponScene);
	}

	public override void _PhysicsProcess(double delta)
	{
		_dodgeCooldown -= (float)delta;
		bool wasDodging = _dodgeLeft > 0f;

		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (_dodgeLeft <= 0f && _dodgeCooldown <= 0f && Input.IsActionJustPressed("dodge"))
		{
			StartDodge(direction);
		}

		if (_weapons.Count == 2 && Input.IsActionJustPressed("switch_weapon"))
		{
			SwitchWeapon();
		}

		if (_dodgeLeft > 0f)
		{
			_dodgeLeft -= (float)delta;
			Velocity = _dodgeDirection * DodgeSpeed;
		}
		else
		{
			Velocity = Velocity.MoveToward(direction * Speed, Acceleration * (float)delta);
		}

		Modulate = wasDodging ? DodgeColour : Colors.White;

		if (_shove != Vector2.Zero)
		{
			MoveAndCollide(_shove);
			_shove = Vector2.Zero;
		}

		MoveAndSlide();
		if (wasDodging && IsInstanceValid(_dodgeTrail))
		{
			_dodgeTrail!.SetPointPosition(1, GlobalPosition - _dodgeStart);
		}

		_weaponPivot.LookAt(GetGlobalMousePosition());

		if (GetNodeOrNull("Body") is AnimatedSprite2D body)
		{
			body.Play(Velocity.LengthSquared() > 1f ? "run" : "idle");
			body.FlipH = GetGlobalMousePosition().X < GlobalPosition.X;
		}
	}

	public void Shove(Vector2 motion)
	{
		_shove += motion;
	}

	internal void SwitchWeapon()
	{
		if (_weapons.Count < 2)
		{
			return;
		}

		SoundEffects.Play(this, Sound.Switch);
		_activeWeapon = 1 - _activeWeapon;
		ShowActiveWeapon();
	}

	public bool TryCollectWeapon(WeaponPickup pickup, bool replace)
	{
		if (pickup.WeaponScene is null || HasWeapon(pickup.WeaponScene))
		{
			return false;
		}

		if (_weapons.Count < 2)
		{
			AddWeapon(pickup.WeaponScene, active: false);
			SoundEffects.Play(this, Sound.Weapon);
			pickup.QueueFree();
			return true;
		}

		if (!replace)
		{
			return false;
		}

		Weapon oldWeapon = _weapons[_activeWeapon];
		PackedScene oldScene = _weaponScenes[_activeWeapon];

		WeaponPickup dropped = WeaponPickupScene.Instantiate<WeaponPickup>();
		dropped.WeaponScene = oldScene;
		pickup.GetParent().AddChild(dropped);
		dropped.GlobalPosition = pickup.GlobalPosition;

		oldWeapon.QueueFree();
		Weapon replacement = pickup.WeaponScene.Instantiate<Weapon>();
		_weaponPivot.AddChild(replacement);
		_weapons[_activeWeapon] = replacement;
		_weaponScenes[_activeWeapon] = pickup.WeaponScene;
		ShowActiveWeapon();
		SoundEffects.Play(this, Sound.Weapon);
		pickup.QueueFree();
		return true;
	}

	private void AddWeapon(PackedScene scene, bool active)
	{
		Weapon weapon = scene.Instantiate<Weapon>();
		_weaponPivot.AddChild(weapon);
		_weapons.Add(weapon);
		_weaponScenes.Add(scene);

		if (active)
		{
			_activeWeapon = _weapons.Count - 1;
		}

		ShowActiveWeapon();
	}

	internal bool HasWeapon(PackedScene scene)
	{
		foreach (PackedScene held in _weaponScenes)
		{
			if (held.ResourcePath == scene.ResourcePath)
			{
				return true;
			}
		}

		return false;
	}

	private void ShowActiveWeapon()
	{
		for (int i = 0; i < _weapons.Count; i++)
		{
			bool active = i == _activeWeapon;
			_weapons[i].Visible = active;
			_weapons[i].ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		}
	}

	internal void StartDodge(Vector2 direction)
	{
		SoundEffects.Play(this, Sound.Dodge);
		_dodgeDirection = direction != Vector2.Zero
			? direction
			: (GlobalPosition - GetGlobalMousePosition()).Normalized();

		_dodgeLeft = DodgeSeconds;
		_dodgeCooldown = DodgeCooldown;
		_health.GrantInvulnerability(DodgeInvulnerableSeconds);
		_dodgeStart = GlobalPosition;
		_dodgeTrail = new Line2D
		{
			Name = "DodgeTrail",
			Width = 5f,
			DefaultColor = new Color(DodgeColour.R, DodgeColour.G, DodgeColour.B, 0.3f),
			Antialiased = true,
			ZIndex = -1,
			Points = new[] { Vector2.Zero, Vector2.Zero },
		};
		GetTree().CurrentScene.AddChild(_dodgeTrail);
		_dodgeTrail.GlobalPosition = _dodgeStart;
		Tween fade = _dodgeTrail.CreateTween();
		fade.TweenProperty(_dodgeTrail, "modulate:a", 0f, DodgeSeconds + 0.12f);
		fade.TweenCallback(Callable.From(_dodgeTrail.QueueFree));
	}
}
