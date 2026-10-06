# build-lemontest.ps1 - compile ONLY the minimal LemonUI test script.
#
# Purpose: answer one question with the least possible code - can LemonUI render a
# graphical menu on this machine? Everything hand-rolled has failed so far, so this
# avoids adding more code to the wrong approach.
#
# Same in-process Roslyn technique as build-roslyn.ps1 (the sandbox denies spawning
# external processes, so MSBuild/csc cannot be launched).
#
# Emits ..\build\GTA5BuddyTest.dll
$ErrorActionPreference = 'Stop'

$srcDir = 'H:\deepseek\GTA5Buddy\src'
$source = Join-Path $srcDir 'LemonTest.cs'
$outDir = 'H:\deepseek\GTA5Buddy\build'
$outDll = Join-Path $outDir 'GTA5BuddyTest.dll'
$shvdn  = 'H:\deepseek\GTA5Buddy\_runtime\shvdne\ScriptHookVDotNet3.dll'
$lemon  = 'H:\deepseek\GTA5Buddy\_runtime\LemonUI.SHVDN3.dll'
$roslyn = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn'
$netRefs = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'

foreach ($p in @($source, $shvdn, $lemon, $roslyn, $netRefs)) {
    if (-not (Test-Path $p)) { throw "missing prerequisite: $p" }
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$probeDirs = @($roslyn, (Split-Path $shvdn -Parent), (Split-Path $lemon -Parent), $netRefs)
$onResolve = [System.ResolveEventHandler] {
    param($s, $e)
    $simple = ($e.Name -split ',')[0]
    foreach ($d in $probeDirs) {
        $cand = Join-Path $d ($simple + '.dll')
        if (Test-Path $cand) { return [System.Reflection.Assembly]::LoadFrom($cand) }
    }
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($onResolve)

Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll')

$wanted = @(
    'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Xml.dll', 'System.Runtime.Versioning.dll',
    'System.Diagnostics.Debug.dll', 'System.Collections.dll'
)

$refs = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]'
foreach ($f in $wanted) {
    $p = Join-Path $netRefs $f
    if (Test-Path $p) { $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($p)) }
}
$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($shvdn))
$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($lemon))

$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([System.IO.File]::ReadAllText($source))
$trees = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]'
$trees.Add($tree)

$opt = New-Object Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions ([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$opt = $opt.WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)
$opt = $opt.WithPlatform([Microsoft.CodeAnalysis.Platform]::X64)

$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('GTA5BuddyTest', $trees, $refs, $opt)

$diags = @($comp.GetDiagnostics())
$errs  = @($diags | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })
foreach ($e in $errs) { Write-Host ("  ERROR " + $e.ToString()) -ForegroundColor Red }
if ($errs.Count -gt 0) { throw "compilation failed with $($errs.Count) error(s)" }

if (Test-Path $outDll) { Remove-Item $outDll -Force }
$fs = [System.IO.File]::Open($outDll, [System.IO.FileMode]::Create)
try   { $emit = $comp.Emit($fs) }
finally { $fs.Close() }

if (-not $emit.Success) {
    foreach ($e in ($emit.Diagnostics | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })) {
        Write-Host ("  EMIT  " + $e.ToString()) -ForegroundColor Red
    }
    throw 'emit failed'
}

Write-Host ("BUILD OK -> {0}  ({1} bytes)" -f $outDll, (Get-Item $outDll).Length) -ForegroundColor Green
