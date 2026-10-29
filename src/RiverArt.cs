using System.Collections.Generic;
using Godot;

namespace The7thRing;

[Tool]
public partial class RiverArt : PoolArt
{
	// Banks wander this many pixels in and out, so the river does not look ruled.
	[Export] public float Wobble { get; set; } = 3f;

	internal const string Group = "river";

	private Line2D Path => Host.GetNode<Line2D>("Path");

	public override void _Ready()
	{
		AddToGroup(Group);
		base._Ready();
	}

	internal void RefreshCollision() => UpdateCollision(Points());

	protected override Rect2 Bounds()
	{
		Vector2[] points = Points();
		Rect2 box = new(points.Length > 0 ? points[0] : Vector2.Zero, Vector2.Zero);
		foreach (Vector2 point in points)
		{
			box = box.Expand(point);
		}

		return box.Grow(Path.Width / 2f + Wobble + Texel);
	}

	protected override bool[,] BuildMask(RandomNumberGenerator rng)
	{
		Vector2[] points = Points();
		UpdateCollision(points);

		bool[,] mask = new bool[Width, Height];
		if (points.Length == 0)
		{
			return mask;
		}

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				Vector2 at = Origin + new Vector2(x + 0.5f, y + 0.5f) * Texel;
				float reach = Path.Width / 2f + Wobble * Mathf.Sin(at.X * 0.05f + at.Y * 0.08f);
				mask[x, y] = DistanceSquared(points, at, Path.Closed) <= reach * reach;
			}
		}

		return Nibble(mask, rng);
	}

	private Vector2[] Points()
	{
		Line2D path = Path;
		Vector2[] points = path.Points;
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = path.Transform * points[i];
		}

		return points;
	}

	private void UpdateCollision(Vector2[] points)
	{
		List<Vector2[]> water = new();
		if (points.Length > 0)
		{
			Geometry2D.PolyEndType ends = Path.Closed ? Geometry2D.PolyEndType.Joined : Geometry2D.PolyEndType.Round;
			water.AddRange(Geometry2D.OffsetPolyline(points, Path.Width / 2f, Geometry2D.PolyJoinType.Round, ends));
		}

		foreach (Node node in GetTree().GetNodesInGroup(Bridge.Group))
		{
			if (node is not Bridge bridge || bridge.Flooded)
			{
				continue;
			}

			Vector2[] deck = bridge.Outline(Host);
			List<Vector2[]> cut = new();
			foreach (Vector2[] piece in water)
			{
				// Holes pass straight through, the clip leaves them alone.
				cut.AddRange(Geometry2D.IsPolygonClockwise(piece) ? new[] { piece } : Geometry2D.ClipPolygons(piece, deck));
			}

			water = cut;
		}

		water = WithoutHoles(water);

		// River.tscn has a fixed set of Water slots, as nodes cannot be added while the room is still loading.
		for (int i = 0; i < Host.GetChildCount(); i++)
		{
			if (Host.GetChild(i) is not CollisionPolygon2D slot)
			{
				continue;
			}

			Vector2[] outline = water.Count > 0 ? water[0] : System.Array.Empty<Vector2>();
			if (water.Count > 0)
			{
				water.RemoveAt(0);
			}

			// Only on change, or the editor would mark the room as modified every frame.
			if (!System.MemoryExtensions.SequenceEqual<Vector2>(slot.Polygon, outline))
			{
				slot.Polygon = outline;
			}
		}

		if (water.Count > 0)
		{
			GD.PushWarning($"{Host.Name}: more bridges than Water slots, some river pieces have no collision. Add a CollisionPolygon2D to River.tscn.");
		}
	}

	// Collision polygons cannot have holes (a moat's island, a bridge ending mid-river), so each shape is cut in two
	// through every hole it surrounds. Godot marks holes by winding them clockwise.
	private static List<Vector2[]> WithoutHoles(List<Vector2[]> polygons)
	{
		List<Vector2[]> solids = polygons.FindAll(polygon => !Geometry2D.IsPolygonClockwise(polygon));
		foreach (Vector2[] hole in polygons.FindAll(Geometry2D.IsPolygonClockwise))
		{
			float cut = 0f;
			foreach (Vector2 point in hole)
			{
				cut += point.Y / hole.Length;
			}

			List<Vector2[]> next = new();
			foreach (Vector2[] solid in solids)
			{
				if (!Geometry2D.IsPointInPolygon(hole[0], solid))
				{
					next.Add(solid);
					continue;
				}

				Rect2 box = new(solid[0], Vector2.Zero);
				foreach (Vector2 point in solid)
				{
					box = box.Expand(point);
				}

				box = box.Grow(1f);
				foreach (Rect2 half in new[] { new Rect2(box.Position, new Vector2(box.Size.X, cut - box.Position.Y)), new Rect2(box.Position.X, cut, box.Size.X, box.End.Y - cut) })
				{
					Vector2[] halfOutline = { half.Position, new(half.End.X, half.Position.Y), half.End, new(half.Position.X, half.End.Y) };
					foreach (Vector2[] part in Geometry2D.IntersectPolygons(solid, halfOutline))
					{
						next.AddRange(Geometry2D.ClipPolygons(part, hole));
					}
				}
			}

			solids = next.FindAll(polygon => !Geometry2D.IsPolygonClockwise(polygon));
		}

		return solids;
	}

	private static float DistanceSquared(Vector2[] points, Vector2 at, bool closed)
	{
		if (points.Length == 1)
		{
			return at.DistanceSquaredTo(points[0]);
		}

		float best = float.MaxValue;
		int segments = closed ? points.Length : points.Length - 1;
		for (int i = 1; i <= segments; i++)
		{
			Vector2 a = points[i - 1];
			Vector2 ab = points[i % points.Length] - a;
			float t = Mathf.Clamp((at - a).Dot(ab) / Mathf.Max(ab.LengthSquared(), 0.0001f), 0f, 1f);
			best = Mathf.Min(best, at.DistanceSquaredTo(a + ab * t));
		}

		return best;
	}
}
