using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class MenuCheck : Node
{
	private int _failures;
	public override async void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		GameShell shell = GD.Load<PackedScene>("res://scenes/GameShell.tscn").Instantiate<GameShell>();
		await Wait(0.01f);
		GetTree().Root.AddChild(shell);
		CanvasLayer menu = shell.GetNode<CanvasLayer>("Menu");
		Button primary = shell.GetNode<Button>("Menu/Center/Buttons/Primary");
		Button quit = shell.GetNode<Button>("Menu/Center/Buttons/Quit");
		AudioStreamPlayer music = shell.GetNode<AudioStreamPlayer>("HomeMusic");
		await Wait(0.15f);
		Check(menu.Visible && GetTree().Paused && primary.Text == "PLAY" && quit.Text == "QUIT", "launch shows home menu with only Play and Quit");
		Check(music.Playing && music.Stream.ResourcePath.EndsWith("Rip & Tear.mp3") && ((AudioStreamMP3)music.Stream).Loop, "home loops boss music");
		await Screenshot("home-menu.png");
		primary.EmitSignal(BaseButton.SignalName.Pressed);
		await Wait(0.15f);
		Main game = (Main)GetTree().CurrentScene;
		Check(!menu.Visible && !GetTree().Paused && !music.Playing && game.CurrentRoom is not null, "Play starts a fresh run and stops menu music");
		Check(GetTree().CurrentScene == game, "projectiles and effects belong to the active run");
		Weapon weapon = game.GetNode<Player>("Player").ActiveWeapon;
		weapon.Reload();
		shell._Input(new InputEventAction { Action = "pause", Pressed = true });
		await Wait(0.3f);
		Check(GetTree().Paused && menu.Visible && primary.Text == "CONTINUE", "Escape pauses and opens Continue/Quit");
		Check(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music")) < -11f, "pause lowers music by twelve dB");
		await Wait(1.2f);
		Check(weapon.Reloading, "gameplay and reload timers freeze while paused");
		await Screenshot("pause-menu.png");
		primary.EmitSignal(BaseButton.SignalName.Pressed);
		await Wait(0.3f);
		Check(!menu.Visible && !GetTree().Paused && Mathf.IsZeroApprox(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))), "Continue resumes and restores music level");
		shell._Input(new InputEventAction { Action = "pause", Pressed = true });
		shell._Input(new InputEventAction { Action = "pause", Pressed = true });
		Check(!GetTree().Paused && !menu.Visible, "Escape also resumes");
		shell._Input(new InputEventAction { Action = "pause", Pressed = true });
		quit.EmitSignal(BaseButton.SignalName.Pressed);
		await Wait(0.2f);
		Check(menu.Visible && primary.Text == "PLAY" && music.Playing && !IsInstanceValid(game), "Quit from pause removes the run and returns to home with boss music");
		primary.EmitSignal(BaseButton.SignalName.Pressed);
		await Wait(0.2f);
		Check(((Main)GetTree().CurrentScene).RoomIndex == 0 && !GetTree().Paused, "Play from home starts again at room one");
		GetTree().CurrentScene = this;
		shell.QueueFree();
		await Wait(0.1f);
		GD.Print(_failures == 0 ? "all menu checks passed" : $"{_failures} menu checks FAILED");
		GetTree().Quit(_failures);
	}
	private async Task Screenshot(string name)
	{
		string dir = OS.GetEnvironment("RING_MENU_SCREENSHOTS");
		if (string.IsNullOrEmpty(dir)) return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(dir, name));
	}
	private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	private void Check(bool ok, string label)
	{
		GD.Print((ok ? "ok " : "FAIL ") + label);
		if (!ok) _failures++;
	}
}
