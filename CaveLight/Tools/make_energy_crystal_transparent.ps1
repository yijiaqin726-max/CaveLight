$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$InputPath = Join-Path $Root "Assets\Art\Energy\source_energy_crystal.png"
$OutputPath = Join-Path $Root "Assets\Art\Energy\energy_crystal_transparent.png"
$PreviewPath = Join-Path $Root "Assets\Art\Energy\energy_crystal_preview_on_dark.png"

function Test-BackgroundPixel {
    param([System.Drawing.Color]$Color)

    if ($Color.A -eq 0) {
        return $true
    }

    $max = [Math]::Max($Color.R, [Math]::Max($Color.G, $Color.B))
    $min = [Math]::Min($Color.R, [Math]::Min($Color.G, $Color.B))
    $saturation = if ($max -eq 0) { 0 } else { ($max - $min) / [double]$max }
    return $max -ge 210 -and $min -ge 185 -and $saturation -le 0.22
}

$Source = [System.Drawing.Bitmap]::new($InputPath)
$Width = $Source.Width
$Height = $Source.Height
$Background = New-Object 'bool[,]' $Width, $Height
$Visited = New-Object 'bool[,]' $Width, $Height
$Queue = [System.Collections.Generic.Queue[object]]::new()

function Add-IfBackground {
    param([int]$X, [int]$Y)

    if ($X -lt 0 -or $Y -lt 0 -or $X -ge $Width -or $Y -ge $Height) {
        return
    }

    if ($Visited[$X, $Y]) {
        return
    }

    $Visited[$X, $Y] = $true
    if (Test-BackgroundPixel $Source.GetPixel($X, $Y)) {
        $Background[$X, $Y] = $true
        $Queue.Enqueue(@($X, $Y))
    }
}

for ($x = 0; $x -lt $Width; $x++) {
    Add-IfBackground $x 0
    Add-IfBackground $x ($Height - 1)
}

for ($y = 0; $y -lt $Height; $y++) {
    Add-IfBackground 0 $y
    Add-IfBackground ($Width - 1) $y
}

while ($Queue.Count -gt 0) {
    $Point = $Queue.Dequeue()
    $x = [int]$Point[0]
    $y = [int]$Point[1]
    Add-IfBackground ($x + 1) $y
    Add-IfBackground ($x - 1) $y
    Add-IfBackground $x ($y + 1)
    Add-IfBackground $x ($y - 1)
}

$Output = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$MinX = $Width
$MinY = $Height
$MaxX = -1
$MaxY = -1

for ($y = 0; $y -lt $Height; $y++) {
    for ($x = 0; $x -lt $Width; $x++) {
        $Color = $Source.GetPixel($x, $y)
        if ($Background[$x, $y]) {
            $Output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, $Color.R, $Color.G, $Color.B))
            continue
        }

        $NearBackground = $false
        for ($dy = -1; $dy -le 1; $dy++) {
            for ($dx = -1; $dx -le 1; $dx++) {
                $nx = $x + $dx
                $ny = $y + $dy
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $Width -and $ny -lt $Height -and $Background[$nx, $ny]) {
                    $NearBackground = $true
                }
            }
        }

        $Alpha = if ($NearBackground -and (Test-BackgroundPixel $Color)) { 120 } else { 255 }
        $Output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($Alpha, $Color.R, $Color.G, $Color.B))

        if ($Alpha -gt 0) {
            if ($x -lt $MinX) { $MinX = $x }
            if ($y -lt $MinY) { $MinY = $y }
            if ($x -gt $MaxX) { $MaxX = $x }
            if ($y -gt $MaxY) { $MaxY = $y }
        }
    }
}

$Padding = 12
if ($MaxX -ge 0) {
    $Left = [Math]::Max(0, $MinX - $Padding)
    $Top = [Math]::Max(0, $MinY - $Padding)
    $Right = [Math]::Min($Width - 1, $MaxX + $Padding)
    $Bottom = [Math]::Min($Height - 1, $MaxY + $Padding)
    $CropWidth = $Right - $Left + 1
    $CropHeight = $Bottom - $Top + 1
    $Cropped = [System.Drawing.Bitmap]::new($CropWidth, $CropHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $Graphics = [System.Drawing.Graphics]::FromImage($Cropped)
    $Graphics.DrawImage($Output, [System.Drawing.Rectangle]::new(0, 0, $CropWidth, $CropHeight), [System.Drawing.Rectangle]::new($Left, $Top, $CropWidth, $CropHeight), [System.Drawing.GraphicsUnit]::Pixel)
    $Graphics.Dispose()
    $Output.Dispose()
    $Output = $Cropped
}

$Output.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$Preview = [System.Drawing.Bitmap]::new($Output.Width, $Output.Height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$Graphics = [System.Drawing.Graphics]::FromImage($Preview)
$Graphics.Clear([System.Drawing.Color]::FromArgb(18, 20, 26))
$Graphics.DrawImage($Output, 0, 0, $Output.Width, $Output.Height)
$Graphics.Dispose()
$Preview.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)

$AlphaZero = 0
$Total = $Output.Width * $Output.Height
for ($y = 0; $y -lt $Output.Height; $y++) {
    for ($x = 0; $x -lt $Output.Width; $x++) {
        if ($Output.GetPixel($x, $y).A -eq 0) {
            $AlphaZero++
        }
    }
}

$Ratio = $AlphaZero / [double]$Total
Write-Output "Input image: $InputPath"
Write-Output "Output image: $OutputPath"
Write-Output "Preview image: $PreviewPath"
Write-Output "Output mode: RGBA"
Write-Output ("Transparent pixel ratio: {0:0.0000}" -f $Ratio)
Write-Output "Has alpha=0 pixels: $($AlphaZero -gt 0)"
if ($Ratio -lt 0.10) {
    Write-Output "Warning: transparent pixel ratio is low. Consider loosening background thresholds."
}

$Source.Dispose()
$Output.Dispose()
$Preview.Dispose()
