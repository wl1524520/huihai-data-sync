param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference = 'Stop'
# Call only for an isolated verification fixture, never the original lib directory.
$fixture = [IO.Path]::GetFullPath($Directory)
$fixtureRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'bin')) + [IO.Path]::DirectorySeparatorChar
if (-not $fixture.StartsWith($fixtureRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Legacy migration requires a tools/bin fixture' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mono.Cecil.dll') -Destination $fixture -Force
$exe = Join-Path $fixture 'LegacyApiNames.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:exe /main:LegacyApiNames /platform:x86 /langversion:4 /codepage:65001 "/out:$exe" /reference:System.Core.dll "/reference:$fixture\Mono.Cecil.dll" (Join-Path $PSScriptRoot 'LegacyApiNames.cs')
if ($LASTEXITCODE -ne 0) { throw 'Legacy name migration compile failed' }
$assemblies = @('Model','BLL','DAL','Device','HH.TiYu.SQLite','UI','LJH.GeneralLibrary') | ForEach-Object { Join-Path $fixture ($_ + '.dll') } | Where-Object { Test-Path -LiteralPath $_ }
& $exe @assemblies
if ($LASTEXITCODE -ne 0) { throw 'Legacy fixture name migration failed' }
