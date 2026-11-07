POWER TO THE PEOPLE — SPOLSZCZENIE PL 1.0.0
Pakiet ZIP do ręcznej instalacji bez graficznego kreatora
Autor i redakcja: marko22 — https://marko22.pl

WYMAGANIA
Windows, Windows PowerShell 5.1 i własna kopia Steam Power to the People
1.3.2, build 20302782 (AppID 1413370). Zamknij grę przed instalacją
i przywracaniem. Nie trzeba instalować dodatkowych programów.

Ten ZIP jest pakietem instalacyjnym, a nie archiwum źródeł GitHuba.
Nie kopiuj jego zawartości bezpośrednio do folderu gry. Rozpakuj cały
ZIP do osobnego folderu roboczego. Nie uruchamiaj skryptu z wnętrza ZIP.

INSTALACJA RĘCZNA
1. Zamknij grę. Rozpakuj archiwum, zachowując podfolder narzedzia.
2. Otwórz Windows PowerShell w rozpakowanym folderze z tym dokumentem.
3. Sprawdź instalację (podstaw swój rzeczywisty folder gry):

   powershell.exe -NoProfile -File .\Instalacja_reczna.ps1 -FolderGry "F:\SteamLibrary\steamapps\common\Power to the People" -Operacja Sprawdz

4. Zainstaluj spolszczenie:

   powershell.exe -NoProfile -File .\Instalacja_reczna.ps1 -FolderGry "F:\SteamLibrary\steamapps\common\Power to the People" -Operacja Instaluj

Skrypt stosuje dokładnie ten sam silnik i łatki co zatwierdzony EXE.
Nie otwiera okna kreatora. Wczytuje jego kod dopiero po sprawdzeniu SHA-256.
Silnik weryfikuje plik EXE gry, oba zmieniane pliki, łatki i wynik każdej
podmiany. Nieznana wersja lub brak oryginalnego backupu blokuje operację.
Nie ma opcji pomijającej sprawdzanie zgodności.

Rozpoznana wcześniejsza paczka marko22 może zostać zaktualizowana przy
zachowaniu oryginalnego backupu. Nie traktuje się spolszczonych plików
jako oryginałów. Zachowaj ukryty folder .marko22-pttp-pl-rc1 w grze.

Po poprawnej instalacji ustawiany jest język Polski (pl); poprzedni wybór
jest zapisywany w backupie. Jeśli zapis języka się nie powiedzie, wybierz
Polski w grze. Ustawienie Auto może wybrać angielski.
Opcjonalny parametr -NieZmieniajJezyka pozostawia ustawienie języka bez
zmian. Nie wyłącza żadnych kontroli plików ani kopii bezpieczeństwa.

PRZYWRACANIE ORYGINAŁÓW / ODINSTALOWANIE
Zamknij grę i wykonaj w tym samym rozpakowanym folderze:

   powershell.exe -NoProfile -File .\Instalacja_reczna.ps1 -FolderGry "F:\SteamLibrary\steamapps\common\Power to the People" -Operacja Przywroc

Zostaną przywrócone zweryfikowane oryginalne pliki i poprzednie ustawienie
języka zapisane dla Twojego profilu Windows. Można też skorzystać z opcji
„Usuń / przywróć” w graficznym instalatorze 1.0.0 — format backupu jest wspólny.
Nie usuwaj backupu przed udanym przywróceniem. Przy brakującym lub uszkodzonym
backupie albo nieznanej zmianie plików operacja zostanie wstrzymana.

JEŚLI WINDOWS BLOKUJE POBRANY SKRYPT
Sprawdź źródło i sumę SHA-256 archiwum. Dla zaufanego, pobranego ZIP otwórz
Właściwości pliku i użyj „Odblokuj”, jeśli ta opcja występuje, a następnie
rozpakuj go ponownie. Nie trzeba zmieniać globalnej polityki wykonywania
skryptów ani wyłączać zabezpieczeń Windows. Jeśli organizacja blokuje
skrypty, skorzystaj z graficznego EXE lub zasad pomocy IT.

ZAWARTOŚĆ
- Instalacja_reczna.ps1 — obsługa instalacji, kontroli i przywracania.
- narzedzia\marko22-Instalator-PowerToThePeople-PL-1.0.0.exe — niezmieniony,
  zatwierdzony silnik i wbudowane łatki; może też otworzyć kreator po
  samodzielnym uruchomieniu tego pliku.
- manifest.json i SHA256SUMS.txt — wersja i sumy plików.

Projekt nieoficjalny. Tłumaczenie powstało przy wsparciu AI, a następnie
zostało poddane korekcie i testom wybranych ekranów. Archiwum nie zawiera
oryginalnych plików gry, prywatnych danych ani kluczy API.

Pobieranie: https://marko22.pl/pobieranie#power-to-the-people
Zgłoszenia: https://github.com/mariuszm22/Power-to-the-People-Spolszczenie-PL/issues
