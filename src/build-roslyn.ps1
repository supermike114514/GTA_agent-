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

# Load Roslyn's own dependencies EXPLICITLY, from the Roslyn folder, before touching Roslyn.
#
# 这里刻意**不用** AppDomain.AssemblyResolve 事件处理器 —— 那正是反复出问题的地方。
#
# 走过的弯路（两次都以 StackOverflowException 崩掉宿主进程，且**没有任何编译错误输出**，
# 表现为"进程已终止"，极难定位）：
#
#   1) 处理器把 .NET **引用程序集**目录也当搜索路径。那里全是只有元数据、不能加载执行的
#      程序集；一旦命中，LoadFrom 失败 -> .NET 再次触发 AssemblyResolve -> 又命中
#      -> 无限递归 -> 栈溢出。
#
#   2) 换成用 HashSet 做重入保护。但事件处理器由 .NET 在自己的线程上调用，
#      脚本作用域变量在那个上下文里取不到，守卫自身抛异常 -> .NET 重试 -> 又是递归。
#
# 正解：直接按名字把依赖 LoadFrom 进来。缺哪个就报明确的 FileNotFoundException，
# 而不是一个没有任何线索的栈溢出。
$roslynDeps = @(
    'System.Runtime.CompilerServices.Unsafe.dll',
    'System.Buffers.dll',
    'System.Numerics.Vectors.dll',
    'System.Memory.dll',
    'System.Collections.Immutable.dll',
    'System.Reflection.Metadata.dll',
    'System.Text.Encoding.CodePages.dll',
    'System.Threading.Tasks.Extensions.dll'
)
foreach ($depName in $roslynDeps) {
    $dep = Join-Path $roslyn $depName
    if (Test-Path $dep) {
        try { [void][System.Reflection.Assembly]::LoadFrom($dep) }
        catch { Write-Host ("  [warn] 无法预加载 {0}: {1}" -f $depName, $_.Exception.Message) -ForegroundColor DarkYellow }
    }
}

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
