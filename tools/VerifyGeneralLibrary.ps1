param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'LJH.GeneralLibrary\LJH.GeneralLibrary.csproj') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0){throw 'GeneralLibrary build failed'}
$run=Join-Path $PSScriptRoot ('bin\GeneralLibraryVerification\'+[Guid]::NewGuid().ToString('N'))
foreach($variant in @('source','legacy')){
 $dir=Join-Path $run $variant
 New-Item -ItemType Directory -Path $dir -Force | Out-Null
 Get-ChildItem (Join-Path $repo 'lib') -Filter '*.dll' -File | Copy-Item -Destination $dir
 if($variant -eq 'source'){Copy-Item (Join-Path $repo "LJH.GeneralLibrary\bin\$Configuration\LJH.GeneralLibrary.dll") $dir -Force}
 & (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x86 /langversion:4 /codepage:65001 "/out:$dir\VerifyGeneralLibrary.exe" "/reference:$dir\LJH.GeneralLibrary.dll" (Join-Path $PSScriptRoot 'VerifyGeneralLibrary.cs')
 if($LASTEXITCODE -ne 0){throw 'GeneralLibrary verification compile failed'}
 Push-Location $dir
 try{& (Join-Path $dir 'VerifyGeneralLibrary.exe');if($LASTEXITCODE -ne 0){throw "GeneralLibrary verification failed: $variant"}}finally{Pop-Location}
}
if((Get-FileHash (Join-Path $run 'source\general-library-behavior.txt')).Hash -ne (Get-FileHash (Join-Path $run 'legacy\general-library-behavior.txt')).Hash){throw "GeneralLibrary behavior snapshots differ: $run"}
Write-Host "GeneralLibrary offline behavior matches original. $run"
