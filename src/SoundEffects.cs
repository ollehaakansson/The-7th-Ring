using System.Collections.Generic;
using Godot;

namespace The7thRing;

public enum Sound
{
	Pistol, Shotgun, Rocket, Explosion, Reload, Switch, Impact, EnemyHit, EnemyDeath,
	PlayerHit, PlayerDeath, ArmorHit, Health, Armor, Weapon, Dodge,
	EnemyShot, Charge, BossVolley, Summon, Enrage, BossDeath, Clear, Fire, Victory,
}

public partial class SoundEffects : Node
{
	private readonly Dictionary<Sound, AudioStream> _streams = new();
	private readonly Dictionary<Sound, ulong> _lastPlayed = new();
	private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[24];
	private int _nextVoice;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		AddToGroup("sound_effects");
		foreach (Sound cue in System.Enum.GetValues<Sound>())
			_streams[cue] = GD.Load<AudioStream>($"res://assets/audio/{cue.ToString().ToLowerInvariant()}.wav");
		for (int i = 0; i < _voices.Length; i++)
		{
			_voices[i] = new AudioStreamPlayer { Name = $"Voice{i}", Bus = "SFX" };
			AddChild(_voices[i]);
		}
	}

	public static void Play(Node source, Sound cue)
	{
		if (source.GetTree().GetFirstNodeInGroup("sound_effects") is SoundEffects audio)
			audio.Play(cue);
	}

	private void Play(Sound cue)
	{
		ulong now = Time.GetTicksMsec();
		ulong spacing = cue switch
		{
			Sound.Impact or Sound.EnemyHit => 70,
			Sound.EnemyDeath or Sound.EnemyShot => 100,
			Sound.Fire => 300,
			_ => 30,
		};
		if (_lastPlayed.TryGetValue(cue, out ulong last) && now - last < spacing) return;
		_lastPlayed[cue] = now;
		AudioStreamPlayer voice = _voices[_nextVoice];
		for (int i = 0; i < _voices.Length; i++)
		{
			int index = (_nextVoice + i) % _voices.Length;
			if (!_voices[index].Playing)
			{
				voice = _voices[index];
				break;
			}
		}
		_nextVoice = (_nextVoice + 1) % _voices.Length;
		voice.Stop();
		voice.Stream = _streams[cue];
		voice.VolumeDb = cue switch
		{
			Sound.Pistol => 0f,
			Sound.Shotgun or Sound.Rocket => 2f,
			Sound.Reload or Sound.Switch => -5f,
			Sound.Explosion or Sound.BossDeath => -1f,
			Sound.Impact => -22f,
			Sound.EnemyHit => -18f,
			Sound.EnemyDeath or Sound.EnemyShot => -15f,
			Sound.Fire => -18f,
			Sound.BossVolley or Sound.Charge => -9f,
			_ => -7f,
		};
		voice.PitchScale = cue is Sound.Pistol or Sound.Shotgun or Sound.EnemyHit or Sound.EnemyDeath
			? (float)GD.RandRange(0.96, 1.04) : 1f;
		voice.Play();
	}

	public void StopAll()
	{
		foreach (AudioStreamPlayer voice in _voices) voice.Stop();
		_lastPlayed.Clear();
	}
}
