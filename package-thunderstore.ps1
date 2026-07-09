# Builds the mod and produces a Thunderstore-ready zip in .\artifacts\
# Usage: powershell -ExecutionPolicy Bypass -File .\package-thunderstore.ps1
$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot

# Read version from the manifest so it stays the single source of truth
$manifest = Get-Content (Join-Path $projectDir "thunderstore\manifest.json") -Raw | ConvertFrom-Json
$version = $manifest.version_number
$name = $manifest.name

Write-Host "Building $name $version..."
dotnet build (Join-Path $projectDir "INeedToPEEak.csproj") -c Release | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$dll = Join-Path $projectDir "bin\Release\netstandard2.1\INeedToPEEak.dll"
if (-not (Test-Path $dll)) { throw "Built DLL not found at $dll" }

# Stage the package: manifest + icon + README + CHANGELOG at root, DLL in plugins/
$stage = Join-Path $env:TEMP "intp_ts_stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory "$stage\plugins" -Force | Out-Null
Copy-Item (Join-Path $projectDir "thunderstore\manifest.json") $stage
Copy-Item (Join-Path $projectDir "thunderstore\icon.png") $stage
Copy-Item (Join-Path $projectDir "thunderstore\CHANGELOG.md") $stage
Copy-Item (Join-Path $projectDir "README.md") $stage
Copy-Item $dll "$stage\plugins"

$artifacts = Join-Path $projectDir "artifacts"
New-Item -ItemType Directory $artifacts -Force | Out-Null
$zip = Join-Path $artifacts "$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Remove-Item $stage -Recurse -Force

Write-Host "Packaged: $zip"
Write-Host "Upload it at https://thunderstore.io/c/peak/create/"
