param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
& (Join-Path $framework 'MSBuild.exe') (Join-Path $repo 'HH.DataCollection.Wifi.sln') /t:Build "/p:Configuration=$Configuration" /p:Platform=x86 /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0){throw 'Solution build failed'}
$output=Join-Path $repo "HH.DataCollection.Wifi\bin\$Configuration"
$projects=@{'Model'='HH.TiYu.Model';'DAL'='HH.TiYu.DAL';'BLL'='HH.TiYu.BLL';'Device'='HH.TiYu.Device';'HH.TiYu.SQLite'='HH.TiYu.SQLite';'UI'='HH.TiYu.UI';'LJH.GeneralLibrary'='LJH.GeneralLibrary'}
foreach($name in $projects.Keys){
 if((Get-FileHash (Join-Path $repo ($projects[$name]+"\bin\$Configuration\$name.dll"))).Hash -ne (Get-FileHash (Join-Path $output "$name.dll")).Hash){throw "Source output overwritten: $name"}
}
$run=Join-Path $PSScriptRoot ('bin\RecoveredApiVerification\'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'Mono.Cecil.dll') $run
& (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x86 /langversion:4 /codepage:65001 "/out:$run\VerifyRecoveredApi.exe" /reference:System.Core.dll "/reference:$run\Mono.Cecil.dll" (Join-Path $PSScriptRoot 'VerifyRecoveredApi.cs')
if($LASTEXITCODE -ne 0){throw 'API verification compile failed'}
& (Join-Path $run 'VerifyRecoveredApi.exe') (Join-Path $repo 'lib') $output
if($LASTEXITCODE -ne 0){throw 'Recovered API verification failed'}
