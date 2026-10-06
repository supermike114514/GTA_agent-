# build.ps1 - compile GTA5Buddy.dll with the VS 2022 MSBuild and net48 reference
# assemblies that ship with the machine (no NuGet, no network).
$ErrorActionPreference = 'Stop'

$root    = Split-Path -Parent $PSScriptRoot
$project = Join-Path $PSScriptRoot 'GTA5Buddy.csproj'
$out     = Join-Path $root 'build\GTA5Buddy.dll'

$candidates = @(
    'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
)
$msbuild = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild.exe not found (Visual Studio 2022 required)' }

if (Test-Path $out) { Remove-Item $out -Force }

& $msbuild $project /p:Configuration=Release /p:Platform=x64 /p:RestorePackages=false /v:m /nologo
$code = $LASTEXITCODE

if ($code -ne 0) { throw "build failed with exit code $code" }

if (-not (Test-Path $out)) { throw "build reported success but $out is missing" }

Write-Output ''
Write-Output "BUILD OK -> $out  ($((Get-Item $out).Length) bytes)"
