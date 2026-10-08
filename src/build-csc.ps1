# build-csc.ps1 - compile GTA5Buddy.dll by invoking csc.exe directly.
#
# 为什么不再用进程内 Roslyn（build-roslyn.ps1）：
#
#   Roslyn 4.14 在 PowerShell 宿主里加载后会崩。实测到的两级问题：
#
#     1) `CSharpSyntaxTree.ParseText` 抛 TypeInitializationException：
#        "PerTypeValues`1 的类型初始值设定项引发异常"
#        内层是 FileNotFoundException：找不到
#        System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1
#        —— 而 Roslyn 目录里那份 Unsafe 的**文件版本是 6.0.21**，
#        程序集版本对不上，强名称绑定无法满足。
#
#     2) 以前用 AppDomain.AssemblyResolve 处理器去兜底，反而更糟：
#        处理器一旦命中"只有元数据、不能加载执行"的 .NET 引用程序集，
#        LoadFrom 失败会再次触发同一个请求，形成无限递归，
#        整个宿主进程以 StackOverflowException 终止（exit 0xC00000FD），
#        而且**没有任何编译错误输出**，极难定位。
#
#   直接调 csc.exe 绕开了整条"进程内加载编译器"的路径：
#   csc.exe 是自包含的独立进程，不存在程序集版本冲突，错误信息也正常可读。
#
# 注意：csc.exe 是外部进程，需要放开沙箱的进程启动限制。
#
# Emits ..\build\GTA5Buddy.dll
$ErrorActionPreference = 'Stop'

$Root    = 'H:\deepseek\GTA5Buddy'
$src     = Join-Path $Root 'src'
$outDir  = Join-Path $Root 'build'
$outDll  = Join-Path $outDir 'GTA5Buddy.dll'
$shvdn   = Join-Path $Root '_runtime\shvdne\ScriptHookVDotNet3.dll'
$lemon   = Join-Path $Root '_runtime\LemonUI.SHVDN3.dll'
$csc     = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
$netRefs = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'

foreach ($p in @($src, $shvdn, $csc, $netRefs)) {
    if (-not (Test-Path $p)) { throw "缺少前置条件: $p" }
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Only src\*.cs and src\ui\*.cs: obj\ holds generated files (including a second
# AssemblyAttributes.cs) that would collide with our own AssemblyInfo.cs.
$sources = @(Get-ChildItem $src -Filter '*.cs' -File | Sort-Object Name)
$sources += @(Get-ChildItem (Join-Path $src 'ui') -Filter '*.cs' -File -ErrorAction SilentlyContinue | Sort-Object Name)

Write-Host ("sources ({0}):" -f $sources.Count) -ForegroundColor DarkGray
foreach ($s in $sources) { Write-Host ("  " + $s.Name) -ForegroundColor DarkGray }

# net48 facades the code actually uses, plus the two runtime assemblies we bind against.
$wanted = @(
    'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Xml.dll', 'System.Runtime.Versioning.dll',
    'System.Diagnostics.Debug.dll', 'System.Collections.dll'
)

$refArgs = New-Object System.Collections.Generic.List[string]
foreach ($f in $wanted) {
    $p = Join-Path $netRefs $f
    if (Test-Path $p) { $refArgs.Add('/reference:"' + $p + '"') }
}
$refArgs.Add('/reference:"' + $shvdn + '"')

if (Test-Path $lemon) {
    $refArgs.Add('/reference:"' + $lemon + '"')
    Write-Host '  reference: LemonUI.SHVDN3.dll' -ForegroundColor DarkGray
} else {
    Write-Host '  reference: LemonUI 缺失 —— 图形面板会编译失败' -ForegroundColor DarkYellow
}
Write-Host ("references: {0}" -f $refArgs.Count) -ForegroundColor DarkGray

# /nostdlib+ so the reference set above is authoritative; otherwise csc may pick up a
# different framework version from its own config.
$cscArgs = New-Object System.Collections.Generic.List[string]
foreach ($a in @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/platform:x64',
                 '/optimize+', '/deterministic-', '/warn:4', '/utf8output')) {
    $cscArgs.Add($a)
}
$cscArgs.Add('/out:"' + $outDll + '"')
foreach ($r in $refArgs) { $cscArgs.Add($r) }
foreach ($s in $sources) { $cscArgs.Add('"' + $s.FullName + '"') }

if (Test-Path $outDll) { Remove-Item $outDll -Force }

# csc.exe writes diagnostics to stdout/stderr; capture both.
$output = & $csc $cscArgs 2>&1
$code = $LASTEXITCODE

foreach ($line in $output) {
    $text = "$line"
    if ($text -match ': error ')      { Write-Host $text -ForegroundColor Red }
    elseif ($text -match ': warning ') { Write-Host $text -ForegroundColor Yellow }
    else                               { Write-Host $text -ForegroundColor Gray }
}

if ($code -ne 0 -or -not (Test-Path $outDll)) {
    throw "csc.exe 编译失败（exit=$code）"
}

Write-Host ''
Write-Host ("BUILD OK -> {0}  ({1} bytes)" -f $outDll, (Get-Item $outDll).Length) -ForegroundColor Green
