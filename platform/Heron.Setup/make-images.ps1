# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Makes the Setup's two pictures from the brand kit - never a new drawing.

.DESCRIPTION
    The Setup shows two pictures (D-149, docs/brand/README.md):

      images\panel-<w>x<h>.png   the tall panel of the Welcome and Finished
                                 pages: the Heron logo on Mist with the
                                 brand's orange line under it - the same
                                 masthead HeronInstaller.exe draws
      images\icon-<n>.png        the top corner of every other page: the
                                 app icon, white heron on Ink

    Both are drawn from files that are already the brand, with the brand's
    colours by hex from docs/brand/README.md's table:

      Mist    #F7F5F2   the background
      Accent  #FD641A   the line under the logo

    Run it again only when a brand file changes; the PNGs it writes are kept
    beside the script so a build needs no drawing step. Windows PowerShell 5.1
    and System.Drawing, which every Windows has.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File platform\Heron.Setup\make-images.ps1
#>
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$here  = Split-Path -Parent $MyInvocation.MyCommand.Path
$root  = Split-Path -Parent (Split-Path -Parent $here)
$out   = Join-Path $here "images"
$logo  = Join-Path $root "platform\Heron.Installer.App\Resources\heron-logo.png"
$icon  = Join-Path $root "docs\brand\heron-app-icon-ink.png"
New-Item -ItemType Directory -Force -Path $out | Out-Null

$mist   = [System.Drawing.ColorTranslator]::FromHtml("#F7F5F2")
$accent = [System.Drawing.ColorTranslator]::FromHtml("#FD641A")

function New-Canvas([int] $w, [int] $h) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    return @($bmp, $g)
}

# The tall panel - Inno Setup's 164:314 shape, at 100 % and 200 % scale.
$src = [System.Drawing.Image]::FromFile($logo)
foreach ($size in @(@(202, 386), @(404, 772))) {
    $w = $size[0]; $h = $size[1]
    $bmp, $g = New-Canvas $w $h
    $g.Clear($mist)
    $lw = [int]($w * 0.80)
    $lh = [int]($src.Height * $lw / $src.Width)
    $x  = [int](($w - $lw) / 2)
    $y  = [int]($h * 0.34)
    $g.DrawImage($src, $x, $y, $lw, $lh)
    $line = [Math]::Max(2, [int]($w / 101))
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillRectangle($brush, $x, $y + $lh + [int]($h * 0.03), $lw, $line)
    $brush.Dispose(); $g.Dispose()
    $bmp.Save((Join-Path $out ("panel-{0}x{1}.png" -f $w, $h)), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
$src.Dispose()

# The corner icon - Inno Setup's 55 px square, at 100 % and 200 % scale.
$src = [System.Drawing.Image]::FromFile($icon)
foreach ($n in @(55, 110)) {
    $bmp, $g = New-Canvas $n $n
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $n, $n)
    $g.Dispose()
    $bmp.Save((Join-Path $out ("icon-{0}.png" -f $n)), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
$src.Dispose()

Get-ChildItem $out | ForEach-Object { Write-Host ("  {0}  {1:N0} bytes" -f $_.Name, $_.Length) }
