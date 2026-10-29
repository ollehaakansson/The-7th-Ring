# Development setup

Everything needed to go from a fresh `git clone` to a running editor, plus what
each pinned file does and how to get out of trouble.

---

## 1. Quick start

**Windows**

```powershell
git clone https://github.com/ollehaakansson/The-7th-Ring.git
cd The-7th-Ring
.\scripts\install.ps1
.\scripts\run.ps1
```

**macOS**

```bash
git clone https://github.com/ollehaakansson/The-7th-Ring.git
cd The-7th-Ring
./scripts/install.sh
./scripts/run.sh
```

`install` is idempotent — re-run it any time. It never touches anything outside
this repo except the .NET SDK check and, optionally, export templates.

---

## 2. Prerequisites

| Tool | Version | How to get it | Needed for |
| --- | --- | --- | --- |
| Git | any recent | [git-scm.com](https://git-scm.com) | version control |
| Git LFS | 3.x | `winget install GitHub.GitLFS` / `brew install git-lfs` | sprites and audio |
| .NET SDK | **10.0.7 or later** | `winget install Microsoft.DotNet.SDK.10` / `brew install --cask dotnet-sdk` | compiling C# |
| Godot | 4.7.2-stable (.NET) | **downloaded automatically by `install`** | everything |

Use the .NET SDK configured in `global.json`. Engine and SDK settings are also
listed in `versions.env`.

---

## 3. What `install` actually does

1. **Reads `versions.env`** — the single source of truth for the engine version.
2. **Checks the .NET SDK.** Prints the exact install command if it is missing or
   too old. This is a warning, not a failure: the editor still opens, you just
   cannot compile C# yet.
3. **Enables Git LFS** for this clone (`git lfs install --local`).
4. **Downloads the pinned Godot** (~150 MB) into `tools/godot/<version>/` and
   verifies the binary reports the version you pinned.
5. **Optionally downloads export templates** (~1 GB) — only with
   `-ExportTemplates` / `--export-templates`, since you only need them to
   produce a distributable build.

`tools/` is git-ignored. The engine is per-checkout and completely separate from
any Godot installed elsewhere on your machine, so this project cannot break your
other projects and vice versa.

### Useful flags

| Flag | Effect |
| --- | --- |
| `-Force` / `--force` | Delete and re-download the engine (use if a download was corrupted) |
| `-ExportTemplates` / `--export-templates` | Also fetch export templates |

---

## 4. How versions stay in sync

Godot has no `venv` or `requirements.txt`. What actually drifts between machines
is the engine version, the .NET SDK and any NuGet packages. Each is pinned in
exactly one place:

| File | Pins | Why it matters |
| --- | --- | --- |
| `versions.env` | Godot engine version | A project saved in a newer Godot can silently break in an older one |
| `global.json` | .NET SDK band | Keeps the compiler and language features identical |
| `The7thRing.csproj` | `Godot.NET.Sdk` + NuGet packages | C# SDK and dependency versions |
| `.gitattributes` | Line endings, Git LFS rules | Stops Windows/macOS whitespace churn and binary bloat |
| `.editorconfig` | Formatting | Stops diffs full of reindentation |

### Adding a NuGet dependency

Pin an exact version — no floating ranges, no wildcards:

```xml
<ItemGroup>
  <PackageReference Include="Some.Package" Version="1.2.3" />
</ItemGroup>
```

Commit the `.csproj` change; the other person just re-runs `dotnet build`.

### Upgrading the engine

Three things must move together, or the build breaks in confusing ways:

1. `GODOT_VERSION` in `versions.env`
2. `Godot.NET.Sdk/<version>` in `The7thRing.csproj`
3. `<TargetFramework>` in `The7thRing.csproj`, if the new engine needs a newer .NET

Then **both** of us run `install` again. Check the new version's release notes
for a required .NET bump before upgrading.

---

## 5. Daily workflow

```powershell
.\scripts\run.ps1          # open the editor
.\scripts\run.ps1 -Play    # run the game without the editor
dotnet build               # compile C# without opening Godot
```

```bash
./scripts/run.sh           # open the editor
./scripts/run.sh --play    # run the game without the editor
dotnet build
```

`run` always launches the engine from `tools/`, never whatever Godot happens to
be on your `PATH`. This keeps the editor version consistent between machines.

---

## 6. Working together

- **Scene files (`.tscn`) merge badly.** They are marked `merge=binary` in
  `.gitattributes` so git raises a conflict instead of silently corrupting a
  scene. Avoid two people editing the same scene at once; split rooms into
  separate scenes where you can.
- **Binary assets go through Git LFS.** If you introduce a new binary file type,
  add it to `.gitattributes` *before* committing the first such file. LFS does
  not retroactively convert files that are already in history.
- **Never commit `tools/`, `.godot/`, `bin/` or `obj/`.** All ignored already.
  `tools/` alone is around 258 MB.

---

## 7. Building a release

Export templates must match the engine version exactly:

```powershell
.\scripts\install.ps1 -ExportTemplates
```

```bash
./scripts/install.sh --export-templates
```

They install to the user-level Godot data directory, not into the repo, because
the engine looks for them there:

| Platform | Location |
| --- | --- |
| Windows | `%APPDATA%\Godot\export_templates\4.7.2.stable.mono\` |
| macOS | `~/Library/Application Support/Godot/export_templates/4.7.2.stable.mono/` |

---

## 8. Troubleshooting

**PowerShell refuses to run the script**

Allow local scripts once per machine:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

**macOS blocks the downloaded editor**

`install.sh` already strips the quarantine flag. If you moved the app manually:

```bash
xattr -dr com.apple.quarantine tools/godot/4.7.2-stable/Godot_mono.app
```

**Godot opens but C# does nothing**

The SDK is missing or older than 10.0.7. Check with `dotnet --list-sdks`, then
install .NET 10 and restart your terminal so `PATH` picks it up.

**Godot crashes on start with "Failed to load hostfxr"**

Godot only looks for .NET in the system-wide location, so a Homebrew `dotnet`
under `/opt/homebrew` is invisible to it. `run.sh` sets `DOTNET_ROOT` from
whatever `dotnet --list-sdks` reports, so launch through the script rather than
opening `Godot_mono.app` by hand.

**`install` fails with a 404**

The version in `versions.env` does not exist as a release. Check the tag against
[godot-builds releases](https://github.com/godotengine/godot-builds/releases).
The script cleans up after itself, so just fix the version and re-run.

**`-Force` fails with an access-denied error**

Windows briefly locks a freshly extracted or just-executed `.exe` while
antivirus scans it. The script retries with backoff and tells you if the editor
is still running. Close Godot and try again.

**A script cannot read the engine version on Windows**

`Godot_..._win64.exe` is a GUI-subsystem binary: its stdout is only captured
when piped. Use the `_console.exe` variant that ships alongside it for anything
script-readable. Also avoid piping a native command into `Select-Object -First`,
which breaks the pipe early and leaves `$LASTEXITCODE` at -1 after a
*successful* run.

**Assets show up as text pointers instead of images**

Git LFS was not installed when you cloned. Install it, then:

```bash
git lfs install --local && git lfs pull
```

---

## 9. Repo layout

```
├── project.godot        Godot project settings
├── The7thRing.csproj    C# project + NuGet dependencies
├── global.json          .NET SDK pin
├── versions.env         engine version pin (single source of truth)
├── scenes/              .tscn scenes - rooms, player, enemies, weapons
├── src/                 C# gameplay code
├── assets/              sprites and audio (Git LFS)
├── scripts/             install.ps1 / install.sh, run.ps1 / run.sh
├── docs/                game plan and this document
└── tools/               downloaded engine (git-ignored)
```
