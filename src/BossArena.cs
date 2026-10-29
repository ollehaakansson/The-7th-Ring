using Godot;

namespace The7thRing;

public partial class BossArena : Area2D
{
	private Room _room = null!;
	private Boss _boss = null!;
	private Node _fire = null!;
	private ProgressBar _bar = null!;
	private bool _started;
	private bool _enraged;
	private bool _finished;

	public override void _Ready()
	{
		_room = GetParent<Room>();
		_boss = _room.GetNode<Boss>("Enemies/Boss");
		_fire = _room.GetNode("FireRing");
		_bar = _room.GetNode<ProgressBar>("BossUI/Health");
		HealthComponent health = _boss.GetNode<HealthComponent>("HealthComponent");
		_bar.MaxValue = health.MaxHealth;
		_bar.Value = health.Current;
		health.Changed += OnHealthChanged;
		health.Died += OnBossDied;
		_room.Cleared += () =>
		{
			_finished = true;
			_fire.QueueFree();
		};
		BodyEntered += body =>
		{
			if (body is Player && !_started)
			{
				_started = true;
				Callable.From(Start).CallDeferred();
			}
		};
	}

	private void Start()
	{
		_boss.ProcessMode = ProcessModeEnum.Inherit;
		_room.StartWaves();
	}

	private void OnBossDied()
	{
		_boss.SetPhysicsProcess(false);
		foreach (Node enemy in _room.GetNode("Enemies").GetChildren())
		{
			if (enemy != _boss) enemy.QueueFree();
		}
	}

	private void OnHealthChanged(int current, int max)
	{
		_bar.Value = current;
		if (current > 0 && current <= max / 2 && !_enraged)
		{
			_enraged = true;
			Callable.From(Enrage).CallDeferred();
		}
	}

	private void Enrage()
	{
		if (_finished || !GodotObject.IsInstanceValid(_boss) || _boss.IsQueuedForDeletion())
		{
			return;
		}

		SoundEffects.Play(this, Sound.Enrage);
		_boss.RestSeconds = 0.25f;
		_boss.WindUpSeconds = 0.3f;
		_boss.VolleyShots = 11;
		_boss.AddsPerSummon = 4;
		_boss.SummonCooldown = 7.5f;
		_boss.MaxChargeSeconds = 1.3f;
		_boss.DazedSeconds = 0.35f;
		foreach (Node child in _fire.GetChildren())
		{
			if (child is Hazard hazard)
			{
				hazard.Erupt();
			}
		}
		_room.Spawn(_room.GetNode("PhaseSupplies"));
	}
}
