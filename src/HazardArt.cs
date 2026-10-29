using Godot;

namespace The7thRing;

public abstract partial class HazardArt : Node2D
{
	protected const int Texel = 2;

	[Export] public float StepsPerSecond { get; set; } = 12f;

	// The editor has no Hazard instance, only a plain Area2D, so editor code reads Host instead.
	protected Node2D Host { get; private set; } = null!;
	protected Hazard Hazard => (Hazard)Host;
	protected RandomNumberGenerator Rng { get; } = new();

	protected int Width { get; private set; }
	protected int Height { get; private set; }

	// Top-left corner of the art, in pixels from the hazard's origin.
	protected Vector2 Origin { get; private set; }

	protected bool[,] Mask { get; private set; } = new bool[0, 0];

	protected virtual int Headroom => 0;

	private Image _image = null!;
	private ImageTexture _texture = null!;
	private float _untilStep;

	public override void _Ready()
	{
		Host = GetParent<Node2D>();
		Rng.Seed = Seed();
		Build();
		Step();
	}

	public override void _Process(double delta)
	{
		_untilStep -= (float)delta;
		if (_untilStep > 0f)
		{
			return;
		}

		_untilStep = 1f / StepsPerSecond;
		if (Engine.IsEditorHint())
		{
			Build();
		}

		Step();
	}

	public override void _Draw()
	{
		DrawTexture(_texture, Origin / Texel - new Vector2(0f, Headroom));
	}

	protected virtual Rect2 Bounds()
	{
		Shape2D shape = Host.GetNode<CollisionShape2D>("CollisionShape2D").Shape;
		Vector2 size = (shape is CircleShape2D circle ? Vector2.One * circle.Radius * 2f : ((RectangleShape2D)shape).Size) * Host.Scale;
		return new Rect2(-size / 2f, size);
	}

	protected virtual bool[,] BuildMask(RandomNumberGenerator rng) => Blob(Width, Height, rng);

	protected abstract void Advance();

	protected abstract void Paint();

	protected void Put(int x, int y, Color colour)
	{
		if (x >= 0 && x < Width && y >= -Headroom && y < Height)
		{
			_image.SetPixel(x, y + Headroom, colour);
		}
	}

	protected void Stamp(Image sprite, int x, int y)
	{
		_image.BlendRect(sprite, new Rect2I(Vector2I.Zero, sprite.GetSize()), new Vector2I(x, y + Headroom));
	}

	protected bool Inside(int x, int y) =>
		x >= 0 && x < Width && y >= 0 && y < Height && Mask[x, y];

	protected bool OnEdge(int x, int y) =>
		Inside(x, y) && !(Inside(x - 1, y) && Inside(x + 1, y) && Inside(x, y - 1) && Inside(x, y + 1));

	private void Step()
	{
		Advance();
		_image.Fill(Colors.Transparent);
		Paint();
		_texture.Update(_image);
		QueueRedraw();
	}

	private void Build()
	{
		Rect2 bounds = Bounds();
		Origin = bounds.Position;
		Width = Mathf.Max(1, Mathf.RoundToInt(bounds.Size.X / Texel));
		Height = Mathf.Max(1, Mathf.RoundToInt(bounds.Size.Y / Texel));
		// Its own generator, so rebuilding in the editor gives the same edge without restarting the bubbles.
		Mask = BuildMask(new RandomNumberGenerator { Seed = Seed() });

		Scale = new Vector2(Texel / Host.Scale.X, Texel / Host.Scale.Y);

		_image = Image.CreateEmpty(Width, Height + Headroom, false, Image.Format.Rgba8);
		_texture = ImageTexture.CreateFromImage(_image);
	}

	// From position so the shape is the same every launch. GetHashCode changes between runs.
	private ulong Seed()
	{
		Vector2I at = (Vector2I)Host.GlobalPosition.Round();
		return (ulong)(uint)(at.X * 73856093 ^ at.Y * 19349663);
	}

	private static bool[,] Blob(int w, int h, RandomNumberGenerator rng)
	{
		bool[,] mask = new bool[w, h];
		float radius = Mathf.Min(Mathf.Min(w, h) / 2f, 6f);

		for (int x = 0; x < w; x++)
		{
			for (int y = 0; y < h; y++)
			{
				float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - w / 2f) - (w / 2f - radius));
				float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - h / 2f) - (h / 2f - radius));
				mask[x, y] = dx * dx + dy * dy <= radius * radius;
			}
		}

		return Nibble(mask, rng);
	}

	protected static bool[,] Nibble(bool[,] mask, RandomNumberGenerator rng)
	{
		int w = mask.GetLength(0);
		int h = mask.GetLength(1);
		bool[,] nibbled = (bool[,])mask.Clone();
		for (int x = 0; x < w; x++)
		{
			for (int y = 0; y < h; y++)
			{
				bool edge = mask[x, y]
					&& (x == 0 || y == 0 || x == w - 1 || y == h - 1
						|| !mask[x - 1, y] || !mask[x + 1, y] || !mask[x, y - 1] || !mask[x, y + 1]);
				if (edge && rng.Randf() < 0.4f)
				{
					nibbled[x, y] = false;
				}
			}
		}

		return nibbled;
	}
}
