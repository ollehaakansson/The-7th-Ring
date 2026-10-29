using Godot;

namespace The7thRing;

[Tool]
public partial class Obstacle : StaticBody2D
{
	private Vector2 _size = new(48, 48);
	private Color _colour = Colors.White;

	[Export] public Vector2 Size
	{
		get => _size;
		set { _size = value; Rebuild(); }
	}

	[Export] public Color Colour
	{
		get => _colour;
		set { _colour = value; Rebuild(); }
	}

	[Export] public float BorderWidth
	{
		get => _borderWidth;
		set { _borderWidth = value; QueueRedraw(); }
	}

	private float _borderWidth = 2f;

	public override void _Ready()
	{
		Rebuild();
	}

	private void Rebuild()
	{
		// Setters run during load, before the children exist.
		if (!IsNodeReady())
		{
			return;
		}

		Vector2 half = _size / 2f;

		Polygon2D visual = GetNode<Polygon2D>("Visual");
		visual.Color = _colour;
		visual.Polygon = new[]
		{
			-half,
			new Vector2(half.X, -half.Y),
			half,
			new Vector2(-half.X, half.Y),
		};

		CollisionShape2D collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = _size };

		QueueRedraw();
	}

	public override void _Draw()
	{
		// Inset so the border doesn't make the block look bigger than its collision.
		Rect2 edge = new Rect2(-_size / 2f, _size).Grow(-_borderWidth / 2f);
		DrawRect(edge, Colors.Black, filled: false, width: _borderWidth);
	}
}
