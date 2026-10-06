# install-runtime.ps1
# Installs ScriptHookV + ScriptHookVDotNet Enhanced into the GTA V Enhanced folder.
# Backs up any file it is about to overwrite. Safe to re-run.

$ErrorActionPreference = 'Stop'

$Game   = 'E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced'
$Shv    = 'H:\deepseek\GTA5Buddy\_runtime\shv\bin'
$Shvdne = 'H:\deepseek\GTA5Buddy\_runtime\shvdne'
$Stamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
$Backup = Join-Path $Game "_mod_backup\$Stamp"

if (-not (Test-Path $Game)) { throw "game folder not found: $Game" }

# --- sanity: this must be the Enhanced layout ---
if (-not (Test-Path (Join-Path $Game 'GTA5_Enhanced.exe'))) {
    throw "GTA5_Enhanced.exe not found in $Game - wrong folder for the Enhanced hook"
}

New-Item -ItemType Directory -Force -Path $Backup | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $Game 'scripts') | Out-Null

# name -> source path
$files = [ordered]@{
    'ScriptHookV.dll'         = Join-Path $Shv 'ScriptHookV.dll'
    'dinput8.dll'             = Join-Path $Shv 'dinput8.dll'
    'xinput1_4.dll'           = Join-Path $Shv 'xinput1_4.dll'
    'args.txt'                = Join-Path $Shv 'args.txt'
    'NativeTrainer.asi'       = Join-Path $Shv 'NativeTrainer.asi'
    'ScriptHookVDotNet.asi'   = Join-Path $Shvdne 'ScriptHookVDotNet.asi'
    'ScriptHookVDotNet3.dll'  = Join-Path $Shvdne 'ScriptHookVDotNet3.dll'
    'ScriptHookVDotNet.ini'   = Join-Path $Shvdne 'ScriptHookVDotNet.ini'
    'MinHook.x64.dll'         = Join-Path $Shvdne 'MinHook.x64.dll'
}

$installed = @()
foreach ($name in $files.Keys) {
    $src = $files[$name]
    if (-not (Test-Path $src)) { Write-Warning "missing source, skipped: $src"; continue }
    $dst = Join-Path $Game $name
    if (Test-Path $dst) {
        Copy-Item $dst (Join-Path $Backup $name) -Force
    }
    Copy-Item $src $dst -Force
    $installed += [pscustomobject]@{
        File  = $name
        Bytes = (Get-Item $dst).Length
    }
}

$installed | Format-Table -AutoSize

Write-Output ''
Write-Output "backup of replaced files : $Backup"
Write-Output "scripts folder           : $(Join-Path $Game 'scripts')"
Write-Output ''
Write-Output 'Installed. Launch story mode, then check:'
Write-Output "  $(Join-Path $Game 'ScriptHookV.log')"
Write-Output "  $(Join-Path $Game 'ScriptHookVDotNet.log')"
