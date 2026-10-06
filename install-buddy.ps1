#Requires -Version 5.1
<#
  install-buddy.ps1 - one-click installer for the GTA V companion mod.

  MUST run elevated (Administrator). GTA V lives under a Steam library that a normal
  user token cannot write to, so the agent could not install these files itself.

  What it does
    1. copies ScriptHookV      -> game folder   (ScriptHookV.dll, dinput8.dll, ...)
    2. copies ScriptHookVDotNet-> game folder   (asi + v3 dll + ini + MinHook)
    3. copies GTA5Buddy.dll    -> game\scripts
    4. with -Junction, links game\scripts at the build output instead, so later
       rebuilds are picked up with no further elevation.
#>
[CmdletBinding()]
param(
    [string]$Game   = 'E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced',
    [string]$Root   = 'H:\deepseek\GTA5Buddy',
    [switch]$Junction,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'

function Say([string]$m, [string]$c = 'Gray') { Write-Host $m -ForegroundColor $c }

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Say ''
Say '=== GTA V 队友模组 安装程序 ===' 'Cyan'
Say ''
if (-not $isAdmin) {
    Say '!! 未以管理员身份运行。' 'Red'
    Say '   本脚本需要管理员权限才能写入游戏目录。' 'Red'
    Say '   请右键本文件 -> 以管理员身份运行，或从管理员 PowerShell 重新执行：' 'Yellow'
    Say "   powershell -ExecutionPolicy Bypass -File `"$PSCommandPath`"" 'Yellow'
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}

if (-not (Test-Path (Join-Path $Game 'GTA5_Enhanced.exe'))) {
    Say "!! 在 $Game 中找不到 GTA5_Enhanced.exe" 'Red'
    Say '   请用 -Game "<你的 GTA V Enhanced 目录>" 指定正确路径。' 'Yellow'
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}

# --- the game locks scripts\GTA5Buddy.dll while it runs -------------------
# Without this check the copy below fails, the script carries on to the end and
# cheerfully reports success - which is exactly how a stale install goes unnoticed.
$modTarget = Join-Path $Game 'scripts\GTA5Buddy.dll'
$lockCheck = { param($path) if (-not (Test-Path $path)) { return $true }
               try { $fs = [IO.File]::Open($path, 'Open', 'ReadWrite', 'None'); $fs.Close(); return $true }
               catch { return $false } }

if (-not (& $lockCheck $modTarget)) {
    Say ''
    Say '!! 目标文件正被 GTA V 占用，无法安装：' 'Red'
    Say "   $modTarget" 'DarkGray'
    Say '' 'Gray'
    Say '   请先完全退出 GTA V（退到桌面 / Steam 里显示未运行），然后重新运行本程序。' 'Yellow'
    Say '   注意：只退到主菜单不算退出，进程仍在，文件仍被锁。' 'Yellow'
    Say ''

    $answer = ''
    if (-not $NoPause) {
        $answer = Read-Host '   退出游戏后按 R 重试 / 直接回车放弃'
    }
    if ($answer -ne 'r' -and $answer -ne 'R') {
        Say '   已放弃，未做任何修改。' 'Yellow'
        if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
        exit 1
    }

    # retry once with a grace window, in case the process is still shutting down
    Say '   等待游戏进程释放文件（最多 60 秒）...' 'White'
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline -and -not (& $lockCheck $modTarget)) { Start-Sleep -Seconds 2 }

    if (-not (& $lockCheck $modTarget)) {
        Say '   文件仍被占用，安装中止，未做任何修改。' 'Red'
        if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
        exit 1
    }
    Say '   文件已可写，继续安装。' 'Green'
}

$shvSrc    = Join-Path $Root '_runtime\shv\bin'
$shvdnSrc  = Join-Path $Root '_runtime\shvdne'
$modDll    = Join-Path $Root 'build\GTA5Buddy.dll'
$backup    = Join-Path $Game ("_mod_backup\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$scriptsDir = Join-Path $Game 'scripts'

foreach ($p in @($shvSrc, $shvdnSrc)) {
    if (-not (Test-Path $p)) { throw "缺少运行时文件: $p" }
}
if (-not (Test-Path $modDll)) { throw "缺少模组文件: $modDll （先运行 src\build.ps1）" }

New-Item -ItemType Directory -Force -Path $backup | Out-Null

$copies = [ordered]@{
    'ScriptHookV.dll'        = Join-Path $shvSrc   'ScriptHookV.dll'
    'dinput8.dll'            = Join-Path $shvSrc   'dinput8.dll'
    'xinput1_4.dll'          = Join-Path $shvSrc   'xinput1_4.dll'
    'args.txt'               = Join-Path $shvSrc   'args.txt'
    'NativeTrainer.asi'      = Join-Path $shvSrc   'NativeTrainer.asi'
    'ScriptHookVDotNet.asi'  = Join-Path $shvdnSrc 'ScriptHookVDotNet.asi'
    'ScriptHookVDotNet3.dll' = Join-Path $shvdnSrc 'ScriptHookVDotNet3.dll'
    'ScriptHookVDotNet.ini'  = Join-Path $shvdnSrc 'ScriptHookVDotNet.ini'
    'MinHook.x64.dll'        = Join-Path $shvdnSrc 'MinHook.x64.dll'
}

Say "[1/3] 安装脚本运行时 -> $Game"
foreach ($name in $copies.Keys) {
    $src = $copies[$name]
    if (-not (Test-Path $src)) { Say "      跳过（源缺失）: $name" 'Yellow'; continue }
    $dst = Join-Path $Game $name
    if (Test-Path $dst) { Copy-Item $dst (Join-Path $backup $name) -Force }
    Copy-Item $src $dst -Force
    Say ("      已安装 {0,-24} {1,9:N0} 字节" -f $name, (Get-Item $dst).Length) 'Green'
}

Say ''
Say '[2/3] 放置模组脚本 -> scripts\'
if (Test-Path $scriptsDir) {
    $item = Get-Item $scriptsDir -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        Say '      scripts 是联接，移除旧联接' 'Yellow'
        cmd /c rmdir "`"$scriptsDir`"" | Out-Null
    }
}

