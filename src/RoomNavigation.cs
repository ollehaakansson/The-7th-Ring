using Godot;

namespace The7thRing;

// A* over a grid of the room. Rebuilt each frame a path is asked for, since hazards and doors change.
public sealed class RoomNavigation
{
	private const int CellSize = 16;

	// About an enemy's half-width.
	private const float Clearance = 10f;

	private readonly AStarGrid2D _grid = new();
	private readonly World2D _world;
	private readonly Vector2 _bounds;
	private ulong _builtFrame = ulong.MaxValue;
	private PhysicsShapeQueryParameters2D? _query;

	public RoomNavigation(World2D world, Vector2 bounds)
	{
		_world = world;
		_bounds = bounds;
	}

	public Vector2[] FindPath(Vector2 from, Vector2 to)
	{
		Build();

		Vector2I start = CellOf(from);
		Vector2I goal = CellOf(to);

		// Enemies often stand inside the clearance margin, so open both ends or they'd never find a path.
		bool startSolid = _grid.IsPointSolid(start);
		bool goalSolid = _grid.IsPointSolid(goal);
		_grid.SetPointSolid(start, false);
		_grid.SetPointSolid(goal, false);

		Vector2[] path = _grid.GetPointPath(start, goal);

		_grid.SetPointSolid(start, startSolid);
		_grid.SetPointSolid(goal, goalSolid);

		// Skip the start cell's centre; walking back to it can mean walking into a wall or fire.
		return path.Length > 1 ? path[1..] : path;
	}

	private void Build()
	{
		ulong frame = Engine.GetPhysicsFrames();
		if (_builtFrame == frame)
		{
			return;
		}

		if (_query is null)
		{
			Setup();
		}

		_builtFrame = frame;

		for (int x = 0; x < _grid.Region.Size.X; x++)
		{
			for (int y = 0; y < _grid.Region.Size.Y; y++)
			{
				Vector2I cell = new(x, y);
				_query!.Transform = new Transform2D(0f, _grid.GetPointPosition(cell));
				_grid.SetPointSolid(cell, _world.DirectSpaceState.IntersectShape(_query, 1).Count > 0);
			}
		}
	}

	private void Setup()
	{
		_grid.Region = new Rect2I(
			Vector2I.Zero,
			new Vector2I(Mathf.CeilToInt(_bounds.X / CellSize), Mathf.CeilToInt(_bounds.Y / CellSize)));
		_grid.CellSize = new Vector2(CellSize, CellSize);
		_grid.Offset = new Vector2(CellSize / 2f, CellSize / 2f);
		_grid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
		_grid.Update();

		_query = new PhysicsShapeQueryParameters2D
		{
			Shape = new CircleShape2D { Radius = Clearance },
			CollisionMask = 1,
			CollideWithAreas = true,
		};
	}

	private Vector2I CellOf(Vector2 position)
	{
		Vector2I cell = new(
			Mathf.FloorToInt(position.X / CellSize),
			Mathf.FloorToInt(position.Y / CellSize));

		return cell.Clamp(Vector2I.Zero, _grid.Region.Size - Vector2I.One);
	}
}
