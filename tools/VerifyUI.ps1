param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $msbuild (Join-Path $repo 'HH.TiYu.UI\HH.TiYu.UI.csproj') /t:Build /p:Configuration=$Configuration /p:Platform=x86 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'UI build failed' }
$run = Join-Path $PSScriptRoot ('bin\UIVerification\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $run | Out-Null
$sourceAssemblies = @('Model','DAL','BLL','Device','HH.TiYu.SQLite','UI','LJH.GeneralLibrary')
Get-ChildItem -LiteralPath (Join-Path $repo 'lib') -Filter '*.dll' | Where-Object { $sourceAssemblies -notcontains $_.BaseName } | Copy-Item -Destination $run
$projects = @{Model='HH.TiYu.Model'; DAL='HH.TiYu.DAL'; BLL='HH.TiYu.BLL'; Device='HH.TiYu.Device'; 'HH.TiYu.SQLite'='HH.TiYu.SQLite'; UI='HH.TiYu.UI'; 'LJH.GeneralLibrary'='LJH.GeneralLibrary'}
foreach ($name in $sourceAssemblies) {
  Copy-Item -LiteralPath (Join-Path $repo ($projects[$name] + '\bin\' + $Configuration + '\' + $name + '.dll')) -Destination $run
}
$exe = Join-Path $run 'VerifyUI.exe'
Copy-Item -LiteralPath (Join-Path $repo 'HH.DataCollection.Wifi\App.config') -Destination ($exe + '.config')
& $csc /nologo /target:exe /platform:x86 /langversion:4 "/out:$exe" /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$run\UI.dll" "/r:$run\Model.dll" "/r:$run\LJH.GeneralLibrary.dll" (Join-Path $PSScriptRoot 'VerifyUI.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI verifier compilation failed' }
# Constructors only: no Show/ShowDialog or Load handlers; no device/network calls.
# Any legacy constructor-side DAL read uses this isolated directory, never production data.
Push-Location $run
try { & $exe; if ($LASTEXITCODE -ne 0) { throw 'UI verification failed' } } finally { Pop-Location }
Write-Output "Isolated UI verification directory: $run"
