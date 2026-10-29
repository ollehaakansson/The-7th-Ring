using Godot;

namespace The7thRing;

public partial class WeaponPickup : Area2D
{
	[Export] public PackedScene WeaponScene { get; set; } = null!;
	private Label _prompt = null!;
	private float _hoverTime;

	public override void _Ready()
	{
		_prompt = GetNode<Label>("Prompt");
		if (WeaponScene is not null)
		{
			Weapon weapon = WeaponScene.Instantiate<Weapon>();
			GetNode<Sprite2D>("Visual").Texture = weapon.Texture;
			weapon.Free();
		}

		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		_prompt.Visible = false;
		if (IsQueuedForDeletion() || WeaponScene is null) return;
		foreach (Node2D body in GetOverlappingBodies())
		{
			if (body is not Player player || player.WeaponCount < 2 || player.HasWeapon(WeaponScene)) continue;
			_prompt.Visible = true;
			_hoverTime += (float)delta;
			_prompt.Position = new Vector2(-12f, -44f + Mathf.Sin(_hoverTime * 3f) * 2f);
			if (Input.IsActionJustPressed("interact") && player.TryCollectWeapon(this, replace: true))
			{
				_prompt.Visible = false;
				return;
			}
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			player.TryCollectWeapon(this, replace: false);
		}
	}
}
