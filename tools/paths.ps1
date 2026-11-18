# Resolve a supplied game directory first; optionally find Steam's default
# Windows library when the caller doesn't provide one. No machine paths in source.
function Resolve-PlateUpGame([string]$Game) {
    $roots = if ($Game) {
        @($Game)
    } else {
        $steam = if ($IsWindows) {
            Get-ItemPropertyValue -LiteralPath 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction SilentlyContinue
        }
        @($steam, $(if (${env:ProgramFiles(x86)}) {Join-Path ${env:ProgramFiles(x86)} 'Steam'})) |
            Where-Object {$_} | Select-Object -Unique | ForEach-Object {Join-Path $_ 'steamapps/common/PlateUp'}
    }
    foreach ($candidate in $roots) {
        foreach ($p in @($candidate, (Join-Path $candidate 'PlateUp'))) {
            if ((Test-Path -LiteralPath (Join-Path $p 'PlateUp.exe') -PathType Leaf) -and
                (Test-Path -LiteralPath (Join-Path $p 'PlateUp_Data/Managed') -PathType Container)) {
                return (Get-Item -LiteralPath $p).FullName
            }
        }
    }
    throw 'Set PLATEUP_GAME_DIR or pass -Game with the directory containing PlateUp.exe.'
}

function Resolve-PlateUpKitchenLib([string]$Game,[string]$KitchenLib) {
    if ($KitchenLib) {return (Get-Item -LiteralPath $KitchenLib -ErrorAction Stop).FullName}
    $local = Join-Path $Game 'Mods/2898069883/content/KitchenLib-Workshop.dll'
    if (Test-Path -LiteralPath $local -PathType Leaf) {return (Get-Item -LiteralPath $local).FullName}
    $d = Get-Item -LiteralPath $Game
    while ($d -and $d.Name -ne 'steamapps') {$d=$d.Parent}
    if ($d) {
        $workshop=Join-Path $d.FullName 'workshop/content/1599600/2898069883/KitchenLib-Workshop.dll'
        if (Test-Path -LiteralPath $workshop -PathType Leaf) {return (Get-Item -LiteralPath $workshop).FullName}
    }
    throw 'KitchenLib was not found. Supply -KitchenLib with the path to KitchenLib-Workshop.dll.'
}
