<#
.SYNOPSIS
    Opens the project with the Godot in tools/, not whatever is on PATH. Extra
    arguments go straight to Godot.
.EXAMPLE
    .\scripts\run.ps1               # open the editor
    .\scripts\run.ps1 -Play         # run the game directly
    .\scripts\run.ps1 -Play -Sandbox # run only the test room
    .\scripts\run.ps1 -Play -Design  # run only the rooms being designed
#>
[CmdletBinding()]
param(
    [switch]$Play,
    [switch]$Sandbox,
    [switch]$Design,
    [Parameter(ValueFromRemainingArguments = $true)]$Rest
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot

$V = @{}
Get-Content (Join-Path $RepoRoot 'versions.env') | ForEach-Object {
    if ($_ -match '^\s*([A-Z_][A-Z0-9_]*)\s*=\s*(.*?)\s*$') { $V[$Matches[1]] = $Matches[2] }
}
$tag  = "$($V['GODOT_VERSION'])-$($V['GODOT_RELEASE'])"
$name = "Godot_v${tag}_mono_win64"
$exe  = Join-Path $RepoRoot "tools\godot\$tag\$name\$name.exe"

if (-not (Test-Path $exe)) {
    Write-Host "Godot $tag is not installed in tools/." -ForegroundColor Red
    Write-Host "Run .\scripts\install.ps1 first." -ForegroundColor Yellow
    exit 1
}

$godotArgs = @('--path', $RepoRoot)
if (-not $Play) { $godotArgs += '--editor' }
if ($Rest)      { $godotArgs += $Rest }
if ($Play) {
    & dotnet build $RepoRoot -v quiet --nologo
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
if ($Sandbox) { $godotArgs += @('--', '--sandbox') }
elseif ($Design) { $godotArgs += @('--', '--design') }

& $exe @godotArgs
