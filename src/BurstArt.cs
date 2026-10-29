using System.Collections.Generic;
using Godot;

namespace The7thRing;

public partial class BurstArt : HazardArt
{
	private static readonly Color Char = new("232323");
	private static readonly Color Soot = new("1b1b1b");
	private static readonly Color[] CrackGlow = { new("5b0000"), new("9b0000"), new("d70000"), new("ff7f1b"), new("ffe74b") };

	[Export] public Texture2D[] Big { get; set; } = System.Array.Empty<Texture2D>();
	[Export] public Texture2D[] Small { get; set; } = System.Array.Empty<Texture2D>();
	[Export] public int Spacing { get; set; } = 8;
	[Export] public bool CharredGround { get; set; } = true;
	[Export] public bool Single { get; set; }

	[Export] public bool Violent { get; set; }
	[Export] public Color[] Spray { get; set; } = { new("9b0000"), new("cb0000"), new("ff7b7b") };

	protected override int Headroom => Big.Length > 0 ? Big[0].GetHeight() * (Violent ? 5 : 4) / 4 : 0;

	private readonly List<(float X, float Y, float Vx, float Vy)> _drops = new();

	private readonly List<(int X, int Y, int Offset)> _flames = new();
	private Image[] _big = System.Array.Empty<Image>();
	private Image[] _small = System.Array.Empty<Image>();
	private readonly List<(int X, int Y, int Age)> _embers = new();
	private bool[,] _cracks = new bool[0, 0];
	private bool[,] _soot = new bool[0, 0];
	private int _tick;

	protected override void Advance()
	{
		if (_cracks.Length == 0)
		{
			Setup();
		}

		_tick++;

		for (int i = _embers.Count - 1; i >= 0; i--)
		{
			(int x, int y, int age) = _embers[i];
			if (age >= 5)
			{
				_embers.RemoveAt(i);
			}
			else
			{
				_embers[i] = (x + Rng.RandiRange(-1, 1) / 2, y - 1, age + 1);
			}
		}

		if (Violent)
		{
			AdvanceSpray();
		}

		if (CharredGround && Hazard.CurrentPhase == Hazard.Phase.Rest && Rng.Randf() < Width * Height / 600f)
		{
			int x = Rng.RandiRange(0, Width - 1);
			int y = Rng.RandiRange(0, Height - 1);
			if (_cracks[x, y])
			{
				_embers.Add((x, y, 0));
			}
		}
	}

	protected override void Paint()
	{
		Hazard.Phase phase = Hazard.CurrentPhase;
		float progress = Hazard.PhaseProgress;

		int glow = phase switch
		{
			Hazard.Phase.Rest => (_tick / 6) % 2,
			Hazard.Phase.Telegraph => 2 + (progress > 0.5f ? 1 : 0),
			_ => 4,
		};

		for (int x = 0; x < Width && CharredGround; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				if (Inside(x, y))
				{
					Put(x, y, _cracks[x, y] ? CrackGlow[glow] : _soot[x, y] ? Soot : Char);
				}
			}
		}

		foreach ((int x, int y, int age) in _embers)
		{
			Put(x, y, CrackGlow[Mathf.Max(0, 4 - age)]);
		}

		if (phase == Hazard.Phase.Rest)
		{
			return;
		}

		for (int i = 0; i < _flames.Count; i++)
		{
			(int x, int y, int offset) = _flames[i];
			if (phase == Hazard.Phase.Telegraph && offset / 100f > 0.25f + progress * 0.6f)
			{
				continue;
			}

			Image[] sprite = phase == Hazard.Phase.Active ? _big : _small;
			if (sprite.Length == 0)
			{
				continue;
			}

			Image frame = sprite[(_tick + offset) / 2 % sprite.Length];
			int shake = Violent && phase == Hazard.Phase.Active ? Rng.RandiRange(-1, 1) : 0;
			Stamp(frame, x + shake - frame.GetWidth() / 2, y - frame.GetHeight() + 1);

			if (Violent && phase == Hazard.Phase.Active && i == 0)
			{
				Image top = sprite[(_tick + 1) / 2 % sprite.Length];
				Stamp(top, x - shake - top.GetWidth() / 2, y - frame.GetHeight() * 5 / 4 + 1);
			}
		}

