param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'HH.GeneralLibrary\HH.GeneralLibrary.csproj') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0){throw 'GeneralLibrary build failed'}
$run=Join-Path $PSScriptRoot ('bin\GeneralLibraryVerification\'+[Guid]::NewGuid().ToString('N'))
foreach($variant in @('source','legacy')){
 $dir=Join-Path $run $variant
 New-Item -ItemType Directory -Path $dir -Force | Out-Null
 Get-ChildItem (Join-Path $repo 'lib') -Filter '*.dll' -File | Copy-Item -Destination $dir
 if($variant -eq 'legacy'){ & (Join-Path $PSScriptRoot 'PrepareLegacyApi.ps1') -Directory $dir }
 if($variant -eq 'source'){Copy-Item (Join-Path $repo "HH.GeneralLibrary\bin\$Configuration\HH.GeneralLibrary.dll") $dir -Force}
 & (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x86 /langversion:4 /codepage:65001 "/out:$dir\VerifyGeneralLibrary.exe" "/reference:$dir\HH.GeneralLibrary.dll" (Join-Path $PSScriptRoot 'VerifyGeneralLibrary.cs')
 if($LASTEXITCODE -ne 0){throw 'GeneralLibrary verification compile failed'}
 Push-Location $dir
 try{& (Join-Path $dir 'VerifyGeneralLibrary.exe');if($LASTEXITCODE -ne 0){throw "GeneralLibrary verification failed: $variant"}}finally{Pop-Location}
}
if((Get-FileHash (Join-Path $run 'source\general-library-behavior.txt')).Hash -ne (Get-FileHash (Join-Path $run 'legacy\general-library-behavior.txt')).Hash){throw "GeneralLibrary behavior snapshots differ: $run"}
# Delete this run's isolated directory after a successful verification, so tools\bin does not
# grow with every run; a failed run keeps its residue for inspection.
for ($attempt = 0; $attempt -lt 5 -and (Test-Path -LiteralPath $run); $attempt++) {
    Start-Sleep -Milliseconds 300
    Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $run) { throw "Verification artifacts could not be removed: $run" }
Write-Host "GeneralLibrary offline behavior matches original. Artifacts verified and removed: $run"
