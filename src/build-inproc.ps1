# build-inproc.ps1 - compile GTA5Buddy.dll with Roslyn IN-PROCESS.
#
# Why: the sandbox can deny spawning external processes (MSBuild then fails with
# MSB3883 "拒绝访问" when it tries to launch csc.exe). Loading the Roslyn compiler
# into this PowerShell process needs no child process at all.
$ErrorActionPreference = 'Stop'

$src     = 'H:\deepseek\GTA5Buddy\src'
$outDir  = 'H:\deepseek\GTA5Buddy\build'
$outDll  = Join-Path $outDir 'GTA5Buddy.dll'
$shvdn   = 'H:\deepseek\GTA5Buddy\_runtime\shvdne\ScriptHookVDotNet3.dll'
$roslyn  = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn'
$netRefs = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'

foreach ($p in @($shvdn, (Join-Path $roslyn 'Microsoft.CodeAnalysis.dll'), (Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll'), $netRefs)) {
    if (-not (Test-Path $p)) { throw "missing prerequisite: $p" }
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll')

$sources = Get-ChildItem $src -Filter '*.cs' -File | Sort-Object Name
Write-Host ("sources: " + (($sources | ForEach-Object { $_.Name }) -join ', '))

# net48 reference assemblies + the SHVDN runtime we bind against
$refFiles = @(
    'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Xml.dll', 'System.Runtime.Versioning.dll',
    'System.Diagnostics.Debug.dll'
)

$refs = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]'
foreach ($f in $refFiles) {
    $p = Join-Path $netRefs $f
    if (Test-Path $p) {
        $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($p))
    } else {
        Write-Host "  (skip missing ref $f)" -ForegroundColor DarkGray
    }
}
$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($shvdn))

$trees = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]'
foreach ($f in $sources) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($text))
}

$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$options = $options.WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)
$options = $options.WithPlatform([Microsoft.CodeAnalysis.Platform]::X64)

$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('GTA5Buddy', $trees, $refs, $options)

$errors = @()
$warnings = @()
foreach ($d in $compilation.GetDiagnostics()) {
    if ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error)   { $errors   += $d }
    elseif ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Warning) { $warnings += $d }
}

if ($warnings.Count) {
    Write-Host "--- warnings ---" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host ("  " + $_.ToString()) -ForegroundColor Yellow }
}

if ($errors.Count) {
    Write-Host "--- errors ---" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host ("  " + $_.ToString()) -ForegroundColor Red }
    throw "compilation failed with $($errors.Count) error(s)"
}

if (Test-Path $outDll) { Remove-Item $outDll -Force }
$fs = [System.IO.File]::Open($outDll, [System.IO.FileMode]::Create)
try {
    $emit = $compilation.Emit($fs)
} finally { $fs.Close() }

if (-not $emit.Success) {
    Write-Host '--- emit errors ---' -ForegroundColor Red
    $emit.Diagnostics | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error } | ForEach-Object { Write-Host ("  " + $_.ToString()) -ForegroundColor Red }
    throw 'emit failed'
}

Write-Host ''
Write-Host ("BUILD OK -> {0}  ({1} bytes, {2} warnings)" -f $outDll, (Get-Item $outDll).Length, $warnings.Count) -ForegroundColor Green
