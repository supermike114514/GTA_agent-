# check-logs.ps1 - post-install verification for GTA5Buddy.
# Shows the version match question (the top risk), the mode-gate verdict, and the logs.
[CmdletBinding()]
param(
    [string]$Game = 'E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced'
)

$ErrorActionPreference = 'Continue'

function Head([string]$t) { Write-Host ''; Write-Host "=== $t ===" -ForegroundColor Cyan }

Head '版本对照（最关键）'
$exe = Join-Path $Game 'GTA5_Enhanced.exe'
if (Test-Path $exe) {
    $i = (Get-Item $exe).VersionInfo
    Write-Host ("  GTA5_Enhanced.exe : {0}" -f $i.FileVersion)
} else {
    Write-Host "  找不到 $exe，请用 -Game 指定游戏目录" -ForegroundColor Red
}

$shv = Join-Path $Game 'ScriptHookV.dll'
if (Test-Path $shv) {
    $s = (Get-Item $shv).VersionInfo
    Write-Host ("  ScriptHookV.dll   : {0}" -f $s.FileVersion)
    if ($s.FileVersion -notlike "*1158.16*" -and $i -and $i.FileVersion -like "*1158.16*") {
        Write-Host '  !! ScriptHookV 版本号比游戏旧。SHV 按 build 精确匹配，' -ForegroundColor Yellow
        Write-Host '     若 ScriptHookV.log 出现 unknown game version，就是这里卡住的。' -ForegroundColor Yellow
        Write-Host '     注意 launcher 版本号与脚本 build 号不一定同步，以日志为准。' -ForegroundColor Yellow
    }
} else {
    Write-Host '  ScriptHookV.dll   : 未安装' -ForegroundColor Yellow
}

Head '安装状态'
foreach ($f in 'dinput8.dll', 'args.txt', 'xinput1_4.dll', 'ScriptHookVDotNet.asi', 'ScriptHookVDotNet3.dll', 'MinHook.x64.dll') {
    $p = Join-Path $Game $f
    Write-Host ("  {0,-24} {1}" -f $f, $(if (Test-Path $p) { '已安装' } else { '缺失' })) -ForegroundColor $(if (Test-Path $p) { 'Green' } else { 'Yellow' })
}
$mod = Join-Path $Game 'scripts\GTA5Buddy.dll'
Write-Host ("  {0,-24} {1}" -f 'scripts\GTA5Buddy.dll', $(if (Test-Path $mod) { "已安装 ($((Get-Item $mod).Length) 字节)" } else { '缺失 - 先运行 一键安装.bat' })) -ForegroundColor $(if (Test-Path $mod) { 'Green' } else { 'Yellow' })

Head '模式闸门判定（故事模式 / 线上）'
$blog = Join-Path $Game 'scripts\GTA5Buddy.log'
if (Test-Path $blog) {
    Write-Host "  日志: $blog" -ForegroundColor DarkGray
    Get-Content $blog -Tail 25 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host '  还没有 GTA5Buddy.log —— 说明模组还没在游戏里跑过一次。' -ForegroundColor DarkGray
    Write-Host '  进一次故事模式后这个文件会自动生成，里面会写明 mode: STORY 或 NOT STORY。' -ForegroundColor DarkGray
}

Head 'ScriptHookV.log'
$log = Join-Path $Game 'ScriptHookV.log'
if (Test-Path $log) {
    Write-Host "  $log  (最后写入 $((Get-Item $log).LastWriteTime))" -ForegroundColor DarkGray
    Get-Content $log -Tail 25 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host '  不存在 —— 说明脚本运行时还没被加载过（或从未启动过游戏）' -ForegroundColor DarkGray
}

Head 'ScriptHookVDotNet.log'
$mlog = Join-Path $Game 'ScriptHookVDotNet.log'
if (Test-Path $mlog) {
    Write-Host "  $mlog  (最后写入 $((Get-Item $mlog).LastWriteTime))" -ForegroundColor DarkGray
    Get-Content $mlog -Tail 40 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host '  不存在' -ForegroundColor DarkGray
}

Head '线上安全提醒'
Write-Host '  这套东西只能用于故事模式，进 GTA Online 时 ScriptHookV 会直接关闭游戏。' -ForegroundColor Yellow
Write-Host '  要在线上玩：把 dinput8.dll 改名为 dinput8.dll.off（脚本运行时整体停用），' -ForegroundColor Yellow
Write-Host '  或运行 uninstall-buddy.ps1 彻底移除。' -ForegroundColor Yellow
Write-Host ''
