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

Projet en tout début de reconstruction. Voir `docs/` pour la documentation de conception (à venir).

## Stack

- Unity 6000.3.12f1 (UI Toolkit pour toute l'interface, y compris les graphiques via `Painter2D`)
- C#
- SQLite (fichier local embarqué)
