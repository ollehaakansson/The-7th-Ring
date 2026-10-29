using Godot;

namespace The7thRing;

public partial class HUD : CanvasLayer
{
	private static readonly Color Drained = new(0.22f, 0.14f, 0.16f);

	[Export] public float FlashSeconds { get; set; } = 0.25f;
	[Export] public float FlashAlpha { get; set; } = 0.35f;

	private HBoxContainer _pips = null!;
	private HBoxContainer _armor = null!;
	private ColorRect _flash = null!;
	private Texture2D _heartFull = null!;
	private Texture2D _heartHalf = null!;
	private Texture2D _heartEmpty = null!;
	private Texture2D _shield = null!;
	private Label _ammo = null!;
	private Control _weaponInfo = null!;
	private TextureRect _weaponIcon = null!;
	private Label _weaponName = null!;
	private Label _switchHint = null!;

	internal Player? Player { get; set; }

	public override void _Ready()
	{
		_pips = GetNode<HBoxContainer>("Pips");
		_armor = GetNode<HBoxContainer>("Armor");
		_flash = GetNode<ColorRect>("Flash");
		_ammo = GetNode<Label>("Ammo");
		_weaponInfo = GetNode<Control>("WeaponInfo");
		_weaponIcon = GetNode<TextureRect>("WeaponInfo/Current/Icon");
		_weaponName = GetNode<Label>("WeaponInfo/Current/Name");
		_switchHint = GetNode<Label>("WeaponInfo/Current/SwitchHint");
		_heartFull = GD.Load<Texture2D>("res://assets/sprites/0x72/ui_heart_full.png");
		_heartHalf = GD.Load<Texture2D>("res://assets/sprites/0x72/ui_heart_half.png");
		_heartEmpty = GD.Load<Texture2D>("res://assets/sprites/0x72/ui_heart_empty.png");
		_shield = GD.Load<Texture2D>("res://assets/sprites/custom/shield.png");
	}

	public override void _Process(double delta)
	{
		_ammo.Visible = Player is not null;
		_weaponInfo.Visible = Player is not null;
		if (Player is not null)
		{
			Weapon weapon = Player.ActiveWeapon;
			_weaponInfo.Position = new Vector2(_pips.Position.X + Mathf.Max(_pips.GetCombinedMinimumSize().X, _armor.GetCombinedMinimumSize().X) + 16f, _pips.Position.Y);
			_weaponIcon.Texture = weapon.Texture;
			_weaponName.Text = weapon.DisplayName;
			_switchHint.Visible = Player.WeaponCount == 2;
			_ammo.Text = weapon.Reloading ? "RELOADING" : $"{weapon.Loaded}/{weapon.MagazineSize}";
		}
	}

	public void Track(HealthComponent health)
	{
		health.Changed += OnChanged;
		health.ArmorChanged += OnArmorChanged;
		health.Damaged += OnDamaged;
		OnChanged(health.Current, health.MaxHealth);
		OnArmorChanged(health.Armor, health.MaxArmor);
	}

	private void OnChanged(int current, int max)
	{
		int hearts = (max + 1) / 2;
		Resize(_pips, hearts, () => new TextureRect
		{
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		});

		for (int i = 0; i < hearts; i++)
		{
			int left = current - (i * 2);
			((TextureRect)_pips.GetChild(i)).Texture = left >= 2 ? _heartFull : left == 1 ? _heartHalf : _heartEmpty;
		}
	}

	private void OnArmorChanged(int current, int max)
	{
		_armor.Visible = current > 0;
		Resize(_armor, max, () => new TextureRect
		{
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = _shield,
		});

		for (int i = 0; i < _armor.GetChildCount(); i++)
		{
			((TextureRect)_armor.GetChild(i)).Modulate = i < current ? Colors.White : Drained;
		}
	}

	private static void Resize(HBoxContainer row, int count, System.Func<Control> make)
	{
		while (row.GetChildCount() > count)
		{
			Node last = row.GetChild(row.GetChildCount() - 1);
			row.RemoveChild(last);
			last.QueueFree();
		}

		while (row.GetChildCount() < count)
		{
			row.AddChild(make());
		}
	}

	private void OnDamaged(int amount)
	{
		_flash.Color = new Color(_flash.Color, FlashAlpha);
		CreateTween().TweenProperty(_flash, "color:a", 0f, FlashSeconds);
	}
}
