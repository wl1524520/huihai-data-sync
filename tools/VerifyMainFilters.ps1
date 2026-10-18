param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $msbuild (Join-Path $repo 'HH.DataCollection.Wifi.sln') /t:Build /p:Configuration=$Configuration /p:Platform=x86 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
# The verifier uses Application.StartupPath for its DB, so never run it in the application directory.
$run = Join-Path $PSScriptRoot ('bin\MainFilters\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $run | Out-Null
$output = Join-Path $repo ('HH.DataCollection.Wifi\bin\' + $Configuration)
Get-ChildItem -LiteralPath $output -Filter '*.dll' | Copy-Item -Destination $run
Copy-Item -LiteralPath (Join-Path $output 'HH.DataCollection.Wifi.exe') -Destination $run
$exe = Join-Path $run 'VerifyMainFilters.exe'
Copy-Item -LiteralPath (Join-Path $output 'HH.DataCollection.Wifi.exe.config') -Destination ($exe + '.config')
& $csc /nologo /target:exe /platform:x86 /langversion:4 "/out:$exe" /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$run\HH.DataCollection.Wifi.exe" "/r:$run\Model.dll" "/r:$run\HH.GeneralLibrary.dll" "/r:$run\System.Data.SQLite.dll" (Join-Path $PSScriptRoot 'VerifyMainFilters.cs')
if ($LASTEXITCODE -ne 0) { throw 'Main filter verifier compilation failed' }
Push-Location $run
try { & $exe; if ($LASTEXITCODE -ne 0) { throw "Main filter verification failed; artifacts: $run" } }
finally { Pop-Location }
Write-Output "Isolated verification artifacts: $run"
