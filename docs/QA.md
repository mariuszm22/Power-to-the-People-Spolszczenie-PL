# QA wydania 1.0.0

Dotychczasowe kontrole objęły kompletność 1677/1677, ID i EN, placeholdery, I2, formatowanie, liczby, jednostki i zachowanie pozostałych języków. Zatwierdzone korekty porównano z aktualną bazą oraz zawartością pakietu.

Sprawdzono instalację na kopii, aktualizację rozpoznanej poprzedniej paczki, oryginalne backupy, przywracanie i jawny wybór języka `pl`. Oceniono sześć ekranów kreatora i trzy odnośniki. Kontrola wyglądu kroku instalowania korzystała z podglądu tego samego układu, ponieważ właściwa operacja przebiegała bardzo szybko.

Rzeczywista gra Steam uruchomiła się po polsku. Potwierdzono trzy etykiety Crytivo Club i wybrane panele rozgrywki. Galeria zawiera sześć autentycznych zrzutów. Pełne przejście wszystkich scenariuszy i sprawdzenie każdego tekstu w każdym możliwym widoku: **NOT_TESTED**.

Znany drobny problem: końcowy ukośnik obok poziomu trudności na ekranie wczytywania. Nie usuwano separatorów globalnie.

Przy publikacji źródeł dodatkowo sprawdzane są zgodność eksportu z SQLite i pakietem, komplet źródeł i zasobów, odtwarzalność budowania oraz brak niedozwolonych plików w Git. Wynik tej kontroli zapisano w `release/1.0.0/source-verification.json`.
