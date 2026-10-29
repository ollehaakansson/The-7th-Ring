using System.Threading.Tasks;
using Godot;

namespace The7thRing;

public partial class WeaponHudCheck : Node
{
	private int _failures;

	public override async void _Ready()
	{
		Main main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
		AddChild(main);
		Player player = main.GetNode<Player>("Player");
		player.SetPhysicsProcess(false);
		HUD hud = main.GetNode<HUD>("HUD");
		Label name = hud.GetNode<Label>("WeaponInfo/Current/Name");
		TextureRect icon = hud.GetNode<TextureRect>("WeaponInfo/Current/Icon");
		Label hint = hud.GetNode<Label>("WeaponInfo/Current/SwitchHint");
		await Frames();
		Check(name.Text == "PISTOL" && icon.Texture == player.ActiveWeapon.Texture && !hint.Visible, "single weapon shows pistol icon and name without switch hint");
		WeaponPickup shotgun = GD.Load<PackedScene>("res://scenes/ShotgunPickup.tscn").Instantiate<WeaponPickup>();
		shotgun.Position = new Vector2(600, 300);
		AddChild(shotgun);
		player.TryCollectWeapon(shotgun, false);
		await Frames();
		Check(hint.Visible && hint.Text.Contains("Q"), "two weapons show Q switch hint");
		player.SwitchWeapon();
		await Frames();
		Check(name.Text == "SHOTGUN" && icon.Texture == player.ActiveWeapon.Texture, "switching updates weapon name and icon");
		player.GlobalPosition = new Vector2(350, 180);
		WeaponPickup pickup = GD.Load<PackedScene>("res://scenes/BazookaPickup.tscn").Instantiate<WeaponPickup>();
		pickup.Position = player.GlobalPosition;
		AddChild(pickup);
		await Frames();
		Label prompt = pickup.GetNode<Label>("Prompt");
		Check(prompt.Visible && prompt.Text.Contains("E") && prompt.GetThemeColor("font_color").A < 0.7f, "full inventory shows translucent E over new weapon");
		string screenshot = OS.GetEnvironment("RING_HUD_SCREENSHOT");
		if (!string.IsNullOrEmpty(screenshot))
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			GetViewport().GetTexture().GetImage().SavePng(screenshot);
		}
		player.GlobalPosition = new Vector2(400, 180);
		await Frames();
		Check(!prompt.Visible, "leaving pickup hides prompt");
		player.GlobalPosition = pickup.GlobalPosition;
		await Frames();
		Input.ActionPress("interact");
		await Frames();
		Input.ActionRelease("interact");
		Check(player.ActiveWeapon.DisplayName == "RPG" && name.Text == "RPG", "E replaces active weapon and updates HUD");
		WeaponPickup duplicate = GD.Load<PackedScene>("res://scenes/BazookaPickup.tscn").Instantiate<WeaponPickup>();
		duplicate.Position = player.GlobalPosition;
		AddChild(duplicate);
		await Frames();
		Check(!duplicate.GetNode<Label>("Prompt").Visible, "already-owned weapon has no misleading pickup hint");
		main.QueueFree();
		await Frames();
		GD.Print(_failures == 0 ? "all weapon HUD checks passed" : $"{_failures} weapon HUD checks FAILED");
		GetTree().Quit(_failures);
	}

	private async Task Frames()
	{
		for (int i = 0; i < 4; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool ok, string text)
	{
		GD.Print((ok ? "ok " : "FAIL ") + text);
		if (!ok) _failures++;
	}
}
