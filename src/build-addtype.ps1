# build-addtype.ps1 - compile GTA5Buddy.dll IN-PROCESS (no child processes).
#
# Environment constraints this works around:
#   * the sandbox currently denies spawning external processes, so build.ps1 (MSBuild)
#     fails with MSB3883 the moment MSBuild tries to launch csc.exe;
#   * loading Roslyn's assemblies into Windows PowerShell 5.1 trips a type initializer;
#   * Add-Type with -OutputAssembly works but its compiler accepts C# 5 only.
#
# The working approach is the .NET Framework's CodeDom provider, which compiles a SET of
# source files in-process. A set matters: concatenating the files into one blob would place
# each file's `using` directives after the previous file's namespace, which is not legal C#.
#
# Emits ..\build\GTA5Buddy.dll, same as build.ps1.
$ErrorActionPreference = 'Stop'

$src    = 'H:\deepseek\GTA5Buddy\src'
$outDir = 'H:\deepseek\GTA5Buddy\build'
$outDll = Join-Path $outDir 'GTA5Buddy.dll'
$shvdn  = 'H:\deepseek\GTA5Buddy\_runtime\shvdne\ScriptHookVDotNet3.dll'

foreach ($p in @($shvdn, $src)) { if (-not (Test-Path $p)) { throw "missing prerequisite: $p" } }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$files = @(Get-ChildItem $src -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
Write-Host ("PowerShell: {0}" -f $PSVersionTable.PSVersion) -ForegroundColor DarkGray
Write-Host ("sources: " + (($files | ForEach-Object { Split-Path $_ -Leaf }) -join ', ')) -ForegroundColor DarkGray

# On .NET Framework the CodeDom provider lives in System.dll.
Add-Type -AssemblyName System

$provider = New-Object Microsoft.CSharp.CSharpCodeProvider
$params   = New-Object System.CodeDom.Compiler.CompilerParameters

$params.GenerateInMemory        = $false
$params.GenerateExecutable      = $false
$params.OutputAssembly          = $outDll
$params.IncludeDebugInformation = $false
$params.TreatWarningsAsErrors   = $false
$params.CompilerOptions         = '/optimize+ /platform:x64'

[void]$params.ReferencedAssemblies.Add($shvdn)

if (Test-Path $outDll) { Remove-Item $outDll -Force }

Write-Host 'compiling (in-process, CodeDom)...' -ForegroundColor DarkGray
$result = $provider.CompileAssemblyFromFile($params, $files)

if ($result.Errors -and $result.Errors.Count -gt 0) {
    foreach ($e in $result.Errors) {
        if ($e.IsWarning) { Write-Host ("  warn  " + $e.ToString()) -ForegroundColor Yellow }
        else              { Write-Host ("  ERROR " + $e.ToString()) -ForegroundColor Red }
    }
    $errCount = @($result.Errors | Where-Object { -not $_.IsWarning }).Count
    if ($errCount -gt 0) { throw "compilation failed with $errCount error(s)" }
}

if (-not (Test-Path $outDll)) { throw "compiler reported success but $outDll is missing" }
Write-Host ''
Write-Host ("BUILD OK -> {0}  ({1} bytes)" -f $outDll, (Get-Item $outDll).Length) -ForegroundColor Green
