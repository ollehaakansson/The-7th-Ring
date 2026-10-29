using Godot;

namespace The7thRing;

public partial class Chaser : Enemy
{
	[Export] public float Speed { get; set; } = 55f;
	[Export] public int ContactDamage { get; set; } = 1;

	protected override void Act(float delta)
	{
		DealContactDamage(ContactDamage);

		Velocity = DirectionTo(Target!.GlobalPosition) * Speed;
	}
}
