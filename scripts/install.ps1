<#
.SYNOPSIS
    Downloads the Godot version pinned in versions.env into tools/. Nothing is
    installed system-wide.

.EXAMPLE
    .\scripts\install.ps1
    .\scripts\install.ps1 -ExportTemplates    # also fetch export templates (~1 GB)
    .\scripts\install.ps1 -Force              # re-download even if present
#>
[CmdletBinding()]
param(
    [switch]$ExportTemplates,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot

function Info  { param($m) Write-Host "  $m" }
function Step  { param($m) Write-Host "`n==> $m" -ForegroundColor Cyan }
function Ok    { param($m) Write-Host "  OK  $m" -ForegroundColor Green }
function Warn  { param($m) Write-Host "  !   $m" -ForegroundColor Yellow }
function Fail  { param($m) Write-Host "`nFAILED: $m" -ForegroundColor Red; exit 1 }

# Windows locks a fresh .exe for a moment (antivirus, process teardown), so retry.
function Remove-DirWithRetry {
    param([string]$Path, [int]$Attempts = 5)

    $running = Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and $_.Path.StartsWith($Path, [StringComparison]::OrdinalIgnoreCase) }
    if ($running) {
        Fail "Godot is still running (PID $($running.Id -join ', ')). Close the editor and try again."
    }

    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            Remove-Item -Recurse -Force $Path -ErrorAction Stop
            return
        } catch {
            if ($i -eq $Attempts) {
                Fail "Could not remove $Path after $Attempts attempts: $($_.Exception.Message)"
            }
            Warn "Path is locked (attempt $i/$Attempts), retrying in ${i}s..."
            Start-Sleep -Seconds $i
        }
    }
}

$EnvFile = Join-Path $RepoRoot 'versions.env'
if (-not (Test-Path $EnvFile)) { Fail "versions.env not found at $EnvFile" }

$V = @{}
Get-Content $EnvFile | ForEach-Object {
    if ($_ -match '^\s*([A-Z_][A-Z0-9_]*)\s*=\s*(.*?)\s*$') { $V[$Matches[1]] = $Matches[2] }
}
foreach ($k in 'GODOT_VERSION','GODOT_RELEASE','DOTNET_SDK_VERSION') {
    if (-not $V.ContainsKey($k)) { Fail "versions.env is missing $k" }
}
if ($V['GODOT_EXPORT_TEMPLATES'] -eq '1') { $ExportTemplates = $true }

$GodotTag  = "$($V['GODOT_VERSION'])-$($V['GODOT_RELEASE'])"
$GodotName = "Godot_v${GodotTag}_mono_win64"
$BaseUrl   = "https://github.com/godotengine/godot-builds/releases/download/$GodotTag"

Write-Host "The 7th Ring - development environment setup" -ForegroundColor Magenta
Info "Godot      $GodotTag (.NET build)"
Info ".NET SDK   $($V['DOTNET_SDK_VERSION']) or later"
Info "Repo       $RepoRoot"

Step ".NET SDK"
$DotnetOk = $false
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    $sdks = & dotnet --list-sdks 2>$null
    $required = [int]($V['DOTNET_SDK_VERSION'] -split '\.')[0]
    $found = @($sdks | ForEach-Object { if ($_ -match '^(\d+)\.') { [int]$Matches[1] } }) |
             Where-Object { $_ -ge $required }
    if ($found) {
        Ok ".NET SDK $($found[0]).x found (need $required or later)"
        $DotnetOk = $true
    } else {
        Warn "dotnet is installed but no SDK >= $required was found"
    }
} else {
    Warn ".NET SDK not found"
}

if (-not $DotnetOk) {
    $major = ($V['DOTNET_SDK_VERSION'] -split '\.')[0]
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        Info "Install it with:"
        Info "    winget install Microsoft.DotNet.SDK.$major"
    } else {
        Info "Download it from https://dotnet.microsoft.com/download"
    }
    Warn "C# scripts will not compile until the SDK is installed."
    Warn "The Godot editor itself will still open, so continuing."
}

