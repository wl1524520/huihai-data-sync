# One-shot tool: extract toolbar icons (wireless/upload/network) from the original
# GuojiaTiZhi.exe embedded resources into this project's Resources folder as PNG.
# After extraction the PNGs live in the project; no runtime dependency on the original exe.
# NOTE: keep this file pure ASCII (Windows PowerShell 5.1 reads .ps1 as GBK without BOM).
param(
    [string]$Source = "D:\Huihai\Guojiatizhi\GuojiaTiZhi.exe",
    [string]$OutDir = "D:\Huihai\HH.DataCollection.Wifi\HH.DataCollection.Wifi\Resources"
)

$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Source))
$stream = $asm.GetManifestResourceStream("HH.TiYu.GuojiaTiZhi.Properties.Resources.resources")
if ($null -eq $stream) { throw "resource HH.TiYu.GuojiaTiZhi.Properties.Resources not found" }
$reader = New-Object System.Resources.ResourceReader($stream)
$all = @{}
$reader.GetEnumerator() | ForEach-Object { $all[$_.Key] = $_.Value }
$reader.Close()

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
foreach ($name in @("wireless", "upload", "network")) {
    $bmp = $all[$name]
    if ($null -eq $bmp) { throw "resource entry $name not found" }
    $out = Join-Path $OutDir "$name.png"
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output ("saved: " + $out + " (" + $bmp.Width + "x" + $bmp.Height + ")")
}
