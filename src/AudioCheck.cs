using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class AudioCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		Main main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
		AddChild(main);
		Player player = main.GetNode<Player>("Player");
		player.SetPhysicsProcess(false);
		HealthComponent health = player.GetNode<HealthComponent>("HealthComponent");
		SoundEffects audio = main.GetNode<SoundEffects>("SoundEffects");
		AudioStreamPlayer[] voices = audio.GetChildren().OfType<AudioStreamPlayer>().ToArray();
		Check(AudioServer.GetBusIndex("Music") > 0 && AudioServer.GetBusIndex("SFX") > 0, "separate music and SFX buses load");
		Check(Mathf.IsEqualApprox(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX")), 0f), "SFX bus is three dB louder");
		foreach (Sound cue in System.Enum.GetValues<Sound>())
		{
			audio.StopAll();
			SoundEffects.Play(player, cue);
			Check(voices.Any(v => v.Playing && v.Stream is not null && v.Stream.GetLength() > 0.05), "effect loads and plays: " + cue);
		}
		audio.StopAll();
		for (int i = 0; i < 200; i++) SoundEffects.Play(player, Sound.Impact);
		Check(voices.Count(v => v.Playing) == 1 && voices.Length == 24, "simultaneous impacts are throttled and the voice pool is bounded");
		audio.StopAll();
		player.ActiveWeapon.Fire();
		Check(voices.Count(v => v.Playing) == 1, "one pistol sound per trigger pull");
		Check(voices.Any(v => v.Playing && Mathf.IsEqualApprox(v.VolumeDb, 0f)), "pistol gets an additional three dB boost");
		audio.StopAll();
		health.GrantInvulnerability(0.1f);
		health.TakeDamage(1);
		Check(!voices.Any(v => v.Playing), "invulnerable hits stay silent");
		await Wait(0.15f);
		health.TakeDamage(1);
		Check(voices.Any(v => v.Playing && v.Stream.ResourcePath.EndsWith("playerhit.wav")), "accepted player damage plays hurt sound");

		main.EnterRoom(2);
		Room room = main.CurrentRoom!;
		Node enemies = room.GetNode("Enemies");
		enemies.ChildEnteredTree += enemy => enemy.ProcessMode = ProcessModeEnum.Disabled;
		Check(room.GetChildren().OfType<Pickup>().Count() == 0, "Island reward waits until combat is cleared");
		room.StartWaves();
		for (int i = 0; i < 100 && !room.IsCleared; i++)
		{
			foreach (Node enemy in enemies.GetChildren()) enemy.GetNode<HealthComponent>("HealthComponent").TakeDamage(1000);
			await Wait(0.1f);
		}
		Pickup? reward = room.GetChildren().OfType<Pickup>().SingleOrDefault();
		Check(room.IsCleared && reward is not null && reward.GlobalPosition == new Vector2(580, 180), "clearing Island spawns one HP pickup at the exit");
		audio.StopAll();
		player.GlobalPosition = new Vector2(580, 180);
		await Wait(0.1f);
		Check(health.Current == health.MaxHealth && !room.GetChildren().OfType<Pickup>().Any(), "Island reward can be reached and collected");
		main.EnterRoom(3);
		Check(main.CurrentRoom!.GetNodeOrNull<Pickup>("PlatformHealth") is not null, "Spillway has additional health on the central platform");
		main.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GD.Print(_failures == 0 ? "all audio and supplies checks passed" : $"{_failures} audio and supplies checks FAILED");
		GetTree().Quit(_failures);
	}

	private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

	private void Check(bool passed, string message)
	{
		GD.Print($"{(passed ? "ok" : "FAIL")} {message}");
		if (!passed) _failures++;
	}
}
