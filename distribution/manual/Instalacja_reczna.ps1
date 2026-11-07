#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$FolderGry,
    [ValidateSet('Sprawdz','Instaluj','Przywroc')][string]$Operacja='Sprawdz',
    [switch]$NieZmieniajJezyka,
    [switch]$Json
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
try {
    if ($PSVersionTable.PSEdition -ne 'Desktop') {
        throw 'Użyj Windows PowerShell 5.1: powershell.exe -NoProfile -File .\Instalacja_reczna.ps1 ...'
    }
    $engineFile=Join-Path $PSScriptRoot 'narzedzia\marko22-Instalator-PowerToThePeople-PL-1.0.0.exe'
    $approvedHash='bcb5c92d9cf135bb03721b213ac0afd75c59bde0e4cd21db0753cb307998a294'
    if (-not (Test-Path -LiteralPath $engineFile -PathType Leaf)) { throw 'Brak narzędzia z pakietu ZIP. Rozpakuj całe archiwum.' }
    $stream=[IO.File]::OpenRead($engineFile)
    $sha=[Security.Cryptography.SHA256]::Create()
    try { $actualHash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($actualHash -ne $approvedHash) {
        throw 'Niezgodny SHA-256 narzędzia. Nie uruchomiono silnika i nie zmieniono plików gry.'
    }
    # Wczytanie zatwierdzonej biblioteki nie uruchamia Main ani okna kreatora.
    # Wszystkie operacje na plikach wykonuje niezmieniony silnik finalnego EXE.
    $assembly=[Reflection.Assembly]::LoadFrom($engineFile)
    $payload=[Marko22.PTTP.EmbeddedPatches]::new($assembly)
    $closedCheck=[Func[bool]] { return [Diagnostics.Process]::GetProcessesByName('Power to the People').Length -gt 0 }
    $root=[IO.Path]::GetFullPath($FolderGry).TrimEnd([char[]]@(92,47))
    $language=$null
    if (-not $NieZmieniajJezyka) {
        $language=[Marko22.PTTP.UserLanguage]::new((Join-Path $root '.marko22-pttp-pl-rc1'),$closedCheck)
    }
    $engine=[Marko22.PTTP.Engine]::new($root,$payload,$closedCheck,$null,$language)
    $before=$engine.Probe()
    switch ($Operacja) {
        'Instaluj' { $message=$engine.Install() }
        'Przywroc' { $message=$engine.Restore() }
        default { $message='Sprawdzono zgodność plików; nie zmieniono gry ani języka.' }
    }
    $result=[pscustomobject]@{Operacja=$Operacja;FolderGry=$root;StanPrzed=$before;StanPo=$engine.Probe();Komunikat=$message;Wersja='1.0.0'}
    if ($Json) { $result | ConvertTo-Json -Compress }
    else { $result | Format-List }
    exit 0
} catch {
    $errorMessage=$_.Exception.GetBaseException().Message
    if ($Json) { [pscustomobject]@{Blad=$errorMessage;Operacja=$Operacja} | ConvertTo-Json -Compress | Write-Output }
    else { Write-Error -Message $errorMessage -ErrorAction Continue }
    exit 1
}
