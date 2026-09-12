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
public sealed record ForecastRequest(DateTime DateFrom, DateTime DateTo);
```

La V1 implémentée calcule un compte à la fois (voir §11) ; la consolidation multi-comptes se fait en sommant les projections individuelles, ce qui neutralise déjà naturellement un virement interne entre deux comptes inclus (§8). `ForecastOptions` n'a pas été nécessaire pour la V1 : aucune des options envisagées (virements d'épargne, comptes d'investissement, budget variable restant) n'a encore de consommateur réel.

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
    int AccountId,
    int? SourceTransactionId,
    int? SourceOccurrenceId,
    DateTime Date,
    long AmountMinor,
    string Label,
    ForecastEventCertainty Certainty,
    bool IsTransfer);

public enum ForecastEventCertainty
{
    Confirmed,   // transaction réelle comptabilisée
    Expected,    // occurrence issue d'une opération récurrente active
    Estimated,   // occurrence ponctuelle ou montant variable estimé
}
```

`IsTransfer` (occurrence dont `DestinationAccountId` est renseigné, ou transaction dont `IsInternalTransfer` est vrai) exclut l'événement des totaux revenus/dépenses attendus sans l'exclure du calcul de solde — cf. §8.

La V1 simplifie les quatre niveaux qualitatifs de l'ancien moteur de décision (`CONFIRMED / HIGHLY_LIKELY / ESTIMATED / UNKNOWN`) à trois : un usage strictement local et manuel ne justifie pas la granularité qu'imposait la diversité des connecteurs.

---

# 5. Génération des occurrences récurrentes

Clé logique inchangée : `recurringOperationId + échéance logique`, garantissant l'idempotence via la contrainte SQL `uq_occurrence_logical_key` (cf. `03-Modele_de_donnees.md`). Une modification d'une opération récurrente avec date d'effet ne touche jamais les occurrences passées ou déjà rapprochées.

Règle de fin de mois inchangée : une occurrence prévue le 31 utilise le dernier jour du mois si celui-ci n'existe pas.

---

# 6. Montants variables

Modes conservés, simplifiés : `FixedAmount`, `LastKnownAmount`, `RecentAverage` (3 ou 6 dernières occurrences rapprochées). Les modes `SeasonalAverage` et `ScheduledAmount` (liés à un échéancier de facture, donc au module Documents) sont retirés de la V1.

**Statut d'implémentation** : `RecurringOperation` ne porte pour l'instant qu'un unique `ExpectedAmountMinor` fixe — les colonnes `amount_mode`/`estimation_mode` n'existent pas encore dans le schéma (`03-Modele_de_donnees.md`). `VariableAmountEstimator` est donc différé : l'ajouter maintenant produirait une classe sans donnée réelle à exploiter. À construire lorsque le besoin d'un montant variable se manifeste concrètement à l'usage, avec la migration de schéma correspondante.

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
├── ForecastEvent.cs
├── ForecastEventBuilder.cs
├── ForecastCalculator.cs
├── ForecastOccurrenceGenerator.cs
├── ReconciliationSuggester.cs
└── IsExternalInitPolyfill.cs
```

Toutes ces classes sont pur C#, sans dépendance à `UnityEngine` (`FinanceOS.Forecast.asmdef` a `noEngineReferences: true`, comme `FinanceOS.Domain`) — testables en Edit Mode exactement comme `ForecastCalculatorTest` l'était en PHPUnit. `VariableAmountEstimator` n'existe pas encore, cf. §6.

`IsExternalInitPolyfill.cs` n'est pas une classe métier : c'est un correctif technique nécessaire pour utiliser des `record`/propriétés `init` (C# 9) sous le runtime scripting d'Unity, qui ne fournit pas le type marqueur correspondant — même famille de contrainte que `DateOnly` (cf. `09-Decisions_techniques.md`, ADR-110 et ADR-111).

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
