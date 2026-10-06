#Requires -Version 5.1
<#
  uninstall-buddy.ps1 - remove the GTA5Buddy companion mod and stop the script runtime.

  MUST run elevated, for the same reason the installer does: the game lives under a
  Steam library that a normal user token cannot write to.

  -RemoveRuntime also takes ScriptHookV / ScriptHookVDotNet back out, and restores any
  file the installer had backed up under _mod_backup.
#>
[CmdletBinding()]
param(
    [string]$Game = 'E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced',
    [switch]$RemoveRuntime,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
function Say([string]$m, [string]$c = 'Gray') { Write-Host $m -ForegroundColor $c }

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Say ''
    Say '!! 需要管理员权限。请右键本文件 -> 以管理员身份运行。' 'Red'
    Say ''
    if (-not $NoPause) { Read-Host '按回车退出' | Out-Null }
    exit 1
}

Say ''
Say '=== GTA V 队友模组 卸载程序 ===' 'Cyan'

# 1. the script itself
$scripts = Join-Path $Game 'scripts'
$modDll = Join-Path $scripts 'GTA5Buddy.dll'
if (Test-Path $modDll) {
    Remove-Item $modDll -Force
    Say "已删除 $modDll" 'Green'
}
$blog = Join-Path $scripts 'GTA5Buddy.log'
if (Test-Path $blog) { Remove-Item $blog -Force; Say "已删除 $blog" 'Green' }

# a junction pointing at the build output is a different beast - remove the link, not the target
$item = Get-Item $scripts -Force -ErrorAction SilentlyContinue
if ($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    cmd /c rmdir "`"$scripts`"" | Out-Null
    Say "已移除 scripts 目录联接" 'Green'
}

if (-not $RemoveRuntime) {
    Say ''
    Say '脚本已移除。ScriptHookV / ScriptHookVDotNet 仍保留在游戏目录：' 'Yellow'
    Say '  - 只想临时停用：把 dinput8.dll 改名为 dinput8.dll.off' 'Yellow'
    Say '  - 想彻底移除：再运行一次本脚本并加 -RemoveRuntime' 'Yellow'
    Say ''
    if (-not $NoPause) { Read-Host '按回车结束' | Out-Null }
    exit 0
}

# 2. the runtime
Say ''
Say '移除脚本运行时...' 'White'
foreach ($f in 'ScriptHookVDotNet.asi', 'ScriptHookVDotNet3.dll', 'ScriptHookVDotNet.ini',
               'MinHook.x64.dll', 'ScriptHookV.dll', 'dinput8.dll', 'xinput1_4.dll',
               'args.txt', 'NativeTrainer.asi') {
    $p = Join-Path $Game $f
    if (Test-Path $p) { Remove-Item $p -Force; Say "  已删除 $f" 'Green' }
}

# 3. restore the newest backup, if the installer made one
$backupRoot = Join-Path $Game '_mod_backup'
if (Test-Path $backupRoot) {
    $newest = Get-ChildItem $backupRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($newest) {
        Say ''
        Say "发现备份 $($newest.FullName)，正在恢复被替换的文件..." 'White'
        Get-ChildItem $newest.FullName -File | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $Game $_.Name) -Force
            Say "  已恢复 $($_.Name)" 'Green'
        }
    }
}

Say ''
Say '卸载完成。' 'Cyan'
Say ''
if (-not $NoPause) { Read-Host '按回车结束' | Out-Null }
