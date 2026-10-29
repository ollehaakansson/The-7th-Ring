using System.Collections.Generic;
using Godot;

namespace The7thRing;

// Child hazards must use Manual mode; this node controls their eruption timing.
public partial class GeyserCycle : Node2D
{
	[Export] public float MinPause { get; set; } = 3f;
	[Export] public float MaxPause { get; set; } = 6f;
	[Export] public int PerEruption { get; set; } = 2;
	[Export(PropertyHint.Range, "0,1,0.05")] public float AllChance { get; set; } = 0.15f;

	private readonly RandomNumberGenerator _rng = new();
	private Timer _timer = null!;

	public override void _Ready()
	{
		_rng.Randomize();
		_timer = new Timer { OneShot = true };
		_timer.Timeout += Erupt;
		AddChild(_timer);
		_timer.Start(_rng.RandfRange(MinPause, MaxPause));
	}

	private void Erupt()
	{
		List<Hazard> geysers = new();
		foreach (Node child in GetChildren())
		{
			if (child is Hazard hazard)
			{
				geysers.Add(hazard);
			}
		}

		for (int i = geysers.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(geysers[i], geysers[j]) = (geysers[j], geysers[i]);
		}

		int count = _rng.Randf() < AllChance ? geysers.Count : Mathf.Min(PerEruption, geysers.Count);
		float longest = 0f;
		for (int i = 0; i < count; i++)
		{
			geysers[i].Erupt();
			longest = Mathf.Max(longest, geysers[i].EruptionSeconds);
		}

		_timer.Start(longest + _rng.RandfRange(MinPause, MaxPause));
	}
}
