# Builds the plugin in Release and zips a Thunderstore package into dist\.
# The zip can be installed in Gale with "Import local mod".
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet build "$root\src\RestedBrews.csproj" -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$manifest = Get-Content "$root\thunderstore\manifest.json" -Raw | ConvertFrom-Json
$stage = Join-Path $root "dist\stage"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null

Copy-Item "$root\thunderstore\manifest.json", "$root\thunderstore\README.md", "$root\thunderstore\icon.png" $stage
Copy-Item "$root\src\bin\Release\RestedBrews.dll" $stage

$zip = Join-Path $root "dist\RestedBrews-$($manifest.version_number).zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Remove-Item -Recurse -Force $stage
Write-Host "Package: $zip"
