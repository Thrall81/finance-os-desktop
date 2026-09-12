# Bibliothèques SQLite vendorisées

Ces binaires sont copiés directement depuis NuGet (pas de gestionnaire de paquets NuGet dans Unity) plutôt que via un outil comme NuGetForUnity, pour garder le projet sans dépendance d'outillage supplémentaire. Voir `docs/09-Decisions_techniques.md`.

| Fichier | Paquet NuGet | Version | Licence |
|---|---|---|---|
| `SQLite-net.dll` | [sqlite-net-pcl](https://www.nuget.org/packages/sqlite-net-pcl) | 1.9.172 | MIT |
| `SQLitePCLRaw.core.dll` | [SQLitePCLRaw.core](https://www.nuget.org/packages/SQLitePCLRaw.core) | 2.1.2 | Apache-2.0 |
| `SQLitePCLRaw.provider.e_sqlite3.dll` | [SQLitePCLRaw.provider.e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3) | 2.1.2 | Apache-2.0 |
| `SQLitePCLRaw.batteries_v2.dll` | [SQLitePCLRaw.bundle_green](https://www.nuget.org/packages/SQLitePCLRaw.bundle_green) | 2.1.2 | Apache-2.0 |
| `../x86_64/e_sqlite3.dll` (natif, Windows x64) | [SQLitePCLRaw.lib.e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3) | 2.1.2 | Public domain (SQLite) |

Version choisie délibérément : `sqlite-net-pcl` 1.9.172 est la dernière version dont la dépendance `.NETStandard2.0` pointe encore vers la paire `SQLitePCLRaw.core` / `provider.e_sqlite3` en version 2.1.x — la combinaison la plus éprouvée dans l'écosystème Unity/Xamarin/MAUI. Les versions plus récentes de `sqlite-net-pcl` (1.11.x) sont passées à `SQLitePCLRaw` 3.x et au paquet natif `SourceGear.sqlite3`, un changement trop récent pour être considéré comme éprouvé sur IL2CPP au moment de l'intégration.

## Mise à jour

Pas de commande automatisée : re-télécharger le `.nupkg` depuis `api.nuget.org`, en extraire le(s) DLL `lib/netstandard2.0/*.dll` (et `runtimes/win-x64/native/e_sqlite3.dll` pour le binaire natif), remplacer les fichiers ici, mettre à jour ce tableau.
