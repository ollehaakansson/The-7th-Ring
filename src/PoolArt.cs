using System.Collections.Generic;
using Godot;

namespace The7thRing;

[Tool]
public partial class PoolArt : HazardArt
{
	[Export] public Color Outline { get; set; } = new("000000");
	[Export] public Color Rim { get; set; } = new("4f0000");
	[Export] public Color[] Surface { get; set; } = { new("670000"), new("7f0000"), new("9b0000") };
	[Export] public Color Bubble { get; set; } = new("cb0000");
	[Export] public Color Shine { get; set; } = new("ff7b7b");
	[Export] public Color Hollow { get; set; } = new("4f0000");
	[Export] public Color[] Steam { get; set; } = { new("ffb7b7"), new("df8787"), new("af4747") };
	[Export] public float BubbleRate { get; set; } = 1.2f;

	[Export] public bool Round { get; set; }

	// How much of the area a Round pool fills, so the pool can sit small inside a wider danger zone.
	[Export(PropertyHint.Range, "0.1,1,0.05")] public float Fill { get; set; } = 1f;

	[Export] public bool HideAtRest { get; set; }

	private readonly List<(int X, int Y, int Age)> _bubbles = new();
	private readonly List<(int X, int Y, int Age)> _steam = new();
	private float _time;

	protected override int Headroom => 8;

	protected override bool[,] BuildMask(RandomNumberGenerator rng)
	{
		if (!Round)
		{
			return base.BuildMask(rng);
		}

		bool[,] mask = new bool[Width, Height];
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				float dx = (x + 0.5f) / Width * 2f - 1f;
				float dy = (y + 0.5f) / Height * 2f - 1f;
				mask[x, y] = dx * dx + dy * dy <= Fill * Fill;
			}
		}

		return Nibble(mask, rng);
	}

	protected override void Advance()
	{
		if (HideAtRest && !Engine.IsEditorHint())
		{
			Visible = Hazard.CurrentPhase != Hazard.Phase.Rest;
		}

		_time += 1f / StepsPerSecond;

		for (int i = _steam.Count - 1; i >= 0; i--)
		{
			(int x, int y, int age) = _steam[i];
			if (age >= 6)
			{
				_steam.RemoveAt(i);
			}
			else
			{
				_steam[i] = (x + (age % 3 == 2 ? Rng.RandiRange(-1, 1) : 0), y - 1, age + 1);
			}
		}

		for (int i = _bubbles.Count - 1; i >= 0; i--)
		{
			(int x, int y, int age) = _bubbles[i];
			if (age >= 6)
			{
				_bubbles.RemoveAt(i);
				if (Rng.Randf() < 0.6f)
				{
					_steam.Add((x, y - 2, 0));
				}
			}
			else
			{
				_bubbles[i] = (x, y, age + 1);
			}
		}

		float expected = BubbleRate * Width * Height / 100f / StepsPerSecond;
		for (float n = expected; n > 0f; n -= 1f)
		{
			if (Rng.Randf() >= n)
			{
				continue;
			}

			int bx = Rng.RandiRange(2, Width - 3);
			int by = Rng.RandiRange(2, Height - 3);
			if (Inside(bx, by) && !OnEdge(bx, by) && !OnEdge(bx - 1, by) && !OnEdge(bx + 1, by))
			{
				_bubbles.Add((bx, by, 0));
			}
		}
	}

	protected override void Paint()
	{
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				if (!Inside(x, y))
				{
					continue;
				}

				if (OnEdge(x, y))
				{
					Put(x, y, Outline);
				}
				else if (OnEdge(x - 1, y) || OnEdge(x + 1, y) || OnEdge(x, y - 1) || OnEdge(x, y + 1))
				{
					Put(x, y, Rim);
				}
				else
				{
					float wave = Mathf.Sin(x * 0.45f + _time * 1.1f)
						+ Mathf.Sin(y * 0.6f - _time * 0.8f)
						+ Mathf.Sin((x + y) * 0.25f + _time * 1.7f);
					int band = wave < -0.8f ? 0 : wave < 1.1f ? 1 : 2;
					Put(x, y, Surface[band]);
				}
			}
		}

		foreach ((int x, int y, int age) in _bubbles)
		{
			PaintBubble(x, y, age);
		}

		foreach ((int x, int y, int age) in _steam)
		{
			Color colour = Steam[Mathf.Min(age / 2, Steam.Length - 1)];
			Put(x, y, colour);
			if (age < 3)
			{
				Put(x, y - 1, colour);
			}
		}
	}

	private void PaintBubble(int x, int y, int age)
	{
		switch (age)
		{
			case 0:
				Put(x, y, Hollow);
				break;
			case 1:
				Put(x, y, Bubble);
				Put(x, y - 1, Shine);
				break;
			case 2:
				Put(x - 1, y, Bubble);
				Put(x + 1, y, Bubble);
				Put(x, y + 1, Bubble);
				Put(x, y - 1, Bubble);
				Put(x, y, Hollow);
				Put(x - 1, y - 1, Shine);
				break;
			case 3:
				for (int dx = -1; dx <= 1; dx++)
				{
					Put(x + dx, y - 2, Bubble);
					Put(x + dx, y + 1, Rim);
					Put(x + dx, y - 1, Bubble);
					Put(x + dx, y, Bubble);
				}

				Put(x - 2, y - 1, Rim);
				Put(x + 2, y - 1, Rim);
				Put(x - 2, y, Rim);
				Put(x + 2, y, Rim);
				Put(x - 1, y - 2, Shine);
				Put(x, y - 2, Shine);
				Put(x - 1, y - 1, Shine);
				break;
			case 4:
				Put(x - 2, y - 2, Bubble);
				Put(x + 2, y - 2, Bubble);
				Put(x - 2, y + 1, Bubble);
				Put(x + 2, y + 1, Bubble);
				Put(x, y - 3, Bubble);
				Put(x, y, Hollow);
				Put(x - 1, y, Hollow);
				Put(x + 1, y, Hollow);
				break;
			default:
				Put(x - 3, y - 2, Surface[2]);
				Put(x + 3, y - 2, Surface[2]);
				Put(x, y - 4, Bubble);
				break;
		}
	}
}
