using Godot;

namespace The7thRing;

public partial class Room : Node2D
{
	[Signal] public delegate void ClearedEventHandler();
	[Signal] public delegate void ExitedEventHandler();

	[Export] public AudioStreamMP3? Music { get; set; }
	[Export] public float MusicStartSeconds { get; set; }

	[Export] public bool WavesWaitForAmbush { get; set; }

	public bool IsCleared => _cleared;

	public Vector2 EntryPosition => GetNode<Marker2D>("EntryPoint").GlobalPosition;

	public RoomNavigation Navigation =>
		_navigation ??= new RoomNavigation(GetWorld2D(), GetViewportRect().Size);

	private RoomNavigation? _navigation;
	private Door _door = null!;
	private Node2D _enemies = null!;
	private Node2D? _waves;
	private int _nextWave;
	private int _alive;
	private int _pending;
	private bool _cleared;

	public override void _Ready()
	{
		_door = GetNode<Door>("Door");
		_door.PlayerEntered += () => EmitSignal(SignalName.Exited);

		_enemies = GetNode<Node2D>("Enemies");

		_enemies.ChildEnteredTree += OnEnemyArrived;

		foreach (Node child in _enemies.GetChildren())
		{
			OnEnemyArrived(child);
		}

		_waves = GetNodeOrNull<Node2D>("Waves");
		if (!WavesWaitForAmbush)
		{
			StartWaves();
		}
	}

	internal void StartWaves()
	{
		if (_nextWave == 0)
		{
			SpawnNextWave();
		}

		if (_alive == 0 && _pending == 0)
		{
			Clear();
		}
	}

	private void SpawnNextWave()
	{
		if (_waves is null || _nextWave >= _waves.GetChildCount())
		{
			return;
		}

		Node wave = _waves.GetChild(_nextWave);
		_nextWave++;
		Spawn(wave);
	}

	internal void Spawn(Node points)
	{
		foreach (Node child in points.GetChildren())
		{
			if (child is not SpawnPoint point)
			{
				continue;
			}

			if (point.Pickup is not null)
			{
				Pickup pickup = point.Pickup.Instantiate<Pickup>();
				AddChild(pickup);
				pickup.GlobalPosition = point.GlobalPosition;
			}

			if (point.Enemy is null)
			{
				continue;
			}

			PackedScene scene = point.Enemy;
			Vector2 at = point.GlobalPosition;
			if (point.Delay <= 0f)
			{
				SpawnAt(scene, at);
				continue;
			}

			// Counted while waiting, so killing the early ones cannot clear the room before the rest arrive.
			_pending++;
			Reopen();
			GetTree().CreateTimer(point.Delay, processAlways: false).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				_pending--;
				if (IsInsideTree() && !IsQueuedForDeletion())
				{
					SpawnAt(scene, at);
				}
			};
		}
	}

	private void SpawnAt(PackedScene scene, Vector2 at)
	{
		Node2D enemy = scene.Instantiate<Node2D>();
		_enemies.AddChild(enemy);
		enemy.GlobalPosition = at;
	}

	private void OnEnemyArrived(Node enemy)
	{
		_alive++;
		enemy.TreeExited += OnEnemyGone;

		Reopen();
	}

	private void Reopen()
	{
		if (_cleared)
		{
			_cleared = false;
			_door.Lock();
		}
	}

	private void OnEnemyGone()
	{
		// Room is being freed, that isn't the player clearing it.
		if (IsQueuedForDeletion())
		{
			return;
		}

		_alive--;
		if (_alive <= 0)
		{
			// Deferred: the last enemy is still leaving the tree.
			Callable.From(AdvanceWave).CallDeferred();
		}
	}

	private void AdvanceWave()
	{
		if (_alive > 0 || _pending > 0 || IsQueuedForDeletion() || !IsInsideTree())
		{
			return;
		}

		SpawnNextWave();

		if (_alive == 0 && _pending == 0)
		{
			Clear();
		}
	}

	private void Clear()
	{
		if (_cleared)
		{
			return;
		}

		if (_nextWave > 0) SoundEffects.Play(this, Sound.Clear);
		_cleared = true;
		if (GetNodeOrNull<Node2D>("ClearSupplies") is { } supplies) Spawn(supplies);
		_door.Unlock();
		EmitSignal(SignalName.Cleared);
	}
}
