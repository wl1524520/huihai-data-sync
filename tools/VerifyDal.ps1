param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'HH.DataCollection.Wifi.sln') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed' }
$run = Join-Path $PSScriptRoot ('bin\DalVerification\' + [Guid]::NewGuid().ToString('N'))
$lib = Join-Path $repo 'lib'
$sourceDal = Join-Path $repo "HH.TiYu.DAL\bin\$Configuration\DAL.dll"
foreach ($variant in @('source')) {
    $dir = Join-Path $run $variant
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Copy-Item (Join-Path $lib 'config.xml'), (Join-Path $lib 'DbUpdate_SQLITE.sql') $dir
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mono.Cecil.dll') -Destination $dir
    if ($variant -eq 'source') {
        $collectorOutput = Join-Path $repo "HH.DataCollection.Wifi\bin\$Configuration"
        $deployedDal = Join-Path $collectorOutput 'DAL.dll'
        if ((Get-FileHash -LiteralPath $sourceDal).Hash -ne (Get-FileHash -LiteralPath $deployedDal).Hash) {
            throw 'Collector output DAL.dll differs from the source project output'
        }
        Get-ChildItem -LiteralPath $collectorOutput -Filter '*.dll' -File | Copy-Item -Destination $dir -Force
    }
    $references = @('System.dll','System.Core.dll','System.Data.dll','System.Data.Linq.dll','System.Windows.Forms.dll','System.Xml.dll','System.Xml.Linq.dll')
    $references += @('DAL.dll','BLL.dll','Model.dll','HH.GeneralLibrary.dll','System.Data.SQLite.dll','Newtonsoft.Json.dll','Mono.Cecil.dll') | ForEach-Object { Join-Path $dir $_ }
    $compilerArgs = @('/nologo', '/target:exe', '/platform:x86', '/langversion:4', '/codepage:65001', "/out:$dir\VerifyDal.exe")
    $compilerArgs += $references | ForEach-Object { '/reference:' + $_ }
    $compilerArgs += @((Join-Path $PSScriptRoot 'VerifyDal.cs'), (Join-Path $repo 'HH.DataCollection.Wifi\Db\LocalDb.cs'), (Join-Path $repo 'HH.DataCollection.Wifi\Sync\SyncConfig.cs'))
    $compilerArgs += @((Join-Path $repo 'HH.DataCollection.Wifi\Sync\StudentSync.cs'), (Join-Path $repo 'HH.DataCollection.Wifi\Sync\SyncApi.cs'))
    & (Join-Path $framework 'csc.exe') @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw "Verification compile failed: $variant" }
    Push-Location $dir
    try {
        & (Join-Path $dir 'VerifyDal.exe') $variant (Join-Path $lib 'DAL.dll')
        if ($LASTEXITCODE -ne 0) { throw "Verification failed: $variant" }
    } finally { Pop-Location }
}
# Delete this run's isolated directory after a successful verification, so tools\bin does not
# grow with every run; a failed run keeps its residue for inspection.
for ($attempt = 0; $attempt -lt 5 -and (Test-Path -LiteralPath $run); $attempt++) {
    Start-Sleep -Milliseconds 300
    Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $run) { throw "Verification artifacts could not be removed: $run" }
Write-Host "Verification artifacts verified and removed: $run"
