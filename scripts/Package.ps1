param([string]$Version, [string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'src/Flow.Launcher.Plugin.NaverMapSearch/Flow.Launcher.Plugin.NaverMapSearch.csproj'
$manifestPath = Join-Path $repo 'src/Flow.Launcher.Plugin.NaverMapSearch/plugin.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if (!$Version) { $Version = $manifest.Version }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be a stable semantic version such as 1.0.0.' }
if ($manifest.Version -ne $Version) { throw 'Tag version must match plugin.json.' }
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
if ($projectXml.Project.PropertyGroup.Version -ne $Version) { throw 'Project and plugin.json versions must match.' }
& $Dotnet build $project -c Release --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
$build = Join-Path (Split-Path $project -Parent) 'bin/Release/net9.0-windows'
$artifacts = Join-Path $repo '.artifacts'
$stage = Join-Path $artifacts ('stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force (Join-Path $stage 'Images') | Out-Null
# Explicit allowlist: no host assemblies, settings, test files, symbols, or SDK binaries.
foreach ($name in @('Flow.Launcher.Plugin.NaverMapSearch.dll', 'Flow.Launcher.Plugin.NaverMapSearch.deps.json', 'plugin.json')) {
    Copy-Item -LiteralPath (Join-Path $build $name) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $build 'Images/icon.png') -Destination (Join-Path $stage 'Images')
Copy-Item -LiteralPath (Join-Path $repo 'README.md'), (Join-Path $repo 'LICENSE') -Destination $stage
$zip = Join-Path $artifacts "NaverMapSearch-$Version.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Write-Output $zip
