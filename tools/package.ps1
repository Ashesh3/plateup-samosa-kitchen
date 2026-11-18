param([string]$Game, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$latest=Join-Path $root 'build\SamosaKitchen.dll'
$hash=(Get-FileHash -LiteralPath $latest).Hash
if($Game){
    . (Join-Path $PSScriptRoot 'paths.ps1')
    $live=Join-Path (Resolve-PlateUpGame $Game) 'Mods/SamosaKitchen/content/SamosaKitchen.dll'
    if($hash -ne (Get-FileHash -LiteralPath $live).Hash){throw 'The built DLL differs from the installed one.'}
}
$version=[Reflection.AssemblyName]::GetAssemblyName($latest).Version.ToString(3)
$files=[ordered]@{'Mods/SamosaKitchen/content/SamosaKitchen.dll'=$latest;'SamosaKitchen-README.md'=(Join-Path $root 'docs/QUICKSTART.md');'Masala-Counter-Guide.png'=(Join-Path $root 'docs/images/masala-counter.png')}
if(!$OutputDirectory){$OutputDirectory=Join-Path $root 'dist'}
$null=New-Item -ItemType Directory -Force -Path $OutputDirectory
$out=Join-Path $OutputDirectory ("SamosaKitchen-"+$version+".zip")
$tmp=Join-Path $OutputDirectory ('package-'+[Guid]::NewGuid().ToString('N')+'.partial')
Add-Type -AssemblyName System.IO.Compression.FileSystem
try {
    $z=[IO.Compression.ZipFile]::Open($tmp,'Create')
    try {foreach($it in $files.GetEnumerator()){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z,$it.Value,$it.Key)|Out-Null}}
    finally {$z.Dispose()}
    $z=[IO.Compression.ZipFile]::OpenRead($tmp)
    try {if($z.Entries.Count -ne $files.Count){throw 'Bad ZIP inventory'}
        foreach($it in $files.GetEnumerator()){$e=$z.GetEntry($it.Key);if(!$e){throw "Missing $($it.Key)"};$s=$e.Open()
            try{$h=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($s))}finally{$s.Dispose()}
            if($h -ne (Get-FileHash -LiteralPath $it.Value).Hash){throw "ZIP content mismatch $($it.Key)"}}
    } finally {$z.Dispose()}
    [IO.File]::Move($tmp,$out,$true)
} finally {if([IO.File]::Exists($tmp)){[IO.File]::Delete($tmp)}}
"Verified ZIP: $out ($($files.Count) files); player DLL SHA256: $hash"
