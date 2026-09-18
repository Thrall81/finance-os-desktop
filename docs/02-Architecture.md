# Architecture

# Finance OS Desktop

---

# 1. Objectif

Décrire l'organisation technique de l'application avant toute implémentation, dans le même esprit que l'ancien `02-Architecture.md` : conventions du moteur avant architecture théorique, abstractions uniquement quand un besoin réel existe.

---

# 2. Vue d'ensemble

Il n'y a plus de séparation client/serveur : **un seul processus**, l'exécutable Unity, qui contient à la fois l'interface, la logique métier et l'accès aux données.

```text
+----------------------------------------------------+
|                  Application Unity                  |
|                                                      |
|  +------------+   +-------------+   +------------+ |
|  |    UI      |-->|     App     |-->|   Domain   | |
|  | (UI Toolkit|   | (services   |   | (modèles + | |
|  |  + Charts) |   |  applicatifs|   |  Forecast) | |
|  +------------+   +-------------+   +------------+ |
|                          |                          |
|                    +------------+                   |
|                    |    Data    |                   |
|                    | (SQLite)   |                   |
|                    +------------+                   |
+----------------------------------------------------+
                          |
                 fichier .db local
```

Aucune flèche ne sort de ce schéma vers le réseau. C'est une contrainte d'architecture, pas seulement une intention (cf. `08-Confidentialite_et_donnees.md`).

---

# 3. Les quatre couches

## 3.1 Domain

Modèles métier purs (`Account`, `Transaction`, `Category`, `RecurringOperation`, `ForecastOccurrence`, `Budget`...) et le moteur de prévision (`ForecastCalculator`). Aucune référence à `UnityEngine`, à SQLite ou à l'UI. Entièrement testable en isolation (Edit Mode, pur C#).

## 3.2 Data

Accès au fichier SQLite : repositories (`AccountRepository`, `TransactionRepository`...), migrations de schéma, mapping ligne SQL ↔ objet Domain. Ne contient aucune règle métier — uniquement de la persistance, comme les repositories Doctrine de l'ancien projet.

## 3.3 App

Services applicatifs qui orchestrent Domain + Data pour répondre à un cas d'usage complet : `CreateTransactionService`, `RecalculateForecastService`, `ImportCsvService`. Équivalent des services Symfony (`Service/Forecast/*`, `Service/Import/*`). C'est la seule couche qui a le droit d'appeler à la fois un repository et le moteur de prévision.

## 3.4 UI

Contrôleurs qui lient les `VisualElement` (UI Toolkit) aux services applicatifs. Une page = un contrôleur. Aucune logique métier ici : validation d'affichage uniquement, la validation réelle reste dans `App`/`Domain`.

---

# 4. Composition et démarrage

Pas de framework d'injection de dépendances. La construction du graphe d'objets est scindée en deux, pour rester testable sans passer par le cycle de vie Unity :

```text
AppContainer (classe C# simple, App)
  → ouvre/crée le fichier SQLite (AppDatabase)
  → applique les migrations en attente
  → instancie les repositories
  → instancie les services applicatifs (App)

AppBootstrap (MonoBehaviour, attaché à un objet de la scène Main.unity — pas encore créé)
  → Awake() construit un AppContainer
  → instancie le contrôleur racine de navigation (UI) à partir de ses services
```

`AppContainer` ne dépend d'aucune classe `UnityEngine` dans sa propre logique de composition (il utilise `AppDatabase`, lui-même autorisé à référencer Unity pour la résolution de chemin) et s'instancie directement avec `new` dans un test, ce qu'un `MonoBehaviour` ne permet pas. Cette construction manuelle reste lisible tant que le nombre de services est raisonnable (quelques dizaines). Un conteneur DI (VContainer) ne sera introduit que si ce fichier devient réellement difficile à maintenir — pas par anticipation.

---

# 5. Navigation

Une seule scène Unity (`Main.unity`) contenant un `UIDocument` racine. La navigation entre écrans (Tableau de bord, Comptes, Transactions, Prévisions, Budget, Paramètres) est gérée par un `NavigationController` qui échange le contenu du panneau racine — pas de chargement de scènes Unity multiples, qui serait plus adapté à un jeu qu'à une application de gestion.

---

# 6. Flux principal d'une opération

Exemple — création d'une transaction manuelle :

```text
Saisie utilisateur (UI)
    │
    ▼
TransactionFormController valide l'affichage et appelle
    │
    ▼
CreateTransactionService (App)
    │
    ├──▶ TransactionRepository.Insert (Data)
    │
    └──▶ ForecastCalculator.Recalculate (Domain) si la transaction
         affecte une période déjà projetée
    │
    ▼
UI notifiée (événement C#) → rafraîchissement du tableau de bord
```

Le recalcul est synchrone dans cette V1 (volume de données faible, pas de traitement asynchrone type Messenger nécessaire).

---

# 7. Points d'extension prévus

- couche `Data` remplaçable en théorie (un autre moteur de stockage local), mais aucune abstraction supplémentaire n'est créée tant qu'un second besoin concret n'existe pas.

`IStatementImporter` (import CSV) envisagée initialement n'a jamais été construite — l'import CSV a été écarté définitivement du périmètre (2026-09-18, ADR-149, `01-Perimetre.md` §3).

---

# 8. Principes d'architecture

Repris tels quels de l'ancien projet :

- privilégier les mécanismes natifs d'Unity et du C# ;
- créer une interface uniquement lorsque plusieurs implémentations existent ou sont concrètement prévues ;
- éviter les couches sans responsabilité propre ;
- limiter les dépendances entre fonctionnalités ;
- ne pas anticiper des besoins hypothétiques (multi-devise, multi-utilisateur, synchronisation).
