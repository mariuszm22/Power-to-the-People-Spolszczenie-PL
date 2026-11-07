# Odtworzenie i rozwój

## Przebudowanie instalatora 1.0.0

Wymagania: Windows, Windows PowerShell i kompilator .NET Framework 4.x znajdujący się standardowo w `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. Kod używa bibliotek systemowych. Nie wymaga NuGet, OpenAI ani oryginalnych plików gry.

Z katalogu głównego uruchom `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -WizardOnly`. Powstanie `build/marko22-Instalator-PowerToThePeople-PL-1.0.0.exe`. Dołączony HTML „Kulisy” zostaje osadzony w EXE. Źródła C# i wszystkie zasoby są zgodne z opublikowanym instalatorem. Nagłówki PE i identyfikatory kompilacji .NET mogą różnić się między kompilacjami, więc nowy plik nie musi mieć SHA-256 oficjalnego EXE.

Wywołanie bez `-WizardOnly` dodatkowo kompiluje istniejący program testów silnika; nie uruchamia go. Testy silnika wymagają osobnej zgodnej kopii gry w `build/kopia_testowa/`. Nie używaj do tego właściwej instalacji Steam.

## Wykorzystanie gotowych łatek

`installer/payload/` zawiera wyłącznie delty PTTPPLP1, nie kompletne zasoby gry. Dla własnych zgodnych oryginałów można wywołać:

```powershell
python tools/patches.py apply --game-data "X:/moja-kopia/Power to the People_Data" --output work/patched
```

Narzędzie tylko czyta wskazane oryginały, sprawdza SHA-256 i zapisuje wynik w nowym katalogu. Nie instaluje niczego do Steam ani nie zmienia rejestru.

## Rozwijanie lokalizacji

1. Zachowaj wersję 1.0.0 i edytuj tylko PL w `translation/EN_PL_1.0.0.json`. Identyfikatory, EN i znaczniki są chronione.
2. `tools/rebuild_i2.py` tworzy kopię `resources.assets` z tabelą Polish/pl na podstawie własnego zgodnego oryginału. Wszystkie pozostałe języki są zachowane.
3. Skrypty `tools/menu/add_polish.ps1` i `translate_club.ps1` dokumentują i odtwarzają zmiany DLL. Wykonuje się je kolejno na kopiach, podając `-OriginalDll` / `-InputDll`, `-ManagedDirectory` i `-OutputDll`. Pierwszy dodaje język do listy, drugi dodaje trzy etykiety tylko dla polskiego. Korzystają z `Unity.Burst.Cecil.dll` własnej gry; biblioteka nie jest dystrybuowana w repozytorium. Auto pozostaje oryginalne.
4. `tools/patches.py create` generuje nowe delty i `Product.cs` z dwóch katalogów: własnych oryginałów i przygotowanych kopii. Wynik zapisuje osobno. Zaktualizuj wersję, manifest i źródła dopiero w ramach nowego kontrolowanego wydania.
5. Wykonaj QA tekstów, instalacji i rzeczywistej gry przed dystrybucją. Dołączone narzędzia niczego nie publikują.

Python 3.10+ wystarcza; narzędzia używają biblioteki standardowej. Pełne oryginały, DLL, SQLite, profile gracza i robocze wyniki pozostają poza Git.
