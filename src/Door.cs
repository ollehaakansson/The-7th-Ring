using Godot;

namespace The7thRing;

public partial class Door : Node2D
{
	[Signal] public delegate void PlayerEnteredEventHandler();

	[Export] public Texture2D ClosedTexture { get; set; } = null!;
	[Export] public Texture2D OpenTexture { get; set; } = null!;

	private Sprite2D _visual = null!;
	private CollisionShape2D _blocker = null!;
	private Area2D _trigger = null!;

	public bool IsOpen { get; private set; }

	public override void _Ready()
	{
		_visual = GetNode<Sprite2D>("Visual");
		_blocker = GetNode<CollisionShape2D>("Blocker/CollisionShape2D");
		_trigger = GetNode<Area2D>("Trigger");
		_trigger.BodyEntered += OnBodyEntered;

		Lock();
	}

	public void Lock()
	{
		IsOpen = false;
		_visual.Texture = ClosedTexture;
		// Can't change collision mid physics step, which is when the room reports clear.
		_blocker.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_trigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
	}

	public void Unlock()
	{
		IsOpen = true;
		_visual.Texture = OpenTexture;
		_blocker.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_trigger.SetDeferred(Area2D.PropertyName.Monitoring, true);
	}

	private void OnBodyEntered(Node2D body)
	{
		// Godot can report a stale overlap on the frame a fresh trigger switches on where the previous room's door was,
		// which skipped straight through rooms that start open. Only count a player actually standing in the doorway.
		if (IsOpen && body is Player && body.GlobalPosition.DistanceTo(_trigger.GlobalPosition) < 48f)
		{
			EmitSignal(SignalName.PlayerEntered);
		}
	}
}
