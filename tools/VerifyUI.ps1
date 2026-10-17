param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $msbuild (Join-Path $repo 'HH.TiYu.UI\HH.TiYu.UI.csproj') /t:Build /p:Configuration=$Configuration /p:Platform=x86 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'UI build failed' }
$run = Join-Path $PSScriptRoot ('bin\UIVerification\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $run | Out-Null
$sourceAssemblies = @('Model','DAL','BLL','Device','UI','HH.GeneralLibrary')
Get-ChildItem -LiteralPath (Join-Path $repo 'lib') -Filter '*.dll' | Where-Object { $sourceAssemblies -notcontains $_.BaseName -and $_.BaseName -ne 'LJH.GeneralLibrary' -and $_.BaseName -ne 'HH.TiYu.SQLite' -and $_.Name -notmatch '^(AForge(\.|$)|FJ\.CoreWin\.dll$|JpgDll\.dll$|SavePhoto\.dll$)' } | Copy-Item -Destination $run
$projects = @{Model='HH.TiYu.Model'; DAL='HH.TiYu.DAL'; BLL='HH.TiYu.BLL'; Device='HH.TiYu.Device'; UI='HH.TiYu.UI'; 'HH.GeneralLibrary'='HH.GeneralLibrary'}
foreach ($name in $sourceAssemblies) {
  Copy-Item -LiteralPath (Join-Path $repo ($projects[$name] + '\bin\' + $Configuration + '\' + $name + '.dll')) -Destination $run
}
$exe = Join-Path $run 'VerifyUI.exe'
Copy-Item -LiteralPath (Join-Path $repo 'HH.DataCollection.Wifi\App.config') -Destination ($exe + '.config')
& $csc /nologo /target:exe /platform:x86 /langversion:4 "/out:$exe" /r:System.Core.dll /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$run\UI.dll" "/r:$run\Model.dll" "/r:$run\HH.GeneralLibrary.dll" "/r:$run\System.Data.SQLite.dll" (Join-Path $repo 'HH.DataCollection.Wifi\Db\LocalDb.cs') (Join-Path $PSScriptRoot 'VerifyUI.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI verifier compilation failed' }
# Constructors only: no Show/ShowDialog or Load handlers; no device/network calls.
# Constructor-side DAL reads use the actual current schema in this isolated directory.
Push-Location $run
try { & $exe; if ($LASTEXITCODE -ne 0) { throw 'UI verification failed' } } finally { Pop-Location }
# Delete this run's isolated directory after a successful verification, so tools\bin does not
# grow with every run; a failed run keeps its residue for inspection.
for ($attempt = 0; $attempt -lt 5 -and (Test-Path -LiteralPath $run); $attempt++) {
    Start-Sleep -Milliseconds 300
    Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $run) { throw "Verification artifacts could not be removed: $run" }
Write-Output "Isolated UI verification passed. Artifacts verified and removed: $run"
