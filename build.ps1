param([switch]$WizardOnly)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot;$stage=Join-Path $taskRoot 'installer'
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $csc)){throw 'Brak lokalnego kompilatora .NET Framework.'}
$output=Join-Path $taskRoot 'build';New-Item -ItemType Directory -Path $output -Force|Out-Null
$resources=@((('/resource:{0},PTTP.i2.patch.gz') -f (Join-Path $stage 'payload\i2.patch.gz')),(('/resource:{0},PTTP.menu.patch.gz') -f (Join-Path $stage 'payload\menu.patch.gz')))
$core=Join-Path $stage 'src\Core.cs';$product=Join-Path $stage 'src\Product.cs';$wizard=Join-Path $stage 'src\Wizard.cs'
$known=Join-Path $stage 'src\KnownReleases.cs'
$language=Join-Path $stage 'src\UserLanguage.cs'
$visual=Join-Path $stage 'src\ReferenceVisuals.cs';$controls=Join-Path $stage 'src\ReferenceControls.cs'
$assets=Join-Path $stage 'assets'
$ui=@((('/resource:{0},Marko22Installer.UiProfile.json') -f (Join-Path $assets 'ui-profile.json')),(('/resource:{0},Marko22Installer.Ui.Background.jpg') -f (Join-Path $assets 'Games\PowerToThePeople\background.jpg')),(('/resource:{0},Marko22Installer.Ui.Brand.png') -f (Join-Path $assets 'Common\marko22-brand.png')),(('/resource:{0},Marko22Installer.Ui.BrandIcon.png') -f (Join-Path $assets 'Common\marko22-scorpion.png')))
$exe=Join-Path $output 'marko22-Instalator-PowerToThePeople-PL-1.0.0.exe'
$ui += (('/resource:{0},Marko22Installer.Ui.GameLogo.png') -f (Join-Path $assets 'Games\PowerToThePeople\logo.png'))
$ui += (('/resource:{0},Marko22Installer.Kulisy.html') -f (Join-Path $taskRoot 'docs\Kulisy_spolszczenia_Power_to_the_People.html'))
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$exe" ("/win32manifest:"+(Join-Path $stage 'app.manifest')) ("/win32icon:"+(Join-Path $assets 'Common\marko22-scorpion.ico')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll $resources $ui $core $product $known $language $wizard $visual $controls (Join-Path $stage 'src\AssemblyInfo.cs')
if($LASTEXITCODE -ne 0){throw 'Nie udało się zbudować kreatora.'}
if($WizardOnly){Write-Output 'Zbudowano istniejący kreator. Pełny osobny zestaw testów silnika nie był uruchamiany.';exit}
$tests=Join-Path $stage 'tests\EngineTests.cs';$testExe=Join-Path $output 'EngineTests.exe'
& $csc /nologo /target:exe /platform:anycpu /optimize+ /utf8output "/out:$testExe" /reference:System.dll /reference:System.Core.dll /reference:System.Runtime.Serialization.dll $resources $core $product $known $language $tests
if($LASTEXITCODE -ne 0){throw 'Nie udało się zbudować testów silnika.'}
Write-Output "Zbudowano kreator i osobny program testów. Żaden nie został uruchomiony."
