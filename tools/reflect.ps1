# reflect.ps1 - list real public members of a type in an assembly, via reflection.
param(
    [Parameter(Mandatory = $true)][string]$Assembly,
    [Parameter(Mandatory = $true)][string]$Type,
    [string]$Filter = ''
)
$ErrorActionPreference = 'Stop'
$bytes = [System.IO.File]::ReadAllBytes($Assembly)
$asm = [System.Reflection.Assembly]::Load($bytes)
$t = $asm.GetType($Type)
if (-not $t) {
    Write-Output "TYPE NOT FOUND: $Type"
    Write-Output '--- available types containing that leaf name ---'
    $asm.GetTypes() | Where-Object { $_.Name -like "*$($Type.Split('.')[-1])*" } |
        Select-Object -First 20 -ExpandProperty FullName
    exit 1
}
$flags = [System.Reflection.BindingFlags]::Public -bor
         [System.Reflection.BindingFlags]::Instance -bor
         [System.Reflection.BindingFlags]::Static -bor
         [System.Reflection.BindingFlags]::FlattenHierarchy

$re = if ($Filter) { [regex]::new($Filter, 'IgnoreCase') } else { $null }

Write-Output "=== $Type  (base: $($t.BaseType.FullName)) ==="
Write-Output '--- methods ---'
$t.GetMethods($flags) |
    Where-Object { $_.DeclaringType -eq $t -and -not $_.IsSpecialName } |
    Where-Object { -not $re -or $re.IsMatch($_.Name) } |
    Sort-Object Name |
    ForEach-Object {
        $ps = ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
        "$($_.ReturnType.Name) $($_.Name)($ps)"
    }

Write-Output '--- properties ---'
$t.GetProperties($flags) |
    Where-Object { $_.DeclaringType -eq $t } |
    Where-Object { -not $re -or $re.IsMatch($_.Name) } |
    Sort-Object Name |
    ForEach-Object { "$($_.PropertyType.Name) $($_.Name) $(if($_.CanWrite){'{get;set;}'}else{'{get;}'})" }

Write-Output '--- events ---'
$t.GetEvents($flags) |
    Where-Object { $_.DeclaringType -eq $t } |
    Where-Object { -not $re -or $re.IsMatch($_.Name) } |
    Sort-Object Name |
    ForEach-Object { "$($_.EventHandlerType.Name) $($_.Name)" }
