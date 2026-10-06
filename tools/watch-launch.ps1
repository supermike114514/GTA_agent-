# watch-launch.ps1 - wait for the player to launch the game, then report what the logs say.
#
# Reads (never writes) the game folder. Story mode data lives in
# %USERPROFILE%\Documents\Rockstar Games\GTA V and is untouched.
[CmdletBinding()]
param(
    [string]$Game = 'E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced',
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = 'Continue'
function Say([string]$m, [string]$c = 'Gray') { Write-Host $m -ForegroundColor $c }

$shvLog  = Join-Path $Game 'ScriptHookV.log'
$shvdnLog = Join-Path $Game 'ScriptHookVDotNet.log'
$modLog  = Join-Path $Game 'scripts\GTA5Buddy.log'

# record pre-existing state so we can tell "new" from "stale"
$before = @{}
foreach ($p in @($shvLog, $shvdnLog, $modLog)) {
    $before[$p] = if (Test-Path $p) { (Get-Item $p).LastWriteTime } else { $null }
}

Say ''
Say '=== 等待你启动 GTA V Enhanced（故事模式）===' 'Cyan'
Say "  监视中，最长 $TimeoutSeconds 秒。现在请手动打开游戏。" 'White'
Say '  （Steam 未运行时请先开 Steam；进游戏后选【故事模式】）' 'DarkGray'
Say ''

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$seenGame = $false
$lastSize = @{}
$decisionAt = $null

while ((Get-Date) -lt $deadline) {
    # game process?
    $proc = Get-Process | Where-Object { $_.ProcessName -match '^GTA5|^PlayGTAV' }
    if ($proc -and -not $seenGame) {
        $seenGame = $true
        Say ("  检测到游戏进程: {0} (PID {1})" -f ($proc.ProcessName -join ','), ($proc.Id -join ',')) 'Green'
    }

    # any log appear or grow?
    foreach ($p in @($shvLog, $shvdnLog, $modLog)) {
        if (Test-Path $p) {
            $now = (Get-Item $p).LastWriteTime
            $size = (Get-Item $p).Length
            $isNew = ($before[$p] -eq $null) -or ($now -ne $before[$p])
            if ($isNew -and ($lastSize[$p] -ne $size)) {
                $lastSize[$p] = $size
                Say ("  日志活动: {0}  ({1} 字节)" -f (Split-Path $p -Leaf), $size) 'Yellow'
            }
        }
    }

    # Stop only once the story is fully told: ScriptHookV has written its verdict AND
    # either SHVDN logged in or the game process is gone. Breaking on "Success" alone
    # exits before ScriptHookVDotNet has had a chance to initialise.
    if (Test-Path $shvLog) {
        $txt = Get-Content $shvLog -Raw -ErrorAction SilentlyContinue
        $shvDecided = ($txt -match 'Unknown game version') -or ($txt -match 'Success')
        if ($shvDecided) {
            if (Test-Path $shvdnLog) { break }
            if (-not $proc) { break }
            if (-not $decisionAt) { $decisionAt = Get-Date }
            # give SHVDN a grace window while the game is still up
            if (((Get-Date) - $decisionAt).TotalSeconds -gt 45) { break }
        }
    }

    Start-Sleep -Seconds 3
}

Say ''
Say '=== ScriptHookV.log ===' 'Cyan'
if (Test-Path $shvLog) {
    Get-Content $shvLog | ForEach-Object { Write-Host "  $_" }
} else {
    Say '  未生成 —— ScriptHookV 没有初始化（游戏没启动到主菜单，或 asi loader 没生效）' 'Yellow'
}

Say ''
Say '=== 判定 ===' 'Cyan'
if (Test-Path $shvLog) {
    $txt = Get-Content $shvLog -Raw
    if ($txt -match 'Unknown game version') {
        Say '  ❌ SHV 拒绝了游戏版本 —— 需要等 SHV 更新到 1.0.1158.16，或改用 Legacy 版' 'Red'
    } elseif ($txt -match 'Success') {
        $ver = [regex]::Match($txt, 'game version is (\S+)').Groups[1].Value
        Say "  ✅ SHV 初始化成功，识别的游戏版本: $ver" 'Green'
        if ($txt -match 'Registering script') {
            Say '     asi 已注册（见上方 Registering script）。SHVDN 若仍无日志，说明它没跑到初始化。' 'DarkGray'
        }
    } else {
        Say '  ⚠ 日志存在但没有明确的成功/失败行，请把上面内容发我' 'Yellow'
    }
} else {
    Say '  无法判定：SHV 日志未生成' 'Yellow'
}

Say ''
Say '=== ScriptHookVDotNet.log（是否存在，说明 asi 加载器生效）===' 'Cyan'
if (Test-Path $shvdnLog) { Get-Content $shvdnLog -Tail 30 | ForEach-Object { Write-Host "  $_" } }
else { Say '  未生成' 'Yellow' }

Say ''
Say '=== 模组模式闸门日志 scripts\GTA5Buddy.log ===' 'Cyan'
if (Test-Path $modLog) { Get-Content $modLog -Tail 30 | ForEach-Object { Write-Host "  $_" } }
else { Say '  未生成 —— 模组没被 SHVDN 加载（通常是上一步 SHV 就没成功）' 'Yellow' }
Say ''
