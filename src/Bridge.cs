using Godot;

namespace The7thRing;

[Tool]
public partial class Bridge : Node2D
{
	internal const string Group = "crossing";

	private const int Texel = 2;

	private static readonly Texture2D Deck = GD.Load<Texture2D>("res://assets/sprites/0x72/env/floor_1.png");

	private Vector2 _size = new(160, 44);

	[Export] public Vector2 Size
	{
		get => _size;
		set { _size = value; QueueRedraw(); }
	}

	[Export] public Color DeckTint { get; set; } = new(1.35f, 1.2f, 1.1f);
	[Export] public Color Rail { get; set; } = new("2b1a12");
	[Export] public float RailWidth { get; set; } = 4f;

	// How long blood takes to rise over the deck, or drain off it. The deck only hurts once it is fully under.
	[Export] public float FloodSeconds { get; set; } = 0.8f;

	internal bool Flooded { get; private set; }

	internal void Flood(bool flooded)
	{
		if (flooded == Flooded)
		{
			return;
		}

		Flooded = flooded;
		Tween tween = CreateTween();
		if (flooded)
		{
			tween.TweenProperty(this, "modulate:a", 0f, FloodSeconds);
			tween.TweenCallback(Callable.From(RefreshRivers));
		}
		else
		{
			RefreshRivers();
			tween.TweenProperty(this, "modulate:a", 1f, FloodSeconds);
		}
	}

	private void RefreshRivers()
	{
		foreach (Node node in GetTree().GetNodesInGroup(RiverArt.Group))
		{
			((RiverArt)node).RefreshCollision();
		}
	}

	internal Vector2[] Outline(Node2D space)
	{
		Transform2D toSpace = space.GlobalTransform.AffineInverse() * GlobalTransform;
		Vector2 half = _size / 2f;
		return new[]
		{
			toSpace * -half,
			toSpace * new Vector2(half.X, -half.Y),
			toSpace * half,
			toSpace * new Vector2(-half.X, half.Y),
		};
	}

	public override void _Draw()
	{
		Rect2 deck = new(-_size / 2f, _size);

		DrawRect(new Rect2(deck.Position + new Vector2(0f, deck.Size.Y), new Vector2(deck.Size.X, Texel * 2)), new Color(0f, 0f, 0f, 0.45f));

		DrawSetTransform(Vector2.Zero, 0f, new Vector2(Texel, Texel));
		DrawTextureRect(Deck, new Rect2(deck.Position / Texel, deck.Size / Texel), tile: true, DeckTint);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

		DrawRect(new Rect2(deck.Position, new Vector2(deck.Size.X, RailWidth)), Rail);
		DrawRect(new Rect2(deck.Position + new Vector2(0f, deck.Size.Y - RailWidth), new Vector2(deck.Size.X, RailWidth)), Rail);
	}
}
