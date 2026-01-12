#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# Prepare output
Remove-Item -Recurse -Force "$root/compiled" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path "$root/compiled" | Out-Null

dotnet restore "$root/K4-Zenith.sln"

$projects = @(
  @{Label="Core"; Csproj="src/K4-Zenith.csproj"; Dll="K4-Zenith.dll"; PublishDir="src/bin/K4-Zenith/plugins/K4-Zenith"}
  @{Label="KitsuneMenu"; Csproj="KitsuneMenu/src/KitsuneMenu.csproj"; Dll="KitsuneMenu.dll"; PublishDir="KitsuneMenu/src/bin/KitsuneMenu/shared/KitsuneMenu"}
  @{Label="CustomTags"; Csproj="modules/custom-tags/K4-Zenith-CustomTags.csproj"; Dll="K4-Zenith-CustomTags.dll"; PublishDir="modules/custom-tags/bin/K4-Zenith-CustomTags"}
  @{Label="ExtendedCommands"; Csproj="modules/extended-commands/K4-Zenith-ExtendedCommands.csproj"; Dll="K4-Zenith-ExtendedCommands.dll"; PublishDir="modules/extended-commands/bin/K4-Zenith-ExtendedCommands"}
  @{Label="Ranks"; Csproj="modules/ranks/K4-Zenith-Ranks.csproj"; Dll="K4-Zenith-Ranks.dll"; PublishDir="modules/ranks/bin/K4-Zenith-Ranks"}
  @{Label="Statistics"; Csproj="modules/statistics/K4-Zenith-Stats.csproj"; Dll="K4-Zenith-Stats.dll"; PublishDir="modules/statistics/bin/K4-Zenith-Stats"}
  @{Label="TimeStats"; Csproj="modules/time-stats/K4-Zenith-TimeStats.csproj"; Dll="K4-Zenith-TimeStats.dll"; PublishDir="modules/time-stats/bin/K4-Zenith-TimeStats"}
  @{Label="Toplists"; Csproj="modules/toplists/K4-Zenith-Toplists.csproj"; Dll="K4-Zenith-Toplists.dll"; PublishDir="modules/toplists/bin/K4-Zenith-Toplists"}
  @{Label="Bans"; Csproj="modules/zenith-bans/K4-Zenith-Bans.csproj"; Dll="K4-Zenith-Bans.dll"; PublishDir="modules/zenith-bans/bin/K4-Zenith-Bans"}
)

foreach ($p in $projects) {
  Write-Host "[INFO] Building $($p.Label)..."
  dotnet publish $p.Csproj -c Release -f net8.0 --nologo
  $srcDir = Join-Path $root $p.PublishDir
  $folderName = [System.IO.Path]::GetFileNameWithoutExtension($p.Dll)
  $targetRoot = if ($p.Label -eq "KitsuneMenu") {
    Join-Path $root "compiled/counterstrikesharp/shared"
  } else {
    Join-Path $root "compiled/counterstrikesharp/plugins"
  }
  $dstDir = Join-Path $targetRoot $folderName
  Remove-Item -Recurse -Force $dstDir -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Path $dstDir | Out-Null
  Copy-Item -Path (Join-Path $srcDir '*') -Destination $dstDir -Recurse -Force
  Write-Host "  -> Contents of $($p.Label) copied to $dstDir"
}

Write-Host "[OK] Artifacts prêts dans $root/compiled"
