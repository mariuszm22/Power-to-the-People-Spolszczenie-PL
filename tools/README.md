# Narzędzia lokalne

- `unity_lokalnie.py`: istniejący czytnik i zapis Unity SerializedFile v22 oraz I2, bez sieci.
- `rebuild_i2.py`: odtwarza polską kolumnę z pełnego eksportu i własnego zgodnego oryginału.
- `delta.py`: oryginalny algorytm tworzenia PTTPPLP1 wykorzystany w wydaniu.
- `patches.py`: kontrolowane zastosowanie lub utworzenie delt na lokalnych kopiach.
- `menu/`: istniejące operacje IL, z parametrami wejścia i wyjścia zamiast ścieżek roboczych autora.

Przebudowa samego instalatora korzysta z już zatwierdzonych delt i nie potrzebuje tych narzędzi ani plików gry. Narzędzia nie wykonują żądań API, nie uruchamiają gry i nie publikują plików.
