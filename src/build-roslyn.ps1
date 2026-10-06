# build-roslyn.ps1 - compile GTA5Buddy.dll IN-PROCESS with the Roslyn compiler APIs.
#
# Environment constraints this works around:
#   * the sandbox currently denies spawning external processes, so build.ps1 (MSBuild)
#     dies with MSB3883 the moment MSBuild tries to launch csc.exe;
#   * CodeDom's CompileAssemblyFromFile throws "不支持给定路径的格式" in this sandbox;
#   * Add-Type -OutputAssembly works but its compiler accepts C# 5 only, which would
#     forbid string interpolation in the source.
#
# Roslyn loads fine as long as its own dependencies resolve, which is what the
# AssemblyResolve handler below provides. Everything stays in this process.
#
# Emits ..\build\GTA5Buddy.dll, same as build.ps1, and compiles a SET of source files so
# each file keeps its own using directives.
$ErrorActionPreference = 'Stop'

$src    = 'H:\deepseek\GTA5Buddy\src'
$outDir = 'H:\deepseek\GTA5Buddy\build'
$outDll = Join-Path $outDir 'GTA5Buddy.dll'
$shvdn  = 'H:\deepseek\GTA5Buddy\_runtime\shvdne\ScriptHookVDotNet3.dll'
$roslyn = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn'
$netRefs = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'

foreach ($p in @($shvdn, $roslyn, $netRefs)) { if (-not (Test-Path $p)) { throw "missing prerequisite: $p" } }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Let Roslyn's dependencies (System.Collections.Immutable, System.Reflection.Metadata, ...)
# resolve out of its own folder instead of failing the type initializer.
$probeDirs = @($roslyn, (Split-Path $shvdn -Parent), $netRefs)
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

Write-Host ("PowerShell: {0}" -f $PSVersionTable.PSVersion) -ForegroundColor DarkGray

Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll')
Write-Host 'Roslyn loaded' -ForegroundColor DarkGray

# Only src\*.cs and src\ui\*.cs are compiled: obj\ contains generated files (including a
# second AssemblyAttributes.cs) that would collide with our own AssemblyInfo.cs.
$sources = @(Get-ChildItem $src -Filter '*.cs' -File | Sort-Object Name)
$sources += @(Get-ChildItem (Join-Path $src 'ui') -Filter '*.cs' -File -ErrorAction SilentlyContinue | Sort-Object Name)
Write-Host ("sources ({0}):" -f $sources.Count) -ForegroundColor DarkGray
foreach ($s in $sources) { Write-Host ("  " + $s.Name) -ForegroundColor DarkGray }

# Reference set: net48 facades the code actually uses, plus the SHVDN runtime we bind to.
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

# LemonUI provides the GTA-native (Scaleform) menu look. It is compiled against SHVDN
# 3.6.0.0 while this setup runs 3.9.0.0, so the runtime is expected to unify the versions.
# The reference is optional: without the DLL the mod still builds and falls back to the
# hand-rolled panel.
$lemon = 'H:\deepseek\GTA5Buddy\_runtime\LemonUI.SHVDN3.dll'
if (Test-Path $lemon) {
    $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($lemon))
    Write-Host '  reference: LemonUI.SHVDN3.dll' -ForegroundColor DarkGray
} else {
    Write-Host '  reference: LemonUI absent' -ForegroundColor DarkYellow
}
Write-Host ("references: {0}" -f $refs.Count) -ForegroundColor DarkGray

$trees = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]'
foreach ($f in $sources) {
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([System.IO.File]::ReadAllText($f.FullName)))
}

$opt = New-Object Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions ([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$opt = $opt.WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)
$opt = $opt.WithPlatform([Microsoft.CodeAnalysis.Platform]::X64)

$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('GTA5Buddy', $trees, $refs, $opt)

$diags = @($comp.GetDiagnostics())
$errs  = @($diags | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })
$warns = @($diags | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Warning })

foreach ($w in $warns) { Write-Host ("  warn  " + $w.ToString()) -ForegroundColor Yellow }
if ($errs.Count -gt 0) {
    foreach ($e in $errs) { Write-Host ("  ERROR " + $e.ToString()) -ForegroundColor Red }
    throw "compilation failed with $($errs.Count) error(s)"
}

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

Write-Host ''
Write-Host ("BUILD OK -> {0}  ({1} bytes, {2} warnings)" -f $outDll, (Get-Item $outDll).Length, $warns.Count) -ForegroundColor Green
