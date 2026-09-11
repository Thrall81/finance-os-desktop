# Finance OS Desktop

Application Windows locale de gestion et de prévision budgétaire personnelle.

Repartie de zéro à partir de l'ancien projet `personal-finance-os` (Symfony + Angular + PostgreSQL, conservé de côté), avec un changement radical d'architecture :

- application **desktop Windows**, développée avec **Unity 6** ;
- **aucun serveur, aucune base de données réseau** : stockage local dans un fichier **SQLite** unique ;
- **aucun connecteur externe** (pas de Gmail, pas d'Open Banking, pas de dossier surveillé) : saisie manuelle et import CSV local uniquement ;
- **statique et téléchargeable gratuitement**, utilisable entièrement hors ligne ;
- pas de compte, pas de session, pas de transmission de données à un tiers — donc aucun traitement de données personnelles au sens RGPD.

Le cœur fonctionnel — moteur de prévision de trésorerie, modèle de données (comptes, transactions, catégories, opérations récurrentes, occurrences prévisionnelles, budgets) — reprend les concepts déjà validés dans l'ancien projet, portés en C#.

## Statut

Documentation de conception rédigée, implémentation pas encore commencée.

## Documentation

| Document | Contenu |
|---|---|
| [docs/00-Vision.md](docs/00-Vision.md) | Pourquoi ce projet, philosophie, principes |
| [docs/01-Perimetre.md](docs/01-Perimetre.md) | Périmètre de la V1, exclusions, feuille de route |
| [docs/02-Architecture.md](docs/02-Architecture.md) | Couches Domain/Data/App/UI, composition, navigation |
| [docs/03-Modele_de_donnees.md](docs/03-Modele_de_donnees.md) | Entités et schéma SQLite |
| [docs/04-Stack_technique.md](docs/04-Stack_technique.md) | Unity, UI Toolkit, SQLite, organisation du dépôt |
| [docs/05-Conventions_de_code.md](docs/05-Conventions_de_code.md) | Conventions C#, montants, dates, tests, Git |
| [docs/06-Moteur_de_prevision.md](docs/06-Moteur_de_prevision.md) | Algorithme de projection de trésorerie |
| [docs/07-Interface.md](docs/07-Interface.md) | Écrans, graphiques faits maison, formulaires |
| [docs/08-Confidentialite_et_donnees.md](docs/08-Confidentialite_et_donnees.md) | Stockage local, sauvegarde, absence de réseau |
| [docs/09-Decisions_techniques.md](docs/09-Decisions_techniques.md) | Journal des décisions (ADR-101 et suivants) |

## Stack

- Unity 6000.3.12f1 (UI Toolkit pour toute l'interface, y compris les graphiques via `Painter2D`)
- C#
- SQLite (fichier local embarqué)
