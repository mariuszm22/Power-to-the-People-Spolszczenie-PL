param([Parameter(Mandatory=$true)][string]$InputDll,[Parameter(Mandatory=$true)][string]$ManagedDirectory,[Parameter(Mandatory=$true)][string]$OutputDll)
$ErrorActionPreference='Stop'
$managed=[IO.Path]::GetFullPath($ManagedDirectory)
if(Test-Path -LiteralPath $OutputDll){throw 'Output already exists'}
$null=[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Unity.Burst.Cecil.dll'))
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new();$resolver.AddSearchDirectory($managed)
$p=[Mono.Cecil.ReaderParameters]::new();$p.AssemblyResolver=$resolver
$a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly(([IO.Path]::GetFullPath($InputDll)),$p);$module=$a.MainModule
$menu=$module.Types|Where-Object FullName -CEQ 'MainMenu';$start=$menu.Methods|Where-Object Name -CEQ 'Start'
$voidType=$start.ReturnType
$stringType=(($module.Types|Where-Object FullName -CEQ 'BasePlatform').Methods|Where-Object Name -CEQ 'GetLanguage').ReturnType
if(@($start.Body.Instructions|Where-Object{$_.OpCode.Name -eq 'ret'}).Count -ne 1 -or $start.Body.Instructions[-1].OpCode.Name -ne 'ret'){throw 'Unknown MainMenu.Start structure'}
$core=$resolver.Resolve(($module.AssemblyReferences|Where-Object Name -CEQ 'UnityEngine.CoreModule'))
$ui=$resolver.Resolve(($module.AssemblyReferences|Where-Object Name -CEQ 'UnityEngine.UI'))
$gameObject=$core.MainModule.GetType('UnityEngine.GameObject');$component=$core.MainModule.GetType('UnityEngine.Component');$transform=$core.MainModule.GetType('UnityEngine.Transform');$unityObject=$core.MainModule.GetType('UnityEngine.Object');$text=$ui.MainModule.GetType('UnityEngine.UI.Text')
$find=$module.ImportReference(($gameObject.Methods|Where-Object{$_.Name -ceq 'Find' -and $_.Parameters.Count -eq 1}))
$getTransform=$module.ImportReference(($gameObject.Methods|Where-Object Name -CEQ 'get_transform'))
$findChild=$module.ImportReference(($transform.Methods|Where-Object{$_.Name -ceq 'Find' -and $_.Parameters.Count -eq 1}))
$getComponent=[Mono.Cecil.GenericInstanceMethod]::new($module.ImportReference(($component.Methods|Where-Object{$_.Name -ceq 'GetComponent' -and $_.HasGenericParameters -and $_.Parameters.Count -eq 0})));$getComponent.GenericArguments.Add($module.ImportReference($text))
$isNull=$module.ImportReference(($unityObject.Methods|Where-Object Name -CEQ 'op_Equality'))
$setText=$module.ImportReference(($text.Methods|Where-Object Name -CEQ 'set_text'))
$localizer=$module.Types|Where-Object FullName -CEQ 'I2.Loc.LocalizationManager'
if(-not $localizer){$ref=(($module.Types|Where-Object FullName -CEQ 'PlatformManager').Methods|Where-Object Name -CEQ 'Start').Body.Instructions|Where-Object{$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -ceq 'set_CurrentLanguageCode'};$localizer=$ref.Operand.DeclaringType.Resolve()}
$language=$module.ImportReference(($localizer.Methods|Where-Object Name -CEQ 'get_CurrentLanguageCode'))
$equals=$null
foreach($type in $module.Types){foreach($method in $type.Methods){if($method.HasBody){foreach($instruction in $method.Body.Instructions){if($instruction.Operand -is [Mono.Cecil.MethodReference] -and $instruction.Operand.DeclaringType.FullName -ceq 'System.String' -and $instruction.Operand.Name -ceq 'op_Equality'){$equals=$instruction.Operand;break}}};if($equals){break}};if($equals){break}}
if(-not $equals){throw 'Missing existing string comparison reference'}
# Reuse the game's own primitive-type scopes; older UI modules expose mscorlib aliases.
$find.Parameters[0].ParameterType=$stringType;$findChild.Parameters[0].ParameterType=$stringType
$setText.Parameters[0].ParameterType=$stringType;$setText.ReturnType=$voidType;$isNull.ReturnType=$equals.ReturnType
$attributes=[Mono.Cecil.MethodAttributes]::Private -bor [Mono.Cecil.MethodAttributes]::Static -bor [Mono.Cecil.MethodAttributes]::HideBySig
$helper=[Mono.Cecil.MethodDefinition]::new('Marko22SetClubText',$attributes,$voidType)
foreach($n in @('relativePath','polishText')){$helper.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($n,[Mono.Cecil.ParameterAttributes]::None,$stringType))}
$helper.Body.InitLocals=$true
foreach($type in @($gameObject,$transform,$text)){$helper.Body.Variables.Add([Mono.Cecil.Cil.VariableDefinition]::new($module.ImportReference($type)))}
$il=$helper.Body.GetILProcessor();$done=$il.Create([Mono.Cecil.Cil.OpCodes]::Ret)
$body=@(
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,'Canvas'),$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$find),$il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_0),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldnull),$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$isNull),$il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$done),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0),$il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$getTransform),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0),$il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$findChild),$il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_1),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_1),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldnull),$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$isNull),$il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$done),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_1),$il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$getComponent),$il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_2),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_2),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldnull),$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$isNull),$il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$done),
 $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_2),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1),$il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$setText),$done)
foreach($i in $body){$il.Append($i)};$menu.Methods.Add($helper)
$il=$start.Body.GetILProcessor();$ret=$start.Body.Instructions[-1]
foreach($i in @($il.Create([Mono.Cecil.Cil.OpCodes]::Call,$language),$il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,'pl'),$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$equals),$il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$ret))){$il.InsertBefore($ret,$i)}
foreach($item in @(@('Crytivo Club/Sign In Button/Text','ZALOGUJ SIĘ'),@('Crytivo Club/Button/Text','DOŁĄCZ DO CRYTIVO CLUB'),@('Crytivo Club/Button/Text Sign In','ODBLOKUJ EKSKLUZYWNE TREŚCI'))){
 $il.InsertBefore($ret,$il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,[string]$item[0]));$il.InsertBefore($ret,$il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr,[string]$item[1]));$il.InsertBefore($ret,$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$helper))
}
foreach($reference in @($module.AssemblyReferences|Where-Object Name -CEQ 'mscorlib')){$null=$module.AssemblyReferences.Remove($reference)}
$a.Write(([IO.Path]::GetFullPath($OutputDll)));$a.Dispose()
Write-Output 'Prepared three Polish-only Crytivo labels; original scene, EN and other languages unchanged.'
