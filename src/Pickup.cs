using Godot;

namespace The7thRing;

public partial class Pickup : Area2D
{
	[Export] public int Health { get; set; }
	[Export] public int Armor { get; set; }

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.GetNodeOrNull<HealthComponent>("HealthComponent") is not { } health)
		{
			return;
		}

		// Single | so both get applied even if the first one was refused.
		bool taken = health.Heal(Health) | health.AddArmor(Armor);

		if (taken)
		{
			SoundEffects.Play(this, Armor > 0 ? Sound.Armor : Sound.Health);
			QueueFree();
		}
	}
}