$buildDir = Split-Path -Parent $modDll
if ($Junction) {
    New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
    cmd /c mklink /J "`"$scriptsDir`"" "`"$buildDir`"" | Out-Null
    if (Test-Path (Join-Path $scriptsDir 'GTA5Buddy.dll')) {
        Say "      scripts -> $buildDir （联接，重新编译即时生效）" 'Green'
    } else {
        Say '      联接创建失败，回退为复制' 'Yellow'
        New-Item -ItemType Directory -Force -Path $scriptsDir | Out-Null
        Copy-Item $modDll $scriptsDir -Force
        Say '      已复制 GTA5Buddy.dll' 'Green'
    }
} else {
    New-Item -ItemType Directory -Force -Path $scriptsDir | Out-Null
    Copy-Item $modDll $scriptsDir -Force
    Say ("      已复制 GTA5Buddy.dll  {0:N0} 字节" -f (Get-Item (Join-Path $scriptsDir 'GTA5Buddy.dll')).Length) 'Green'
}

# --- LemonUI: hard dependency of the graphical panel ------------------------
# The menu is drawn through LemonUI (which renders via the game's own Scaleform UI
# system). Without this DLL next to the mod, SHVDN fails to load GTA5Buddy entirely,
# so a missing copy here shows up as "the mod does nothing at all".
$lemonSrc = Join-Path $Root '_runtime\LemonUI.SHVDN3.dll'
$lemonDst = Join-Path $scriptsDir 'LemonUI.SHVDN3.dll'
if (Test-Path $lemonSrc) {
    Copy-Item $lemonSrc $scriptsDir -Force
    Say ("      已复制 LemonUI.SHVDN3.dll  {0:N0} 字节" -f (Get-Item $lemonDst).Length) 'Green'
} else {
    Say '!! 警告：找不到 _runtime\LemonUI.SHVDN3.dll' 'Red'
    Say '   图形面板会因此无法加载（SHVDN 会因缺少依赖而跳过 GTA5Buddy）' 'Red'
    Say '   请确认从完整的仓库副本运行安装，或把该 DLL 手动放进 scripts\' 'Red'
}

# --- verify the mod file actually landed, and matches the build ------------
# A partial install that still prints "完成" is worse than an outright failure, so this
# compares size and hash against the artifact we meant to install.
$installed = Join-Path $scriptsDir 'GTA5Buddy.dll'
if (-not (Test-Path $installed)) {
    Say ''
    Say '!! 校验失败：scripts\GTA5Buddy.dll 不存在，安装没有生效。' 'Red'
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}

$srcLen = (Get-Item $modDll).Length
$dstLen = (Get-Item $installed).Length
$srcHash = (Get-FileHash $modDll -Algorithm SHA256).Hash
$dstHash = (Get-FileHash $installed -Algorithm SHA256).Hash

if ($srcLen -ne $dstLen -or $srcHash -ne $dstHash) {
    Say ''
    Say '!! 校验失败：已安装的 GTA5Buddy.dll 与构建产物不一致，安装没有真正生效。' 'Red'
    Say ("   构建 {0:N0} 字节  vs  已安装 {1:N0} 字节" -f $srcLen, $dstLen) 'Yellow'
    Say '   最常见原因：安装时 GTA V 仍在运行，文件被占用。' 'Yellow'
    Say '   请完全退出游戏后重新运行本程序。' 'Yellow'
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}

