# Budowa projektu

Gra: Unity 2021.1.23f1, lokalizacja I2Languages, obiekt PathID 1027 w `resources.assets`. Tabela obejmuje 1677 rekordów. Polski/pl dodano jako osobną kolumnę. Trzy teksty Crytivo Club są przypisywane w menu tylko przy polskim języku.

`installer/src/Core.cs` odpowiada za kontrolę plików, zastosowanie delt, transakcyjny zapis i przywracanie. `KnownReleases.cs` rozpoznaje wcześniejsze paczki wyłącznie po dokładnych parach hashy. `UserLanguage.cs` zabezpiecza i ustawia dwa wpisy języka w Unity PlayerPrefs bieżącego użytkownika; nie zmienia innych ustawień. Poprzedni wybór wraca przy odinstalowaniu. Działająca gra blokuje operację.

`Wizard.cs`, `ReferenceControls.cs`, `ReferenceVisuals.cs` oraz `installer/assets/` definiują przyjęty sześciokrokowy kreator, logo gry, identyfikację marko22 i trzy stałe odnośniki. Źródła C# są zachowane bez zmian z wydania 1.0.0. Skrypt budowania różni się wyłącznie ścieżkami repozytorium i katalogiem wyjściowym.

Folder backupu `.marko22-pttp-pl-rc1` zachowuje historyczną nazwę dla zgodności. Przetłumaczony plik nigdy nie staje się oryginalnym backupem. Nieznana wersja plików i uszkodzone kopie blokują instalację/przywracanie.

Repozytorium zawiera zatwierdzony eksport EN↔PL zamiast roboczej bazy SQLite z historią żądań. Nie zawiera instalacji gry ani plików wykonywalnych. Manifest 1.0.0 podaje zgodność gry, hashe oraz granice przeprowadzonego QA.
