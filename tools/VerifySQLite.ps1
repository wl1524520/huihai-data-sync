param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'HH.DataCollection.Wifi.sln') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$output=Join-Path $repo "HH.DataCollection.Wifi\bin\$Configuration"
$projectOutput=Join-Path $repo "HH.TiYu.SQLite\bin\$Configuration\HH.TiYu.SQLite.dll"
if((Get-FileHash $projectOutput).Hash -ne (Get-FileHash (Join-Path $output 'HH.TiYu.SQLite.dll')).Hash){throw 'Collector does not use source SQLite output'}
$run=Join-Path $PSScriptRoot ('bin\SQLiteVerification\'+[Guid]::NewGuid().ToString('N'))
foreach($variant in @('source','legacy')){
    $dir=Join-Path $run $variant
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Get-ChildItem -LiteralPath $output -Filter '*.dll' -File | Copy-Item -Destination $dir
    Copy-Item (Join-Path $PSScriptRoot 'Mono.Cecil.dll') $dir
    Copy-Item (Join-Path $repo 'HH.DataCollection.Wifi\App.config') (Join-Path $dir 'VerifySQLite.exe.config')
    if($variant -eq 'legacy'){Copy-Item (Join-Path $repo 'lib\HH.TiYu.SQLite.dll') $dir -Force}
    if($variant -eq 'legacy'){ & (Join-Path $PSScriptRoot 'PrepareLegacyApi.ps1') -Directory $dir }
    $refs=@('System.dll','System.Core.dll','System.Data.dll','System.Data.Linq.dll','System.Xml.Linq.dll','System.Windows.Forms.dll')
    $refs+=@('Model.dll','HH.TiYu.SQLite.dll','Device.dll','LJH.GeneralLibrary.dll','System.Data.SQLite.dll','Mono.Cecil.dll') | ForEach-Object{Join-Path $dir $_}
    $compilerArgs=@('/main:VerifySQLite','/nologo','/target:exe','/platform:x86','/langversion:4','/codepage:65001',"/out:$dir\VerifySQLite.exe")
    if($variant -eq 'source'){$compilerArgs+='/define:SOURCE'}
    $compilerArgs+=$refs | ForEach-Object{'/reference:'+$_}
    $compilerArgs+=@( (Join-Path $PSScriptRoot 'VerifySQLite.cs'),(Join-Path $PSScriptRoot 'LegacyApiNames.cs'),(Join-Path $repo 'HH.DataCollection.Wifi\Db\LocalDb.cs'))
    & (Join-Path $framework 'csc.exe') @compilerArgs
    if($LASTEXITCODE -ne 0){throw "Compile failed: $variant"}
    Push-Location $dir
    try { & (Join-Path $dir 'VerifySQLite.exe') (Join-Path $repo 'lib\HH.TiYu.SQLite.dll');if($LASTEXITCODE -ne 0){throw "Verification failed: $variant"} } finally{Pop-Location}
}
if((Get-FileHash (Join-Path $run 'source\behavior.txt')).Hash -ne (Get-FileHash (Join-Path $run 'legacy\behavior.txt')).Hash){throw 'Legacy/source behavior mismatch'}
# Delete this run's isolated directory after a successful verification, so tools\bin does not
# grow with every run; a failed run keeps its residue for inspection.
for ($attempt = 0; $attempt -lt 5 -and (Test-Path -LiteralPath $run); $attempt++) {
    Start-Sleep -Milliseconds 300
    Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $run) { throw "Verification artifacts could not be removed: $run" }
Write-Host "Behavior snapshots match. Artifacts verified and removed: $run"
