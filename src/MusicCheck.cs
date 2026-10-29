using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class MusicCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		Main main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
		AddChild(main);
		Player player = main.GetNode<Player>("Player");
		player.SetPhysicsProcess(false);
		AudioStreamPlayer music = main.GetNode<AudioStreamPlayer>("Music");
		string[] tracks = { "Argent Energy.mp3", "At Doom's Gate.mp3", "Rust, Dust & Guts.mp3",
			"Flesh & Metal.mp3", "Bfg Division.mp3", "Harbinger.mp3", "Rip & Tear.mp3" };
		float[] starts = { 70f, 37f, 144f, 171f, 60f, 80f, 0f };
		for (int i = 0; i < tracks.Length; i++)
		{
			ulong enteredAt = Time.GetTicksMsec();
			main.EnterRoom(i);
			main.CurrentRoom!.ProcessMode = ProcessModeEnum.Disabled;
			await Wait(0.1f);
			float entryElapsed = (Time.GetTicksMsec() - enteredAt) / 1000f;
			Check(music.Stream is AudioStreamMP3 track && Mathf.IsEqualApprox(track.LoopOffset, starts[i])
				&& music.GetPlaybackPosition() >= starts[i] - 0.1f && music.GetPlaybackPosition() < starts[i] + entryElapsed + 0.25f,
				$"room uses the selected track offset: {tracks[i]} (position {music.GetPlaybackPosition()}, loop {((AudioStreamMP3)music.Stream).LoopOffset})");
			music.Seek((float)music.Stream.GetLength() - 0.15f);
			await Wait(0.5f);
			Check(music.Playing && music.GetPlaybackPosition() >= starts[i] - 0.1f && music.GetPlaybackPosition() < starts[i] + 2f,
				"track loops to its selected offset: " + tracks[i]);
			Check(music.Playing && music.Stream.ResourcePath == "res://music/" + tracks[i]
				&& music.Stream is AudioStreamMP3 { Loop: true }
				&& Mathf.IsEqualApprox(music.VolumeDb, -8f), "room starts its looping track immediately: " + tracks[i]);
		}

		music.Seek((float)music.Stream.GetLength() - 0.15f);
		await Wait(0.6f);
		Check(music.Playing && music.GetPlaybackPosition() < 2f, "track loops after its end");
		main.EnterRoom(2);
		main.CurrentRoom!.ProcessMode = ProcessModeEnum.Disabled;
		await Wait(0.2f);
		Check(music.GetPlaybackPosition() >= 144f && music.Stream is AudioStreamMP3 { LoopOffset: 144f },
			"Island track starts at 2:24");
		music.Seek((float)music.Stream.GetLength() - 0.15f);
		await Wait(0.6f);
		Check(music.Playing && music.GetPlaybackPosition() >= 144f && music.GetPlaybackPosition() < 146f,
			"Island track loops back to 2:24");
		main.CurrentRoom!.EmitSignal(Room.SignalName.Cleared);
		await Wait(0.6f);
		Check(music.Playing && music.VolumeDb < -8f && music.VolumeDb > -60f, "room clear fades without abruptly stopping");
		main.EnterRoom(1);
		main.CurrentRoom!.ProcessMode = ProcessModeEnum.Disabled;
		await Wait(1.1f);
		Check(music.Playing && music.Stream.ResourcePath.EndsWith(tracks[1])
			&& Mathf.IsEqualApprox(music.VolumeDb, -8f), "entering during a fade immediately replaces the song and cancels the old fade");
		main.CurrentRoom.EmitSignal(Room.SignalName.Cleared);
		await Wait(1.7f);
		Check(!music.Playing, "music stops after the 1.5-second fade");
		main.RestartRun();
		main.CurrentRoom!.ProcessMode = ProcessModeEnum.Disabled;
		await Wait(0.2f);
		Check(main.CurrentRoom.IsCleared && music.Playing, "safe starting room keeps its music and restart restores volume");
		player.GetNode<HealthComponent>("HealthComponent").TakeDamage(1000);
		await Wait(1.7f);
		Check(GetTree().Paused && !music.Playing, "death fade finishes even while the end screen pauses the game");
		GetTree().Paused = false;
		main.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GD.Print(_failures == 0 ? "all music checks passed" : $"{_failures} music checks FAILED");
		GetTree().Quit(_failures);
	}

	private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

	private void Check(bool passed, string message)
	{
		GD.Print($"{(passed ? "ok" : "FAIL")} {message}");
		if (!passed) _failures++;
	}
}