Say ''
Say ("      [校验通过] GTA5Buddy.dll {0:N0} 字节，SHA256 与构建产物一致" -f $dstLen) 'Green'

# --- verify the runtime landed too -----------------------------------------
# 只校验 GTA5Buddy.dll 是不够的：真正决定"游戏里有没有反应"的是 dinput8.dll（ASI 加载器）
# 和 ScriptHookVDotNet.asi。它们缺失时 ScriptHookV 根本不会加载任何脚本，
# 表现就是"安装程序说完成，但进游戏毫无反应"——所以这里逐个复核。
$expected = @(
    'ScriptHookV.dll', 'dinput8.dll', 'xinput1_4.dll', 'args.txt', 'NativeTrainer.asi',
    'ScriptHookVDotNet.asi', 'ScriptHookVDotNet3.dll', 'ScriptHookVDotNet.ini', 'MinHook.x64.dll'
)
$missing = @()
foreach ($name in $expected) {
    $p = Join-Path $Game $name
    if (-not (Test-Path $p)) { $missing += $name }
}
if (-not (Test-Path $lemonDst)) { $missing += 'scripts\LemonUI.SHVDN3.dll' }

if ($missing.Count -gt 0) {
    Say ''
    Say '!! 校验失败：以下文件没有出现在游戏目录里：' 'Red'
    foreach ($m in $missing) { Say "     $m" 'Red' }
    Say ''
    Say '   这意味着脚本运行时不完整，游戏里不会有任何反应。' 'Yellow'
    Say '   最常见的原因：' 'Yellow'
    Say '     1) 杀毒软件把这些文件当木马删掉了（dinput8.dll 是重点查杀对象）' 'Yellow'
    Say '        -> 在杀软里给游戏目录加白名单，然后重新安装' 'Yellow'
    Say '     2) 安装时 GTA V 仍在运行，文件被占用' 'Yellow'
    Say '        -> 完全退出游戏后重新运行本程序' 'Yellow'
    Say ''
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}
Say ("      [校验通过] 9 个运行时文件 + LemonUI 依赖全部就位") 'Green'

# config template: placed in the GAME ROOT, which is proven writable by the game process
# (ScriptHookV.log lives there). Only seeded when absent so user edits are never clobbered.
$iniSrc = Join-Path $Root 'GTA5Buddy.ini'
$iniDst = Join-Path $Game 'GTA5Buddy.ini'
if (Test-Path $iniSrc) {
    if (Test-Path $iniDst) {
        Say '      已存在 GTA5Buddy.ini，保留你的配置不覆盖' 'DarkGray'
    } else {
        Copy-Item $iniSrc $iniDst -Force
        Say '      已放置 GTA5Buddy.ini（字体/位置/HUD 开关）' 'Green'
    }
}

Say ''
Say '[3/3] 备份与校验'
Say "      被替换文件的备份: $backup" 'DarkGray'
Say ''
Say '=== 安装完成 ===' 'Cyan'
Say ''
Say '############################################################' 'Yellow'
Say '#  仅限【故事模式】(Story Mode)                            #' 'Yellow'
Say '#                                                          #' 'Yellow'
Say '#  这是本模组的硬性边界，共三层保护：                      #' 'Yellow'
Say '#    1) ScriptHookV 检测到你进入 GTA Online 会直接关闭游戏 #' 'Yellow'
Say '#    2) 模组自身有模式闸门：判定到线上会话时拒绝装载，     #' 'Yellow'
Say '#       队友不会生成、不会下任何任务                       #' 'Yellow'
Say '#    3) 本安装脚本在此处警告，并提供一键卸载              #' 'Yellow'
Say '#                                                          #' 'Yellow'
Say '#  要在 GTA Online 玩：把 dinput8.dll 改名为 dinput8.dll.off#' 'Yellow'
Say '#  或运行同目录的 uninstall-buddy.ps1 -RemoveRuntime        #' 'Yellow'
Say '############################################################' 'Yellow'
Say ''
Say '接下来：' 'White'
Say '  1. 启动 GTA V Enhanced，进入【故事模式】'
Say '  2. 进游戏后看左下角是否有 "队友模组已加载" 提示'
Say '     模式闸门的判定结果会写进 scripts\GTA5Buddy.log'
Say '  3. 若没有提示，运行 tools\check-logs.ps1 查看全部日志'
Say ''
Say '按键：J 召唤/收回   K 待命/跟随   E 或 H 施救   F4 打开 SHVDN 控制台' 'White'
Say ''
Say "卸载：powershell -ExecutionPolicy Bypass -File `"$(Join-Path $Root 'uninstall-buddy.ps1')`" -RemoveRuntime" 'DarkGray'
Say '      只想临时停用：把游戏目录里的 dinput8.dll 改名为 dinput8.dll.off' 'DarkGray'
Say ''

if (-not $NoPause) { Read-Host '按回车结束' | Out-Null }
