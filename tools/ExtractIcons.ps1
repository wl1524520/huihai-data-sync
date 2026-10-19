# Extract only the collector toolbar icons from the original application's resources.
# Keep this script ASCII for Windows PowerShell 5.1.
param(
    [string]$Source = (Join-Path (Split-Path $PSScriptRoot -Parent) '..\Guojiatizhi\GuojiaTiZhi.exe'),
    [string]$OutDir = (Join-Path (Split-Path $PSScriptRoot -Parent) 'HH.DataCollection.Wifi\Resources')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$asm = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($Source))
$stream = $asm.GetManifestResourceStream('HH.TiYu.GuojiaTiZhi.Properties.Resources.resources')
if ($null -eq $stream) { throw 'Toolbar resource stream not found' }
$reader = New-Object Resources.ResourceReader($stream)
# Original import-students icon -> download; original one-click-report icon -> upload.
$icons = @{ wireless = 'wireless.png'; import = 'download.png'; upload = 'upload.png'; network = 'network.png' }
$saved = @{}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
try {
    $entries = $reader.GetEnumerator()
    while ($entries.MoveNext()) {
        $name = [string]$entries.Key
        if (!$icons.ContainsKey($name)) { continue }
        $image = $entries.Value
        try {
            $image.Save((Join-Path $OutDir $icons[$name]), [Drawing.Imaging.ImageFormat]::Png)
            $saved[$name] = $true
            Write-Output ('Saved: ' + $icons[$name] + ' (source: ' + $name + ')')
        } finally { $image.Dispose() }
    }
    foreach ($name in $icons.Keys) {
        if (!$saved.ContainsKey($name)) { throw ('Missing toolbar icon: ' + $name) }
    }
} finally { $reader.Close(); $stream.Dispose() }
