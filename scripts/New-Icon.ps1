# Original vector geometry; no NAVER trademark assets are used.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$imageDir = Join-Path $PSScriptRoot '../src/Flow.Launcher.Plugin.NaverMapSearch/Images'
New-Item -ItemType Directory -Force $imageDir | Out-Null
$bitmap = [System.Drawing.Bitmap]::new(128, 128)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#12372B'))
$pinBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#53E3A6'))
$pin = [System.Drawing.Drawing2D.GraphicsPath]::new()
try {
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.FillEllipse($background, 4, 4, 120, 120)
    $pin.AddBezier(64, 104, 56, 93, 32, 70, 32, 51)
    $pin.AddBezier(32, 51, 32, 11, 96, 11, 96, 51)
    $pin.AddBezier(96, 51, 96, 70, 72, 93, 64, 104)
    $pin.CloseFigure()
    $graphics.FillPath($pinBrush, $pin)
    $graphics.FillEllipse($background, 51, 38, 26, 26)
    $bitmap.Save((Join-Path $imageDir 'icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $pin.Dispose()
    $pinBrush.Dispose()
    $background.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
