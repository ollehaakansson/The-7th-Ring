using Godot;

namespace The7thRing;

public partial class Main : Node2D
{
	[Export] public string[] Rooms { get; set; } = GetRooms(sandbox: false);

	public int RoomIndex { get; private set; }
	public Room? CurrentRoom { get; private set; }

	private Node2D _roomHost = null!;
	private Player _player = null!;
	private HealthComponent _health = null!;
	private EndScreen _end = null!;
	private AudioStreamPlayer _music = null!;
	private Tween? _musicFade;
	private SoundEffects _sounds = null!;
	private const float MusicVolumeDb = -8f;

	public override void _Ready()
	{
		_sounds = new SoundEffects { Name = "SoundEffects" };
		AddChild(_sounds);
		_player = GetNode<Player>("Player");
		string[] args = OS.GetCmdlineUserArgs();
		if (System.Array.Exists(args, argument => argument == "--sandbox"))
		{
			Rooms = GetRooms(sandbox: true);
		}
		else if (System.Array.Exists(args, argument => argument == "--design"))
		{
			Rooms = DesignRooms;
			WeaponPickup shotgun = GD.Load<PackedScene>("res://scenes/ShotgunPickup.tscn").Instantiate<WeaponPickup>();
			AddChild(shotgun);
			_player.TryCollectWeapon(shotgun, replace: false);
			_player.SwitchWeapon();
		}

		_roomHost = GetNode<Node2D>("RoomHost");
		_health = _player.GetNode<HealthComponent>("HealthComponent");
		_health.Died += OnPlayerDied;

		HUD hud = GetNode<HUD>("HUD");
		hud.Track(_health);
		hud.Player = _player;

		_end = GetNode<EndScreen>("EndScreen");
		_end.Restarted += RestartRun;

		_music = new AudioStreamPlayer { Name = "Music", Bus = "Music", ProcessMode = ProcessModeEnum.Always };
		AddChild(_music);
		EnterRoom(0);
	}

	public override void _ExitTree()
	{
		_musicFade?.Kill();
		_music.Stop();
		_music.Stream = null;
		_sounds.StopAll();
	}

	private static readonly string[] DesignRooms =
	{
		"res://scenes/rooms/design/Geryon.tscn",
	};

	internal static string[] GetRooms(bool sandbox) => sandbox
		? new[] { "res://scenes/rooms/Sandbox.tscn" }
		: new[]
		{
			"res://scenes/rooms/Room01.tscn",
			"res://scenes/rooms/design/Ford.tscn",
			"res://scenes/rooms/design/Island.tscn",
			"res://scenes/rooms/design/Spillway.tscn",
			"res://scenes/rooms/design/Crossfire.tscn",
			"res://scenes/rooms/design/Furnace.tscn",
			"res://scenes/rooms/design/Geryon.tscn",
		};

	public void EnterRoom(int index)
	{
		_sounds.StopAll();
		if (CurrentRoom is not null)
		{
			// Free first, so the room knows it is being torn down and not cleared.
			CurrentRoom.QueueFree();
			_roomHost.RemoveChild(CurrentRoom);
			CurrentRoom = null;
		}

		if (index >= Rooms.Length)
		{
			FadeMusic();
			SoundEffects.Play(this, Sound.Victory);
			_end.Show("THE RING IS CLEARED");
			return;
		}

		RoomIndex = index;
		CurrentRoom = GD.Load<PackedScene>(Rooms[index]).Instantiate<Room>();
		PlayMusic(CurrentRoom.Music, CurrentRoom.MusicStartSeconds);
		_roomHost.AddChild(CurrentRoom);
		CurrentRoom.Cleared += FadeMusic;
		CurrentRoom.Exited += OnRoomExited;
		_player.GlobalPosition = CurrentRoom.EntryPosition;
	}

	private void PlayMusic(AudioStreamMP3? stream, float startSeconds)
	{
		_musicFade?.Kill();
		_music.Stop();
		_music.Stream = stream;
		_music.VolumeDb = MusicVolumeDb;
		if (stream is not null)
		{
			stream.Loop = true;
			stream.LoopOffset = startSeconds;
			_music.Play(startSeconds);
		}
	}

	private void FadeMusic()
	{
		if (!_music.Playing || (_musicFade?.IsRunning() ?? false)) return;
		_musicFade = _music.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
		_musicFade.TweenProperty(_music, "volume_db", -60f, 1.5f);
		_musicFade.TweenCallback(Callable.From(_music.Stop));
	}

	public void RestartRun()
	{
		_health.Reset();
		EnterRoom(0);
	}

	private void OnRoomExited()
	{
		// Deferred: this comes in mid physics step, from the room about to be freed.
		int next = RoomIndex + 1;
		Callable.From(() => EnterRoom(next)).CallDeferred();
	}

	private void OnPlayerDied()
	{
		FadeMusic();
		// Deferred: can't pause the tree mid physics step.
		Callable.From(() => _end.Show("YOU DIED")).CallDeferred();
	}
}
