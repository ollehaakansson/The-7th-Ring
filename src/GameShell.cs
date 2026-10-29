using Godot;

namespace The7thRing;

public partial class GameShell : Node
{
	private Main? _game;
	private Room? _background;
	private CanvasLayer _menu = null!;
	private Label _title = null!;
	private Button _primary = null!;
	private AudioStreamPlayer _homeMusic = null!;
	private bool _paused;
	private Tween? _duck;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_menu = GetNode<CanvasLayer>("Menu");
		_title = GetNode<Label>("Menu/Center/Buttons/Title");
		_primary = GetNode<Button>("Menu/Center/Buttons/Primary");
		_homeMusic = GetNode<AudioStreamPlayer>("HomeMusic");
		((AudioStreamMP3)_homeMusic.Stream).Loop = true;
		((AudioStreamMP3)_homeMusic.Stream).LoopOffset = 0f;
		_primary.Pressed += () => { if (_game is null) StartGame(); else ResumeGame(); };
		GetNode<Button>("Menu/Center/Buttons/Quit").Pressed += () =>
		{
			if (_game is null) GetTree().Quit();
			else ShowHome();
		};
		string[] args = OS.GetCmdlineUserArgs();
		if (System.Array.Exists(args, a => a is "--design" or "--sandbox"))
		{
			_menu.Hide();
			Callable.From(StartGame).CallDeferred();
		}
		else ShowHome();
	}

	public override void _Input(InputEvent @event)
	{
		if (_game is null || _game.GetNode<EndScreen>("EndScreen").Visible
			|| !@event.IsActionPressed("pause") || @event.IsEcho()) return;
		GetViewport().SetInputAsHandled();
		if (_paused) ResumeGame();
		else PauseGame();
	}

	private void ShowHome()
	{
		_duck?.Kill();
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), 0f);
		GetTree().CurrentScene = this;
		if (_game is not null)
		{
			_game.QueueFree();
			GetTree().Root.RemoveChild(_game);
			_game = null;
		}
		_background = GD.Load<PackedScene>("res://scenes/rooms/design/Island.tscn").Instantiate<Room>();
		_background.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(_background);
		_paused = false;
		GetTree().Paused = true;
		_title.Text = "THE 7TH RING";
		_primary.Text = "PLAY";
		_menu.Show();
		_primary.GrabFocus();
		_homeMusic.Play();
	}

	private void StartGame()
	{
		if (_background is not null)
		{
			_background.QueueFree();
			RemoveChild(_background);
			_background = null;
		}
		_homeMusic.Stop();
		_menu.Hide();
		GetTree().Paused = false;
		_game = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
		_game.ProcessMode = ProcessModeEnum.Pausable;
		GetTree().Root.AddChild(_game);
		GetTree().CurrentScene = _game;
	}

	private void PauseGame()
	{
		_paused = true;
		GetTree().Paused = true;
		_game!.GetNode<SoundEffects>("SoundEffects").StopAll();
		_title.Text = "PAUSED";
		_primary.Text = "CONTINUE";
		_menu.Show();
		_primary.GrabFocus();
		SetMusicLevel(-12f);
	}

	private void ResumeGame()
	{
		_paused = false;
		_menu.Hide();
		GetTree().Paused = false;
		SetMusicLevel(0f);
	}

	private void SetMusicLevel(float target)
	{
		_duck?.Kill();
		int bus = AudioServer.GetBusIndex("Music");
		_duck = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
		_duck.TweenMethod(Callable.From<float>(db => AudioServer.SetBusVolumeDb(bus, db)),
			AudioServer.GetBusVolumeDb(bus), target, 0.2f);
	}

	public override void _ExitTree()
	{
		_duck?.Kill();
		if (IsInstanceValid(_game)) _game!.QueueFree();
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), 0f);
		GetTree().Paused = false;
	}
}
