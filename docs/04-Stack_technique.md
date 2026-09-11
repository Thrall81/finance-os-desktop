# Stack technique

# Finance OS Desktop

---

# 1. Résumé

```text
Moteur          Unity 6000.3.12f1 (LTS-track "Unity 6")
Langage         C# (profil .NET Standard 2.1 imposé par Unity)
Interface       UI Toolkit exclusivement (UXML + USS + C#) — aucun uGUI/Canvas
Graphiques      Rendu vectoriel maison via Painter2D (pas de bibliothèque tierce)
Stockage        SQLite (fichier local unique), accès via sqlite-net-pcl
Plateforme      Windows 10/11 x64 uniquement pour la V1
Tests           Unity Test Framework (NUnit) — Edit Mode pour la logique pure
Distribution    Build "Standalone Windows" + installeur Inno Setup, téléchargement gratuit
```

Aucun serveur, aucune base de données réseau, aucun framework web, aucun conteneur. Le projet n'a pas besoin de Docker : l'exécutable final est autonome.

---

# 2. Pourquoi Unity plutôt qu'une alternative desktop classique

Des alternatives existent (Electron, Tauri, .NET MAUI, Avalonia). Unity a été retenu parce que :

- l'auteur du projet le maîtrise déjà et y est rapide ;
- UI Toolkit (successeur moderne de uGUI) est suffisamment mature pour une interface de gestion dense en formulaires et tableaux (`MultiColumnListView`, data binding, USS proche du CSS) ;
- un seul écosystème couvre à la fois l'interface **et** les graphiques personnalisés (`Painter2D`), sans dépendance tierce ;
- le packaging Windows d'une build Unity Standalone est simple et ne nécessite pas de runtime supplémentaire à installer par l'utilisateur.

Le prix à payer, assumé consciemment : Unity reste un moteur de jeu, donc plus lourd à l'installation (build ~100+ Mo) qu'une application .NET native minimaliste, et certaines conventions desktop (menus natifs Windows, accessibilité clavier fine) demandent plus de travail manuel qu'avec un framework UI natif.

---

# 3. Interface — UI Toolkit uniquement

Un seul paradigme d'interface dans tout le projet : **pas de uGUI/Canvas**, même pour les graphiques (cf. `07-Interface.md`).

- `UIDocument` racine par écran logique, `PanelSettings` partagé ;
- fichiers `.uxml` (structure) + `.uss` (style) + contrôleurs C# (logique de liaison) ;
- `MultiColumnListView` pour les tableaux (transactions, occurrences) ;
- data binding runtime d'UI Toolkit pour lier les modèles aux champs.

---

# 4. Graphiques — rendu maison

Décision documentée dans `13-Decisions_techniques.md` (ADR-103) : plutôt qu'une bibliothèque tierce comme XCharts (uGUI), les graphiques sont dessinés via l'API `Painter2D` (dessin vectoriel immédiat) dans des `VisualElement` personnalisés. Quatre types de rendu couvrent tous les besoins de la V1 : courbe (avec segments pleins/pointillés pour réel/prévu), barres, anneau, infobulle au survol.

---

# 5. Stockage — SQLite embarqué

## 5.1 Bibliothèque

`sqlite-net-pcl` (+ binaires natifs `SQLite3` correspondants pour Windows x64) : légère, éprouvée, API synchrone simple, largement utilisée dans l'écosystème Unity/Xamarin/MAUI pour exactement ce cas d'usage.

Alternative écartée : `Microsoft.Data.Sqlite` (nécessite NuGetForUnity et une gestion de dépendances .NET plus lourde pour un bénéfice marginal ici).

## 5.2 Emplacement du fichier

Voir `08-Confidentialite_et_donnees.md` pour la politique complète (mode portable par défaut, repli sur `%APPDATA%`).

## 5.3 Migrations de schéma

Pas d'outil de migration dédié comme Doctrine Migrations. Le schéma est versionné par un entier `PRAGMA user_version` ; au démarrage, l'application applique séquentiellement les scripts SQL de migration nécessaires (approche minimale, sans dépendance supplémentaire).

---

# 6. Organisation du dépôt

```text
finance-os-desktop/
├── Assets/
│   ├── Scripts/
│   │   ├── Domain/          # modèles métier purs (aucune dépendance Unity)
│   │   ├── Forecast/        # moteur de prévision (pur C#, testable hors Unity)
│   │   ├── Data/            # accès SQLite, repositories, migrations
│   │   ├── App/             # services applicatifs, bootstrap, composition
│   │   └── UI/               # contrôleurs liant UI Toolkit aux services applicatifs
│   ├── UI/
│   │   ├── UXML/
│   │   ├── USS/
│   │   └── Charts/          # VisualElement personnalisés (Painter2D)
│   ├── Scenes/
│   │   └── Main.unity        # scène unique de l'application
│   └── Tests/
│       ├── EditMode/         # tests unitaires (Domain, Forecast, Data)
│       └── PlayMode/         # tests d'intégration UI si nécessaire
├── docs/
├── Packages/
└── ProjectSettings/
```

`Domain` et `Forecast` ne référencent aucune classe `UnityEngine` : ils doivent pouvoir être testés en Edit Mode sans dépendre du moteur de rendu, comme `ForecastCalculator` l'était côté Symfony.

---

# 7. Dépendances volontairement exclues

```text
Docker
API réseau (HttpClient, UnityWebRequest) — sauf éventuel bouton manuel "vérifier une mise à jour" en V2, jamais automatique
Toute bibliothèque de connecteur (Gmail, OAuth, Open Banking)
NgRx / état global complexe — inutile, il n'y a pas de SPA
ORM lourd — SQL direct via sqlite-net suffit au volume attendu (quelques dizaines de milliers de lignes)
Framework d'injection de dépendances (Zenject, VContainer) — composition manuelle dans un bootstrap tant que la complexité ne le justifie pas
```

---

# 8. Tests

- **Edit Mode (NUnit)** : logique de `Domain` et `Forecast` — calcul de solde, génération d'occurrences, point bas, idempotence — sans dépendre du moteur Unity, comme les tests PHPUnit du moteur de prévision de l'ancien projet.
- **Edit Mode** également pour la couche `Data` (SQLite) via une base de test temporaire sur disque.
- **Play Mode** réservé aux cas où une interaction UI réelle doit être vérifiée (formulaires, navigation).

---

# 9. Distribution

- Build "Standalone Windows" (IL2CPP, architecture x64) ;
- packaging en installeur via Inno Setup (script versionné dans `packaging/`) ou en simple dossier zip portable ;
- diffusion gratuite : GitHub Releases ou site personnel — pas de store, pas de compte requis pour télécharger ;
- aucune télémétrie de build, aucun SDK d'analytics inclus.