		foreach ((float x, float y, float vx, float vy) in _drops)
		{
			Color colour = Spray[Mathf.Clamp((int)(-vy) + 1, 0, Spray.Length - 1)];
			Put(Mathf.RoundToInt(x), Mathf.RoundToInt(y), colour);
			Put(Mathf.RoundToInt(x), Mathf.RoundToInt(y) + 1, colour);
		}
	}

	private void AdvanceSpray()
	{
		for (int i = _drops.Count - 1; i >= 0; i--)
		{
			(float x, float y, float vx, float vy) = _drops[i];
			if (y > Height)
			{
				_drops.RemoveAt(i);
			}
			else
			{
				_drops[i] = (x + vx, y + vy, vx, vy + 0.6f);
			}
		}

		if (Hazard.CurrentPhase != Hazard.Phase.Active || _flames.Count == 0)
		{
			return;
		}

		(int jetX, int jetY, _) = _flames[0];
		int top = Big.Length > 0 ? Big[0].GetHeight() * 5 / 4 : 0;
		for (int n = Hazard.PhaseProgress < 0.1f ? 24 : 6; n > 0; n--)
		{
			_drops.Add((jetX + Rng.RandfRange(-2f, 2f), jetY - Rng.RandfRange(top * 0.3f, top), Rng.RandfRange(-2.5f, 2.5f), Rng.RandfRange(-3f, 0f)));
		}
	}

	private static Image Pixels(Texture2D texture)
	{
		Image image = texture.GetImage();
		image.Convert(Image.Format.Rgba8);
		return image;
	}

	private void Setup()
	{
		// BlendRect needs Images in the same format.
		_big = System.Array.ConvertAll(Big, Pixels);
		_small = System.Array.ConvertAll(Small, Pixels);

		_cracks = new bool[Width, Height];
		_soot = new bool[Width, Height];

		for (int x = 0; x < Width; x += 3)
		{
			for (int y = 0; y < Height; y += 2)
			{
				if (Rng.Randf() < 0.35f)
				{
					for (int dx = 0; dx < 3 && x + dx < Width; dx++)
					{
						for (int dy = 0; dy < 2 && y + dy < Height; dy++)
						{
							_soot[x + dx, y + dy] = true;
						}
					}
				}
			}
		}

		int cracks = Mathf.Max(2, Width * Height / 150);
		for (int i = 0; i < cracks; i++)
		{
			int x = Rng.RandiRange(2, Mathf.Max(2, Width - 3));
			int y = Rng.RandiRange(2, Mathf.Max(2, Height - 3));
			Vector2I dir = Rng.Randf() < 0.5f ? Vector2I.Right : Vector2I.Down;
			for (int len = Rng.RandiRange(6, 14); len > 0; len--)
			{
				if (Inside(x, y) && !OnEdge(x, y))
				{
					_cracks[x, y] = true;
				}

				if (Rng.Randf() < 0.3f)
				{
					dir = dir == Vector2I.Right || dir == Vector2I.Left
						? new Vector2I(0, Rng.Randf() < 0.5f ? 1 : -1)
						: new Vector2I(Rng.Randf() < 0.5f ? 1 : -1, 0);
				}

				x += dir.X;
				y += dir.Y;
			}
		}

		if (Single)
		{
			_flames.Add((Width / 2, Height * 2 / 3, 0));
			if (Violent)
			{
				_flames.Add((Width / 2 - 6, Height * 2 / 3 + 1, 33));
				_flames.Add((Width / 2 + 6, Height * 2 / 3 + 1, 66));
			}

			return;
		}

		// Back to front, so nearer sprites overlap farther ones.
		for (int y = 4, row = 0; y < Height; y += 6, row++)
		{
			for (int x = 2 + (row % 2) * Spacing / 2; x < Width - 1; x += Spacing)
			{
				int fx = x + Rng.RandiRange(-2, 2);
				int fy = Mathf.Min(y + Rng.RandiRange(-1, 1), Height - 2);
				if (Inside(fx, fy))
				{
					_flames.Add((fx, fy, Rng.RandiRange(0, 99)));
				}
			}
		}
	}
}
