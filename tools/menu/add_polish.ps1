param([Parameter(Mandatory=$true)][string]$OriginalDll,[Parameter(Mandatory=$true)][string]$ManagedDirectory,[Parameter(Mandatory=$true)][string]$OutputDll)
$ErrorActionPreference='Stop'
$old=[IO.Path]::GetFullPath($OriginalDll);$managed=[IO.Path]::GetFullPath($ManagedDirectory)
$null=[System.Reflection.Assembly]::LoadFrom((Join-Path $managed 'Unity.Burst.Cecil.dll'))
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new();$resolver.AddSearchDirectory($managed)
$parameters=[Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
$a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($old,$parameters)
$ctor=($a.MainModule.Types|Where-Object FullName -eq 'BasePlatform').Methods|Where-Object Name -eq '.ctor'
$il=$ctor.Body.GetILProcessor()
foreach($spec in @(@('languages','pl'),@('languageNames','Polski'))){
 $x=@($ctor.Body.Instructions|Where-Object{$_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq $spec[0]})
 if($x.Count -ne 1){throw 'Niezgodny układ list języków'}
 $add=$x[0].Previous.Operand
 $il.InsertBefore($x[0],$il.Create([Mono.Cecil.Cil.OpCodes]::Dup));$il.InsertBefore($x[0],$il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,[string]$spec[1]));$il.InsertBefore($x[0],$il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$add))
}
$out=[IO.Path]::GetFullPath($OutputDll);New-Item -ItemType Directory -Path (Split-Path $out -Parent) -Force|Out-Null
if(Test-Path -LiteralPath $out){throw 'Plik produkcyjny już istnieje'}
$a.Write($out);$a.Dispose()
Write-Output 'Przygotowano produkcyjne menu Polski/pl bez zmian domyślnego języka i profilu.'
