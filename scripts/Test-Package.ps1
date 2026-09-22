param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Path))
try {
    $entry = $archive.GetEntry('plugin.json')
    if (!$entry) { throw 'Not an installable plugin ZIP: plugin.json must be at the archive root. Extract the outer Actions artifact ZIP first.' }
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    foreach ($field in @('ID', 'Name', 'Version', 'Language', 'ExecuteFileName', 'IcoPath')) {
        if (!$manifest.$field) { throw "plugin.json is missing $field" }
    }
    foreach ($name in @($manifest.ExecuteFileName, $manifest.IcoPath.Replace('\', '/'))) {
        if (!$archive.GetEntry($name)) { throw "Required file missing from ZIP: $name" }
    }
    $expected = @('Images/icon.png', 'Flow.Launcher.Plugin.NaverMapSearch.dll', 'Flow.Launcher.Plugin.NaverMapSearch.deps.json', 'plugin.json', 'README.md', 'LICENSE')
    if (Compare-Object ($archive.Entries.FullName | Sort-Object) ($expected | Sort-Object)) { throw 'ZIP contains unexpected or missing files.' }
    Write-Host "Validated installable ZIP: $($manifest.Name) $($manifest.Version)"
}
finally { $archive.Dispose() }