Step "Git LFS"
if (Get-Command git-lfs -ErrorAction SilentlyContinue) {
    & git -C $RepoRoot lfs install --local | Out-Null
    Ok "Git LFS active (sprites, audio and other binaries are stored properly)"
} else {
    Warn "git-lfs not found - binary assets will bloat the repo history."
    Info "Install with: winget install GitHub.GitLFS"
}

Step "Godot engine"
$ToolsDir  = Join-Path $RepoRoot 'tools'
$GodotDir  = Join-Path $ToolsDir "godot\$GodotTag"
$GodotExe  = Join-Path $GodotDir "$GodotName\$GodotName.exe"
# The plain .exe prints nothing to a pipe, so read output from the console one.
$GodotCli  = Join-Path $GodotDir "$GodotName\${GodotName}_console.exe"

if ((Test-Path $GodotExe) -and -not $Force) {
    Ok "Already present at tools\godot\$GodotTag"
} else {
    if ($Force -and (Test-Path $GodotDir)) { Remove-DirWithRetry -Path $GodotDir }
    New-Item -ItemType Directory -Force -Path $GodotDir | Out-Null

    $zipUrl = "$BaseUrl/$GodotName.zip"
    $zip    = Join-Path $env:TEMP "$GodotName.zip"
    Info "Downloading $GodotName.zip (~150 MB)"
    Info $zipUrl
    try {
        $pp = $ProgressPreference; $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $zipUrl -OutFile $zip -UseBasicParsing
        $ProgressPreference = $pp
    } catch {
        # A half-made folder would make the next run think it's installed.
        Remove-Item -Recurse -Force $GodotDir -ErrorAction SilentlyContinue
        Fail "Download failed: $($_.Exception.Message)`n  Check that $GodotTag exists at https://github.com/godotengine/godot-builds/releases"
    }
    Info "Extracting"
    Expand-Archive -Path $zip -DestinationPath $GodotDir -Force
    Remove-Item $zip -Force
    if (-not (Test-Path $GodotExe)) { Fail "Expected editor at $GodotExe but it is not there" }
    Ok "Installed to tools\godot\$GodotTag"
}

# Check the version. Don't pipe into Select-Object -First, it sets
# $LASTEXITCODE to -1.
$versionOutput = @(& $GodotCli --version 2>$null)
$reported = if ($versionOutput.Count -gt 0) { $versionOutput[0] } else { $null }
if (-not $reported) { Warn "Could not read the engine version from $GodotCli" }
if ($reported -and $reported -notlike "$($V['GODOT_VERSION'])*") {
    Warn "Binary reports '$reported' but versions.env pins $($V['GODOT_VERSION'])"
} elseif ($reported) {
    Ok "Version check: $reported"
}

if ($ExportTemplates) {
    Step "Export templates"
    $TplRoot = Join-Path $env:APPDATA "Godot\export_templates\$($V['GODOT_VERSION']).$($V['GODOT_RELEASE']).mono"
    if ((Test-Path $TplRoot) -and -not $Force) {
        Ok "Already present"
    } else {
        $tpzName = "Godot_v${GodotTag}_mono_export_templates.tpz"
        $tpz     = Join-Path $env:TEMP $tpzName
        Info "Downloading $tpzName (~1 GB, this takes a while)"
        $pp = $ProgressPreference; $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri "$BaseUrl/$tpzName" -OutFile $tpz -UseBasicParsing
        $ProgressPreference = $pp

        $staging = Join-Path $env:TEMP "godot-tpl-$([guid]::NewGuid())"
        Expand-Archive -Path $tpz -DestinationPath $staging -Force
        New-Item -ItemType Directory -Force -Path $TplRoot | Out-Null
        Copy-Item -Path (Join-Path $staging 'templates\*') -Destination $TplRoot -Recurse -Force
        Remove-Item $staging -Recurse -Force; Remove-Item $tpz -Force
        Ok "Installed to $TplRoot"
    }
} else {
    Step "Export templates"
    Info "Skipped. Run with -ExportTemplates when you need to build a release."
}

Write-Host "`nSetup complete." -ForegroundColor Green
Write-Host "Open the editor with:  " -NoNewline; Write-Host ".\scripts\run.ps1" -ForegroundColor Cyan
if (-not $DotnetOk) {
    Write-Host "Install the .NET SDK first if you intend to write C# today." -ForegroundColor Yellow
}

exit 0
