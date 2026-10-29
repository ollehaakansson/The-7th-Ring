using Godot;

namespace The7thRing;

public partial class HealthComponent : Node
{
	[Signal] public delegate void ChangedEventHandler(int current, int max);

	[Signal] public delegate void ArmorChangedEventHandler(int current, int max);

	[Signal] public delegate void DamagedEventHandler(int amount);
	[Signal] public delegate void DiedEventHandler();

	[Export] public int MaxHealth { get; set; } = 3;
	[Export] public int MaxArmor { get; set; } = 3;

	[Export] public float InvulnerableSeconds { get; set; }

	public int Current { get; private set; }

	public int Armor { get; private set; }

	public bool IsAlive => Current > 0;

	private float _immunity;
	private float _granted;

	public override void _Ready()
	{
		Current = MaxHealth;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_immunity > 0f)
		{
			_immunity -= (float)delta;
		}

		if (_granted > 0f)
		{
			_granted -= (float)delta;
		}
	}

	public void TakeDamage(int amount, bool ignoreHitImmunity = false)
	{
		if (amount <= 0 || Current <= 0 || (!ignoreHitImmunity && _immunity > 0f) || _granted > 0f)
		{
			return;
		}

		if (GetParent() is Player) SoundEffects.Play(this, Armor > 0 ? Sound.ArmorHit : Sound.PlayerHit);
		else if (GetParent() is Enemy) SoundEffects.Play(this, Sound.EnemyHit);
		_immunity = InvulnerableSeconds;
		EmitSignal(SignalName.Damaged, amount);

		// Armor takes the whole hit, so one pickup always blocks one hit.
		if (Armor > 0)
		{
			Armor = Mathf.Max(Armor - amount, 0);
			EmitSignal(SignalName.ArmorChanged, Armor, MaxArmor);
			return;
		}

		Current = Mathf.Max(Current - amount, 0);
		EmitSignal(SignalName.Changed, Current, MaxHealth);

		if (Current == 0)
		{
			if (GetParent() is Player) SoundEffects.Play(this, Sound.PlayerDeath);
			else if (GetParent() is Boss) SoundEffects.Play(this, Sound.BossDeath);
			else if (GetParent() is Enemy) SoundEffects.Play(this, Sound.EnemyDeath);
			EmitSignal(SignalName.Died);
		}
	}

	// A separate timer from hit immunity, so a roll right after a hit cannot cut that window short.
	public void GrantInvulnerability(float seconds)
	{
		_granted = Mathf.Max(_granted, seconds);
	}

	public bool Heal(int amount)
	{
		if (amount <= 0 || Current >= MaxHealth || Current <= 0)
		{
			return false;
		}

		Current = Mathf.Min(Current + amount, MaxHealth);
		EmitSignal(SignalName.Changed, Current, MaxHealth);
		return true;
	}

	public bool AddArmor(int amount)
	{
		if (amount <= 0 || Armor >= MaxArmor)
		{
			return false;
		}

		Armor = Mathf.Min(Armor + amount, MaxArmor);
		EmitSignal(SignalName.ArmorChanged, Armor, MaxArmor);
		return true;
	}

	public void Reset()
	{
		Current = MaxHealth;
		Armor = 0;
		_immunity = 0f;
		_granted = 0f;
		EmitSignal(SignalName.Changed, Current, MaxHealth);
		EmitSignal(SignalName.ArmorChanged, Armor, MaxArmor);
	}
}
