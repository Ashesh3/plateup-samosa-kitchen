param([Parameter(Mandatory=$true)][string]$Dll,[Parameter(Mandatory=$true)][string]$Cecil)
$ErrorActionPreference='Stop'
Add-Type -Path $Cecil
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Get-Item -LiteralPath $Dll).FullName)
try {
    $a=$assembly.MainModule.GetType('SamosaKitchen.Art')
    $template=$a.Methods|Where-Object Name -eq Template
    $counter=$assembly.MainModule.GetType('SamosaKitchen.MasalaCounter')
    $templateIL=@($template.Body.Instructions|ForEach-Object {$_.Operand -as [string]})
    if(!($templateIL -match 'AddComponent<UnityEngine.BoxCollider>')){throw 'FAIL: counter template has no physical BoxCollider.'}
    if(!($templateIL -match 'UnityEngine.BoxCollider::set_size') -or !($templateIL -match 'UnityEngine.BoxCollider::set_center')){throw 'FAIL: collider dimensions not configured'}
    foreach($prop in 'get_IsPurchasable','get_SellOnlyAsDuplicate'){
        $method=$counter.Methods|Where-Object Name -eq $prop
        if(($method.Body.Instructions.OpCode.Code -join ',') -ne 'Ldc_I4_1,Ret'){throw "FAIL: counter shop rule $prop changed"}
    }
    'PASS: compiled template constructs its sized BoxCollider; shop remains purchasable, duplicates only.'
} finally {$assembly.Dispose()}
