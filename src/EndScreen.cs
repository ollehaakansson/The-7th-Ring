using Godot;

namespace The7thRing;

// Runs with ProcessMode Always, otherwise the key that closes it would be paused too.
public partial class EndScreen : CanvasLayer
{
	[Signal] public delegate void RestartedEventHandler();

	private Label _headline = null!;

	public override void _Ready()
	{
		_headline = GetNode<Label>("Center/Lines/Headline");
		Visible = false;
	}

	public void Show(string headline)
	{
		_headline.Text = headline;
		Visible = true;
		GetTree().Paused = true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || !@event.IsActionPressed("ui_accept"))
		{
			return;
		}

		Visible = false;
		GetTree().Paused = false;
		GetViewport().SetInputAsHandled();

		EmitSignal(SignalName.Restarted);
	}
}
