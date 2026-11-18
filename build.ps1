param(
    [string]$Game = $env:PLATEUP_GAME_DIR,
    [string]$KitchenLib,
    [switch]$QAOnly
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'tools/paths.ps1')
$Game = Resolve-PlateUpGame $Game
$managed = Join-Path $Game 'PlateUp_Data\Managed'
$lib = Resolve-PlateUpKitchenLib $Game $KitchenLib
$sdk = @(dotnet --list-sdks | Where-Object { $_ -match '^\d+' })[-1]
if ($sdk -notmatch '^(\S+) \[(.+)\]') {throw 'Install a .NET SDK to build; playing only needs the release DLL.'}
$csc = Join-Path $Matches[2] (Join-Path $Matches[1] 'Roslyn\bincore\csc.dll')
$common = @('/nologo','/nostdlib+','/target:library','/langversion:9','/optimize+','/deterministic+','/warn:4')
# Use the game's own reference set, not desktop .NET Core. Nothing from Steam
# is redistributed by this script.
$refNames = 'mscorlib','System','System.Core','netstandard','UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','Unity.Entities','Unity.Collections','Unity.Mathematics','Unity.TextMeshPro','Sirenix.Serialization','Sirenix.OdinInspector.Attributes','Kitchen.Common','Kitchen.GameData','Kitchen.FranchiseMode','KitchenMode','KitchenMods'
$refs = @($refNames | ForEach-Object {'/reference:'+(Get-Item -LiteralPath (Join-Path $managed ($_+'.dll'))).FullName}) + @('/reference:'+(Get-Item -LiteralPath $lib).FullName)
$out=Join-Path $PSScriptRoot 'build'
$null=New-Item -ItemType Directory -Force -Path $out
if($QAOnly){
    & dotnet $csc @common @refs ('/out:'+(Join-Path $out 'SamosaKitchen.QA.dll')) (Join-Path $PSScriptRoot 'tests\SamosaProbe.cs')
}else{
    $src=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object FullName)
    & dotnet $csc @common @refs ('/resource:'+(Join-Path $PSScriptRoot 'Assets\samosa.smk')+',SamosaKitchen.Art.smk') ('/out:'+(Join-Path $out 'SamosaKitchen.dll')) @src
}
if($LASTEXITCODE -ne 0){throw "Compiler failed ($LASTEXITCODE)."}
