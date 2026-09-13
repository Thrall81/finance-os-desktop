# Conventions de code

# Finance OS Desktop

---

# 1. Principes généraux

Repris de l'ancien projet, valables indépendamment de la technologie :

1. la solution la plus simple répondant correctement au besoin est privilégiée ;
2. pas d'interface pour une implémentation unique sans remplacement concret prévu ;
3. pas de couche technique sans valeur métier ;
4. le code reflète le vocabulaire métier (`Account`, `ForecastOccurrence`, `RecurringOperation`), jamais `Manager`/`Helper`/`Utility`/`DataService` génériques ;
5. ne pas anticiper un besoin hypothétique (multi-devise, multi-utilisateur, synchronisation cloud).

---

# 2. Langue

- code (classes, méthodes, variables, tables SQL) en **anglais** ;
- interface utilisateur en **français** ;
- documentation en **français**.

---

# 3. Conventions C#

## 3.1 Style

- `PascalCase` pour les classes, méthodes, propriétés publiques ;
- `camelCase` pour les variables locales et paramètres ;
- `_camelCase` pour les champs privés ;
- fichiers `.cs` nommés comme la classe qu'ils contiennent.

## 3.2 Immuabilité

Les objets de résultat de calcul (`ForecastResult`, `ForecastEvent`) sont des `readonly record` ou des classes avec propriétés `init` — équivalent du `readonly` PHP de l'ancien projet.

```csharp
public sealed record ForecastResult(
    DateOnly DateFrom,
    DateOnly DateTo,
    long OpeningBalanceMinor,
    long ClosingBalanceMinor,
    long LowestBalanceMinor,
    DateOnly LowestBalanceDate,
    IReadOnlyList<ForecastDayPoint> Timeline);
```

## 3.3 Nullabilité

`#nullable enable` activé sur tout le projet. Un type nullable (`Account?`) signale une absence normale et documentée, jamais un raccourci pour éviter une vérification.

## 3.4 Classes scellées

Les services applicatifs et repositories sont `sealed` par défaut, sauf besoin explicite d'héritage. La composition (injection par constructeur) prime sur l'héritage.

## 3.5 Enums

Les listes fermées métier utilisent des `enum` C#, jamais des chaînes libres dispersées dans le code.

```csharp
public enum AccountType
{
    Current,
    Savings,
    Cash,
    Investment,
    Debt,
}
```

---

# 4. Montants et devises

Identique à l'ancien projet, sans exception :

- montants stockés et manipulés en **unités mineures**, type `long` (centimes) — jamais `float`/`double` ;
- propriétés explicitement suffixées `Minor` (`AmountMinor`, `BalanceMinor`) ;
- convention de signe : revenu positif, dépense négative, virement sortant négatif / entrant positif ;
- devise ISO 4217 conservée sur chaque montant même si la V1 n'utilise que `EUR`.

---

# 5. Dates

- `DateTime` (composante horaire toujours à zéro) pour une date civile (date d'opération, échéance) ;
- `DateTimeOffset` pour un événement horodaté (date de création d'un enregistrement) ;
- stockage SQLite en texte ISO-8601 (`yyyy-MM-dd` / `yyyy-MM-ddTHH:mm:sszzz`), jamais en format ambigu.

`System.DateOnly` (.NET 6+) n'est pas disponible dans le runtime scripting d'Unity 6000.3 — vérifié empiriquement (`error CS0246`) en écrivant le modèle `Domain`, cf. `09-Decisions_techniques.md` (ADR-110). Ne pas réintroduire `DateOnly` sans revalider sur une version d'Unity ultérieure.

---

# 6. Organisation Domain / App / Data / UI

Voir `02-Architecture.md`. Règle stricte : `Domain` ne référence jamais `UnityEngine`, `Data` ou `UI`. `Data` ne contient aucune règle métier. `UI` n'appelle jamais directement un repository, toujours via `App`.

---

# 7. Identifiants

Décision **différente** de l'ancien projet (documentée en `13-Decisions_techniques.md`, ADR-104) : les identifiants sont des `INTEGER PRIMARY KEY AUTOINCREMENT` SQLite, pas des UUID. L'ancien choix d'UUID visait la synchronisation et les imports multi-sources d'un système distribué ; ce contexte a disparu avec les connecteurs. Un entier auto-incrémenté est plus simple, plus rapide, et idiomatique en SQLite pour un fichier local mono-utilisateur.

---

# 8. Tests

- toute logique de `Domain`/`Forecast` significative est couverte par un test Edit Mode (NUnit) ;
- nommage : `NomDeLaClasseTests`, méthode `MethodName_Scenario_ExpectedResult` ;
- données de test systématiquement fictives (`Camille`, `Alex`, montants inventés) — jamais de donnée personnelle réelle, même en local ;
- pas de test dédié pour un simple getter ou une configuration triviale.

---

# 9. Commentaires

Le code n'est pas commenté pour répéter ce qu'il fait. Un commentaire est réservé à une règle non évidente, une contrainte externe, un contournement documenté — jamais une paraphrase du code.

---

# 10. Git

## 10.1 Commits

```text
type(scope): description
```

Types : `feat`, `fix`, `docs`, `test`, `refactor`, `chore`. Un commit = un changement cohérent, jamais une fonctionnalité mélangée à un reformatage global.

## 10.2 Fichiers Unity

Toujours committer les fichiers `.meta` associés à chaque asset. Ne jamais versionner `Library/`, `Temp/`, `Logs/`, `UserSettings/` (cf. `.gitignore`).

---

# 11. Pratiques explicitement évitées

```text
uGUI / Canvas (UI Toolkit uniquement, cf. 04-Stack_technique.md)
Bibliothèque de graphiques tierce
Framework DI tant qu'il n'est pas nécessaire
ORM complexe au-dessus de SQLite
Traitement asynchrone façon Messenger pour un volume qui ne le justifie pas
Toute forme d'appel réseau
```

---

# 12. Compatibilité .NET

Ne jamais supposer qu'un membre BCL récent (.NET Standard 2.1+, ex. `Dictionary.GetValueOrDefault`) est disponible sans l'avoir vu compiler dans l'assembly concerné — vérifié empiriquement que `FinanceOS.UI` résout un ensemble de références plus restreint que `FinanceOS.App` au sein du même projet (`09-Decisions_techniques.md`, ADR-114 ; même famille de constat qu'ADR-110/ADR-111). Utiliser le motif portable `dict.TryGetValue(key, out var value) ? value : fallback` plutôt que `GetValueOrDefault`.
