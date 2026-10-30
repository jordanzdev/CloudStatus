<#
.SYNOPSIS
    Generates the CloudStatus branding in assets\: CloudStatus.ico (16-256 px) and logo.svg.

.DESCRIPTION
    The logo is a white headphone outline with three rising teal level bars between the ear cups,
    on a dark rounded square. Both outputs come from the same geometry, defined below on a
    256-unit grid. Sizes up to 24 px use a bolder variant so the shape stays readable in the tray.
    Requires Windows PowerShell 5.1 (System.Drawing).
#>

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$smallSizeLimit = 24

$colors = @{
    BackgroundTop    = '#2B313D'
    BackgroundBottom = '#161A21'
    Headphones       = '#FFFFFF'
    Level            = '#26C6A6'
}

$variants = @{
    Regular = @{
        Inset = 8; Radius = 48; ArcRadius = 76; ArcY = 128; Stroke = 20
        CupWidth = 34; CupTop = 124; CupHeight = 76; CupRadius = 12; CupX = 34
        Bars = @(@(92, 172, 20, 32), @(118, 148, 20, 56), @(144, 124, 20, 80))
    }
    Bold    = @{
        Inset = 0; Radius = 44; ArcRadius = 90; ArcY = 124; Stroke = 30
        CupWidth = 46; CupTop = 118; CupHeight = 98; CupRadius = 14; CupX = 16
        Bars = @(@(84, 172, 26, 44), @(115, 136, 26, 80), @(146, 136, 26, 80))
    }
}

function New-RoundedRect([float] $x, [float] $y, [float] $width, [float] $height, [float] $radius) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $diameter = 2 * $radius
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $path
}

function New-LogoBitmap([int] $size) {
    $v = if ($size -le $smallSizeLimit) { $variants.Bold } else { $variants.Regular }
    $bitmap = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.ScaleTransform($size / 256, $size / 256)

    $side = 256 - 2 * $v.Inset
    $gradient = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.PointF 0, 0), (New-Object Drawing.PointF 0, 256),
        ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundTop)), ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundBottom))
    $g.FillPath($gradient, (New-RoundedRect $v.Inset $v.Inset $side $side $v.Radius))

    $white = [Drawing.ColorTranslator]::FromHtml($colors.Headphones)
    $pen = New-Object Drawing.Pen $white, $v.Stroke
    $g.DrawArc($pen, 128 - $v.ArcRadius, $v.ArcY - $v.ArcRadius, 2 * $v.ArcRadius, 2 * $v.ArcRadius, 180, 180)

    $brush = New-Object Drawing.SolidBrush $white
    $g.FillPath($brush, (New-RoundedRect $v.CupX $v.CupTop $v.CupWidth $v.CupHeight $v.CupRadius))
    $g.FillPath($brush, (New-RoundedRect (256 - $v.CupX - $v.CupWidth) $v.CupTop $v.CupWidth $v.CupHeight $v.CupRadius))

    $levelBrush = New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml($colors.Level))
    foreach ($bar in $v.Bars) { $g.FillRectangle($levelBrush, $bar[0], $bar[1], $bar[2], $bar[3]) }

    $g.Dispose()
    $bitmap
}

function New-LogoPng([int] $size) {
    $bitmap = New-LogoBitmap $size
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    , $stream.ToArray()
}

function Write-Ico([string] $path, [int[]] $iconSizes) {
    $images = foreach ($size in $iconSizes) { , (New-LogoPng $size) }
    $stream = [IO.File]::Create($path)
    $writer = New-Object IO.BinaryWriter $stream
    $writer.Write([uint16] 0)                  # reserved
    $writer.Write([uint16] 1)                  # type: icon
    $writer.Write([uint16] $iconSizes.Count)
    $offset = 6 + 16 * $iconSizes.Count
    for ($i = 0; $i -lt $iconSizes.Count; $i++) {
        $dimension = if ($iconSizes[$i] -ge 256) { 0 } else { $iconSizes[$i] } # 0 means 256
        $writer.Write([byte] $dimension)
        $writer.Write([byte] $dimension)
        $writer.Write([byte] 0)                # palette colors
        $writer.Write([byte] 0)                # reserved
        $writer.Write([uint16] 1)              # planes
        $writer.Write([uint16] 32)             # bits per pixel
        $writer.Write([uint32] $images[$i].Length)
        $writer.Write([uint32] $offset)
        $offset += $images[$i].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
    $writer.Dispose()
}

function Write-Svg([string] $path) {
    $v = $variants.Regular
    $side = 256 - 2 * $v.Inset
    $left = 128 - $v.ArcRadius
    $right = 128 + $v.ArcRadius
    $rightCup = 256 - $v.CupX - $v.CupWidth
    $bars = ($v.Bars | ForEach-Object { "    <rect x=`"$($_[0])`" y=`"$($_[1])`" width=`"$($_[2])`" height=`"$($_[3])`" />" }) -join "`n"
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">
  <title>CloudStatus</title>
  <defs>
    <linearGradient id="background" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="$($colors.BackgroundTop)" />
      <stop offset="1" stop-color="$($colors.BackgroundBottom)" />
    </linearGradient>
  </defs>
  <rect x="$($v.Inset)" y="$($v.Inset)" width="$side" height="$side" rx="$($v.Radius)" fill="url(#background)" />
  <path d="M $left $($v.ArcY) A $($v.ArcRadius) $($v.ArcRadius) 0 0 1 $right $($v.ArcY)" fill="none" stroke="$($colors.Headphones)" stroke-width="$($v.Stroke)" />
  <g fill="$($colors.Headphones)">
    <rect x="$($v.CupX)" y="$($v.CupTop)" width="$($v.CupWidth)" height="$($v.CupHeight)" rx="$($v.CupRadius)" />
    <rect x="$rightCup" y="$($v.CupTop)" width="$($v.CupWidth)" height="$($v.CupHeight)" rx="$($v.CupRadius)" />
  </g>
  <g fill="$($colors.Level)">
$bars
  </g>
</svg>
"@
    [IO.File]::WriteAllText($path, $svg.Replace("`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding $false))
}

Write-Ico (Join-Path $assets 'CloudStatus.ico') $sizes
Write-Svg (Join-Path $assets 'logo.svg')

# Preview for reviewing the design by hand; not committed.
if ($env:CLOUDSTATUS_BRANDING_PREVIEW) {
    foreach ($size in 16, 32, 128) {
        [IO.File]::WriteAllBytes((Join-Path $env:CLOUDSTATUS_BRANDING_PREVIEW "logo-$size.png"), (New-LogoPng $size))
    }
}
Write-Host "Written to ${assets}: CloudStatus.ico ($($sizes -join ', ') px), logo.svg"
