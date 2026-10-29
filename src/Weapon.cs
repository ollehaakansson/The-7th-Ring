using Godot;

namespace The7thRing;

public partial class Weapon : Node2D
{
	[Export] public string DisplayName { get; set; } = "PISTOL";
	[Export] public PackedScene BulletScene { get; set; } = null!;
	[Export] public float ShotsPerSecond { get; set; } = 5f;

	// Degrees either way.
	[Export(PropertyHint.Range, "0,45,0.5")] public float SpreadDegrees { get; set; } = 2f;

	[Export] public float BulletKnockback { get; set; } = 12f;
	[Export] public int ProjectilesPerShot { get; set; } = 1;
	[Export] public float Recoil { get; set; }
	[Export] public int MagazineSize { get; set; } = 12;
	[Export] public float ReloadSeconds { get; set; } = 1f;
	public Texture2D Texture => GetNode<Sprite2D>("Barrel").Texture;

	private Marker2D _muzzle = null!;
	private float _cooldown;
	private float _reloadLeft;

	internal int Loaded { get; private set; }
	internal bool Reloading => _reloadLeft > 0f;

	public override void _Ready()
	{
		_muzzle = GetNode<Marker2D>("Muzzle");
		Loaded = MagazineSize;
	}

	public override void _PhysicsProcess(double delta)
	{
		Scale = new Vector2(1f, Mathf.Cos(GlobalRotation) < 0f ? -1f : 1f);
		_cooldown -= (float)delta;

		if (Reloading)
		{
			_reloadLeft -= (float)delta;
			if (!Reloading)
			{
				Loaded = MagazineSize;
			}

			return;
		}

		if (Input.IsActionJustPressed("reload") && Loaded < MagazineSize)
		{
			Reload();
			return;
		}

		if (_cooldown <= 0f && Input.IsActionPressed("fire"))
		{
			Fire();
		}
	}

	internal void Fire()
	{
		SoundEffects.Play(this, ShotSound());
		_cooldown = 1f / ShotsPerSecond;
		Loaded--;

		int count = Mathf.Max(ProjectilesPerShot, 1);
		for (int i = 0; i < count; i++)
		{
			float spreadDegrees = count == 1
				? (float)GD.RandRange(-SpreadDegrees, SpreadDegrees)
				: Mathf.Lerp(-SpreadDegrees, SpreadDegrees, (float)i / (count - 1));

			Bullet bullet = BulletScene.Instantiate<Bullet>();
			bullet.Knockback = BulletKnockback;
			// Add to the scene, or the bullet would turn with the aim.
			GetTree().CurrentScene.AddChild(bullet);
			bullet.GlobalPosition = _muzzle.GlobalPosition;
			bullet.GlobalRotation = GlobalRotation + Mathf.DegToRad(spreadDegrees);
		}

		if (Recoil > 0f && GetParent()?.GetParent() is Player player)
		{
			player.Shove(-Vector2.Right.Rotated(GlobalRotation) * Recoil);
		}

		if (Loaded <= 0)
		{
			Reload();
		}
	}

	private Sound ShotSound() => BulletScene.ResourcePath.EndsWith("Rocket.tscn")
		? Sound.Rocket : ProjectilesPerShot > 1 ? Sound.Shotgun : Sound.Pistol;

	internal void Reload()
	{
		SoundEffects.Play(this, Sound.Reload);
		_reloadLeft = ReloadSeconds;
	}
}
