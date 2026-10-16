param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'HH.DataCollection.Wifi.sln') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed' }
$run = Join-Path $PSScriptRoot ('bin\BusinessVerification\' + [Guid]::NewGuid().ToString('N'))
$lib = Join-Path $repo 'lib'
$output = Join-Path $repo "HH.DataCollection.Wifi\bin\$Configuration"
foreach ($name in @('Model', 'BLL', 'DAL')) {
    $projectOutput = Join-Path $repo "HH.TiYu.$name\bin\$Configuration\$name.dll"
    if ((Get-FileHash -LiteralPath $projectOutput).Hash -ne (Get-FileHash -LiteralPath (Join-Path $output "$name.dll")).Hash) { throw "Collector $name.dll differs from source project output" }
}
foreach ($variant in @('source', 'legacy')) {
    $dir = Join-Path $run $variant
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Get-ChildItem -LiteralPath $lib -Filter '*.dll' -File | Copy-Item -Destination $dir
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mono.Cecil.dll') -Destination $dir
    # The original Model references Json 6; the collector ships Json 7 and
    # redirects that reference. Both processes use the production binding policy.
    Copy-Item -LiteralPath (Join-Path $repo 'HH.DataCollection.Wifi\App.config') -Destination (Join-Path $dir 'VerifyBusiness.exe.config')
    if ($variant -eq 'source') { Get-ChildItem -LiteralPath $output -Filter '*.dll' -File | Copy-Item -Destination $dir -Force }
    if ($variant -eq 'legacy') { & (Join-Path $PSScriptRoot 'PrepareLegacyApi.ps1') -Directory $dir }
    $references = @('System.dll', 'System.Core.dll', 'System.Data.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Runtime.Serialization.dll')
    $references += @('Model.dll', 'BLL.dll', 'LJH.GeneralLibrary.dll', 'Mono.Cecil.dll') | ForEach-Object { Join-Path $dir $_ }
    $compilerArgs = @('/nologo', '/target:exe', '/platform:x86', '/langversion:4', '/codepage:65001', "/out:$dir\VerifyBusiness.exe")
    $compilerArgs += $references | ForEach-Object { '/reference:' + $_ }
    $compilerArgs += @( (Join-Path $PSScriptRoot 'VerifyBusiness.cs'), (Join-Path $PSScriptRoot 'LegacyApiNames.cs') )
    $compilerArgs += '/main:VerifyBusiness'
    & (Join-Path $framework 'csc.exe') @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw "Verification compile failed: $variant" }
    Push-Location $dir
    try {
        & (Join-Path $dir 'VerifyBusiness.exe') $variant $lib
        if ($LASTEXITCODE -ne 0) { throw "Verification failed: $variant" }
    } finally { Pop-Location }
}
$sourceSnapshot = Join-Path $run 'source\behavior.txt'
$legacySnapshot = Join-Path $run 'legacy\behavior.txt'
if ((Get-FileHash -LiteralPath $sourceSnapshot).Hash -ne (Get-FileHash -LiteralPath $legacySnapshot).Hash) {
    Compare-Object (Get-Content -LiteralPath $legacySnapshot) (Get-Content -LiteralPath $sourceSnapshot) | Out-Host
    throw 'Source/legacy pure behavior snapshots differ'
}
# Delete this run's isolated directory after a successful verification, so tools\bin does not
# grow with every run; a failed run keeps its residue for inspection.
for ($attempt = 0; $attempt -lt 5 -and (Test-Path -LiteralPath $run); $attempt++) {
    Start-Sleep -Milliseconds 300
    Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $run) { throw "Verification artifacts could not be removed: $run" }
Write-Host "Behavior snapshots match. Verification artifacts verified and removed: $run"
