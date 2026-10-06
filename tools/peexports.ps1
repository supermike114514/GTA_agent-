# peexports.ps1 - read the PE export table without external tools.
param([Parameter(Mandatory = $true)][string]$Path)

$b = [System.IO.File]::ReadAllBytes($Path)

function U32([int]$o) { [BitConverter]::ToUInt32($b, $o) }
function U16([int]$o) { [BitConverter]::ToUInt16($b, $o) }

$peOff = U32 0x3C
if ((U32 $peOff) -ne 0x00004550) { throw 'not a PE file' }

$coff = $peOff + 4
$numSec = U16 ($coff + 2)
$optSize = U16 ($coff + 16)
$optOff = $coff + 20
$magic = U16 $optOff
$plus = ($magic -eq 0x20b)
$ddOff = $optOff + $(if ($plus) { 112 } else { 96 })

# data directory 0 = exports
$expRva = U32 $ddOff
$expSize = U32 ($ddOff + 4)
if ($expRva -eq 0) { Write-Output 'no export directory'; return }

# section table -> rva to file offset
$secOff = $optOff + $optSize
$secs = @()
for ($i = 0; $i -lt $numSec; $i++) {
    $s = $secOff + ($i * 40)
    $secs += [pscustomobject]@{
        Name  = [Text.Encoding]::ASCII.GetString($b, $s, 8).Trim([char]0)
        VAddr = U32 ($s + 12)
        VSize = U32 ($s + 8)
        Raw   = U32 ($s + 20)
    }
}
function Rva2Off([int]$rva) {
    foreach ($s in $secs) {
        $sz = [Math]::Max($s.VSize, 0x1000)
        if ($rva -ge $s.VAddr -and $rva -lt ($s.VAddr + $sz)) { return $s.Raw + ($rva - $s.VAddr) }
    }
    return -1
}

$expOff = Rva2Off $expRva
$numNames = U32 ($expOff + 24)
$namesRva = U32 ($expOff + 32)
$namesOff = Rva2Off $namesRva

Write-Output "exports: $numNames  (dir size $expSize)"
$out = @()
for ($i = 0; $i -lt $numNames; $i++) {
    $nRva = U32 ($namesOff + ($i * 4))
    $nOff = Rva2Off $nRva
    if ($nOff -lt 0) { continue }
    $end = $nOff
    while ($end -lt $b.Length -and $b[$end] -ne 0) { $end++ }
    $name = [Text.Encoding]::ASCII.GetString($b, $nOff, $end - $nOff)
    $out += $name
}
$out | Sort-Object
