# Moteur de prévision

# Finance OS Desktop

---

# 1. Objectif

Porter en C# le moteur de prévision de l'ancien projet (`10-Moteur_de_prevision.md`). L'algorithme ne change pas : il était déjà décrit de façon technologie-agnostique. Seuls disparaissent les éléments liés aux connecteurs, aux transactions en attente bancaires et au multi-utilisateur.

Le moteur doit rester : fiable, explicable, reproductible, idempotent, indépendant de toute source externe (trivialement vrai ici puisqu'il n'existe plus de source externe).

---

# 2. Séparation réel / prévu

Inchangée :

```text
TransactionEntry        = fait financier constaté
ForecastOccurrence       = événement financier attendu
```

Une occurrence rapprochée (`MatchedTransactionId` renseigné) n'est plus appliquée comme mouvement futur.

---

# 3. Entrée du moteur

```csharp
public sealed record ForecastRequest(
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlyList<int> AccountIds,
    ForecastOptions Options);

public sealed record ForecastOptions(
    bool IncludeSavingsTransfers = true,
    bool IncludeInvestmentAccounts = false,
    bool IncludeRemainingVariableBudget = true);
```

---

# 4. Algorithme

Identique à l'ancien projet, simplifié des étapes propres aux connecteurs :

1. déterminer le solde de départ (dernier solde officiel connu ≤ `DateFrom`, sinon solde initial du compte) ;
2. charger les transactions comptabilisées postérieures au solde de départ ;
3. charger les occurrences futures non rapprochées, non annulées ;
4. convertir le tout en une liste unique de `ForecastEvent` ;
5. trier chronologiquement ;
6. appliquer les mouvements jour par jour ;
7. produire la timeline quotidienne et les indicateurs (solde de fin, point bas, reste à vivre) ;
8. produire les explications associées à chaque valeur affichée.

```csharp
public sealed record ForecastEvent(
    int? SourceOccurrenceId,
    int? SourceTransactionId,
    DateOnly Date,
    long AmountMinor,
    string Label,
    ForecastEventCertainty Certainty);

public enum ForecastEventCertainty
{
    Confirmed,   // transaction réelle comptabilisée
    Expected,    // occurrence issue d'une opération récurrente active
    Estimated,   // occurrence ponctuelle ou montant variable estimé
}
```

La V1 simplifie les quatre niveaux qualitatifs de l'ancien moteur de décision (`CONFIRMED / HIGHLY_LIKELY / ESTIMATED / UNKNOWN`) à trois : un usage strictement local et manuel ne justifie pas la granularité qu'imposait la diversité des connecteurs.

---

# 5. Génération des occurrences récurrentes

Clé logique inchangée : `recurringOperationId + échéance logique`, garantissant l'idempotence via la contrainte SQL `uq_occurrence_logical_key` (cf. `03-Modele_de_donnees.md`). Une modification d'une opération récurrente avec date d'effet ne touche jamais les occurrences passées ou déjà rapprochées.

Règle de fin de mois inchangée : une occurrence prévue le 31 utilise le dernier jour du mois si celui-ci n'existe pas.

---

# 6. Montants variables

Modes conservés, simplifiés : `FixedAmount`, `LastKnownAmount`, `RecentAverage` (3 ou 6 dernières occurrences rapprochées). Les modes `SeasonalAverage` et `ScheduledAmount` (liés à un échéancier de facture, donc au module Documents) sont retirés de la V1.

---

# 7. Rapprochement

Simplifié (cf. `03-Modele_de_donnees.md` §11) : à la saisie ou à l'import d'une transaction, le moteur propose une occurrence correspondante selon les mêmes critères que l'ancien projet (compte identique obligatoire, montant proche, date proche, catégorie/tiers si renseignés). La confirmation reste toujours manuelle ; aucun seuil de confirmation automatique n'est nécessaire pour un usage local où l'utilisateur saisit ou importe lui-même ses données.

---

# 8. Virements internes

Neutralisés dans les vues consolidées, comme dans l'ancien projet : un virement entre deux comptes personnels ne modifie pas le patrimoine total, seulement la trésorerie de chaque compte.

---

# 9. Simulation

Conservée à l'identique : calcul en mémoire, aucune persistance, comparaison avant/après sur le solde de fin de période et le point bas.

---

# 10. Ce qui est retiré du moteur de l'ancien projet

```text
Gestion des transactions en attente bancaires (pending) — n'existe plus sans connecteur
Score de confiance de rapprochement automatique et confirmation automatique — inutile en usage 100% manuel
Cartes à débit différé avancées, échéanciers de documents
Consolidation multi-devise — hors périmètre V1 (EUR uniquement)
Le moteur d'aide à la décision complet (règles financières, objectifs, comparaison de scénarios) — reporté après validation du socle, cf. 01-Perimetre.md
```

---

# 11. Organisation du code (`Assets/Scripts/Forecast/`)

```text
Forecast/
├── ForecastCalculator.cs
├── ForecastEventBuilder.cs
├── ForecastOccurrenceGenerator.cs
├── VariableAmountEstimator.cs
└── ReconciliationSuggester.cs
```

Toutes ces classes sont pur C#, sans dépendance à `UnityEngine`, testables en Edit Mode exactement comme `ForecastCalculatorTest` l'était en PHPUnit.

---

# 12. Tests unitaires prioritaires

Repris à l'identique de l'ancien projet (les scénarios ne dépendent pas de la technologie) :

```text
Solde simple (solde initial - dépense prévue = résultat)
Revenu et dépense le même mois
Occurrence rapprochée remplaçant la prévision
Virement interne neutralisé en vue consolidée
Génération idempotente (deux générations, pas de doublon)
Modification future sans impact rétroactif
Ajustement manuel non écrasé par une régénération
Occurrence mensuelle tombant le dernier jour de février
Calcul correct du point bas et de sa date
Simulation sans persistance d'aucune donnée
```
