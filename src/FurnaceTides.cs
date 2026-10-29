using Godot;

namespace The7thRing;

public partial class FurnaceTides : Area2D
{
	private const float WarningSeconds = 1.2f;
	private const float ClosedSeconds = 4.8f;

	private Room _room = null!;
	private Bridge[] _bridges = System.Array.Empty<Bridge>();
	private Bridge _exit = null!;
	private Timer _timer = null!;
	private Tween? _pulse;
	private int _closed = -1;
	private int _next;
	private bool _warning;
	private bool _started;
	private bool _finished;

	public override void _Ready()
	{
		_room = GetParent<Room>();
		_bridges = new[]
		{
			_room.GetNode<Bridge>("NorthCrossing"),
			_room.GetNode<Bridge>("SouthCrossing"),
			_room.GetNode<Bridge>("EastCrossing"),
		};
		_exit = _room.GetNode<Bridge>("ExitCauseway");
		_exit.Flood(true);
		_exit.Modulate = new Color(1f, 1f, 1f, 0f);

		_timer = new Timer { OneShot = true };
		_timer.Timeout += Advance;
		AddChild(_timer);
		_room.Cleared += Finish;
		BodyEntered += body =>
		{
			if (body is Player && !_started && !_finished)
			{
				_started = true;
				Callable.From(Start).CallDeferred();
			}
		};
	}

	private void Start()
	{
		_room.StartWaves();
		if (!_finished)
		{
			Warn();
		}
	}

	private void Warn()
	{
		_warning = true;
		Bridge bridge = _bridges[_next];
		_pulse = bridge.CreateTween().SetLoops(3);
		_pulse.TweenProperty(bridge, "modulate", new Color(1f, 0.2f, 0.1f), WarningSeconds / 6f);
		_pulse.TweenProperty(bridge, "modulate", Colors.White, WarningSeconds / 6f);
		_timer.Start(WarningSeconds);
	}

	private void Advance()
	{
		if (!_warning)
		{
			Warn();
			return;
		}

		_pulse?.Kill();
		// Reopen the old crossing first, so the three platforms always stay connected.
		if (_closed >= 0)
		{
			Open(_bridges[_closed]);
		}

		Bridge next = _bridges[_next];
		next.Modulate = Colors.White;
		next.Flood(true);
		_closed = _next;
		_next = (_next + 1) % _bridges.Length;
		_warning = false;
		_timer.Start(ClosedSeconds);
	}

	private static void Open(Bridge bridge)
	{
		bridge.Modulate = new Color(1f, 1f, 1f, bridge.Modulate.A);
		bridge.Flood(false);
	}

	private void Finish()
	{
		_finished = true;
		_timer.Stop();
		_pulse?.Kill();
		foreach (Bridge bridge in _bridges)
		{
			Open(bridge);
		}
		Open(_exit);
	}
}
