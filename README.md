# The 7th Ring

Single-player top-down 2D action game set in a demonic, hell-like dungeon.
Fight through a series of rooms, collect weapons and upgrades, face the final
boss. Low-resolution pixel art inspired by the original DOOM, with room-based
progression inspired by The Binding of Isaac.

Course project for **TDDD23** at Linköping University.
Built in **Godot 4.7.2 (.NET)** with **C#**.

- [Game plan](docs/gameplan.md) — design document
- [Development setup](docs/setup.md) — full setup, versioning and troubleshooting

---

## Getting started

**Windows**

```powershell
git clone https://github.com/ollehaakansson/The-7th-Ring.git
cd The-7th-Ring
.\scripts\install.ps1
.\scripts\run.ps1           # open the editor
.\scripts\run.ps1 -Play     # play the full game
.\scripts\run.ps1 -Play -Sandbox # play only the test room
.\scripts\run.ps1 -Play -Design  # test the final boss with the shotgun
```

**macOS**

```bash
git clone https://github.com/ollehaakansson/The-7th-Ring.git
cd The-7th-Ring
./scripts/install.sh
./scripts/run.sh             # open the editor
./scripts/run.sh --play      # play the full game
./scripts/run.sh --play --sandbox # play only the test room
```

`run` opens the pinned editor. Add the play flag to launch the full game
straight away. Combine it with the sandbox flag for the quicker one-room test
loop.

`install` downloads the exact Godot version this project is pinned to into
`tools/` (git-ignored). Nothing is installed system-wide, so it cannot clash
with any other Godot you have. Re-run it any time.

**You also need the .NET SDK 10.0.7 or later** — `install` checks for it and
prints the command if it is missing:

| Platform | Command |
| --- | --- |
| Windows | `winget install Microsoft.DotNet.SDK.10` |
| macOS | `brew install --cask dotnet-sdk` |

Anything unexpected? See [troubleshooting](docs/setup.md#8-troubleshooting).

---

## Controls

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` | Move |
| Mouse | Aim |
| Left mouse button | Fire the active weapon |
| `Space` | Dodge roll |
| `Q` | Switch between the two held weapons |
| `E` | Replace the active weapon with one on the ground |
| `Enter` | Restart after death or completing the run |
| `Esc` | Pause / continue |

A second weapon is collected automatically when the player has a free weapon
slot. Health and armor pickups are also collected by walking over them.

---

## Layout

```
├── project.godot        Godot project settings
├── The7thRing.csproj    C# project + NuGet dependencies
├── global.json          .NET SDK pin
├── versions.env         engine version pin (single source of truth)
├── scenes/              .tscn scenes - rooms, player, enemies, weapons
├── src/                 C# gameplay code
├── assets/              sprites and audio (Git LFS)
├── scripts/             install and run scripts for both platforms
├── docs/                game plan and setup documentation
└── tools/               downloaded engine (git-ignored)
```

## Team

- Axel Glöckner, axegl999
- Olle Håkansson, ollha403

## Menus

Normal Play opens the home screen: Play starts a fresh run and Quit closes the
game. Escape opens the pause menu, with Continue and Quit. Quit from pause returns
to the home screen. Menus blur the room behind them; pause lowers the music and
home plays Rip & Tear. Design and sandbox modes skip the home screen.
