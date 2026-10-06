# listtypes.ps1 - list public types in a script assembly (loader sanity check)
param([Parameter(Mandatory = $true)][string]$Assembly)

$dir = Split-Path -Parent (Resolve-Path $Assembly)
$shvdn = Join-Path $dir 'ScriptHookVDotNet3.dll'
if (-not (Test-Path $shvdn)) {
    $shvdn = 'H:\deepseek\GTA5Buddy\_runtime\shvdne\ScriptHookVDotNet3.dll'
}
# preload the dependency so GetExportedTypes can resolve base types
[void][System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($shvdn))

$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Assembly))
Write-Output "assembly: $($asm.GetName().Name) $($asm.GetName().Version)"
Write-Output '--- public types ---'
try {
    $asm.GetExportedTypes() | ForEach-Object {
        $base = $_.BaseType
        $isScript = $false
        while ($base) {
            if ($base.FullName -eq 'GTA.Script') { $isScript = $true; break }
            $base = $base.BaseType
        }
        "{0}  (base={1})  Script={2}" -f $_.FullName, $_.BaseType.FullName, $isScript
    }
} catch {
    Write-Output "could not enumerate exported types: $($_.Exception.Message)"
}
