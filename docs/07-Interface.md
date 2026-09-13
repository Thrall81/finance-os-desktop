# Interface

# Finance OS Desktop

---

# 1. Objectif

Adapter les principes d'interface de l'ancien `09-Frontend.md` à une application desktop Unity/UI Toolkit mono-fenêtre, mono-utilisateur, sans navigateur.

Principes inchangés : priorité aux informations essentielles, distinction stricte réel/prévu, jamais d'information transmise uniquement par la couleur, formulaires simples, retour immédiat après chaque action. Principe ajouté avec ce document : **réduire au maximum la saisie répétitive**, puisqu'il n'existe aucun connecteur pour l'éviter autrement.

---

# 2. Structure générale

Une fenêtre unique, sans onglets de navigateur ni URL. Barre de navigation latérale repliable + zone de contenu principale, dans le même esprit que le shell Angular de l'ancien projet :

```text
+------------------------------------------------------+
| Finance OS Desktop                            [_][□][X]|
+----------------------+---------------------------------+
| Tableau de bord       | Contenu de l'écran actif        |
| Comptes                |                                 |
| Transactions           |                                 |
| Prévisions             |                                 |
| Budget                 |                                 |
| Paramètres             |                                 |
+----------------------+---------------------------------+
```

---

# 3. Écrans de la V1

```text
Premier lancement (affiché une seule fois, cf. §4)
Tableau de bord
Comptes (liste, création, modification, détail)
Transactions (liste, création, modification, import CSV)
Opérations récurrentes (liste, création, modification)
Prévisions (synthèse, timeline, liste des occurrences, file de vérification, simulation)
Budget mensuel
Paramètres (devise, horizon, seuil, emplacement des données, export)
```

---

# 4. Premier lancement

Tant qu'aucun compte n'existe en base, la fenêtre principale ouvre sur un parcours en 3 à 4 étapes plutôt que sur le tableau de bord (cf. `01-Perimetre.md` §2.1) :

```text
Étape 1 — Bienvenue
  Explication en une phrase de ce que fait l'application (prévision, pas comptabilité).

Étape 2 — Premier compte
  Nom, type, solde actuel. Peut en ajouter d'autres, ou continuer avec un seul.

Étape 3 — Charges et revenus principaux (facultative, clairement marquée « Passer »)
  Salaire, loyer, une ou deux factures récurrentes — formulaire minimal,
  complétable plus tard depuis l'écran Opérations récurrentes.

Étape 4 — Récapitulatif
  Ce qui a été créé, bouton « Terminer » qui ouvre le tableau de bord.
```

Navigation clavier complète, `aria`-équivalent UI Toolkit pour l'étape courante. Quitter à n'importe quelle étape conserve les données déjà saisies. Ce parcours ne réapparaît jamais une fois un premier compte créé — y compris si l'utilisateur n'a complété que l'étape 2.

---

# 5. Tableau de bord

Cartes reprises de l'ancien projet : Solde disponible, Solde prévu en fin de mois, Point bas prévisionnel, Reste à vivre — puis graphique de trésorerie, **file de vérification** (occurrences arrivées à échéance non confirmées, mise en avant si non vide), prochaines opérations, synthèse budgétaire, alertes calculées à l'affichage (solde faible, dépassement de budget).

**État d'implémentation** : `Dashboard.uxml` + `DashboardController` affichent Solde disponible, Solde prévu en fin de mois, Point bas prévisionnel et la file de vérification, alimentés par `DashboardViewModelBuilder` (`Assets/Scripts/UI/`). Reste à vivre, prochaines opérations, synthèse budgétaire, alertes et le graphique de trésorerie ne sont pas encore construits — le graphique en particulier est différé volontairement : son rendu `Painter2D` ne peut être vérifié qu'à l'exécution réelle (un rendu), impossible à contrôler en mode batch dans cet environnement de développement (cf. `09-Decisions_techniques.md`, ADR-112). Aucune police personnalisée n'est encore intégrée : l'interface utilise la police par défaut d'Unity en attendant une passe de style dédiée.

**Navigation** : une barre latérale (`Shell.uxml` + `ShellController`) donne accès au Tableau de bord, à Comptes, à Transactions et à Opérations récurrentes ; chaque écran est un `VisualTreeAsset` instancié dans la zone de contenu du shell plutôt qu'une scène séparée (cf. `09-Decisions_techniques.md`, ADR-113). Les autres entrées listées en §3 (Prévisions, Budget, Paramètres) n'ont pas encore d'entrée dans la barre latérale.

**Comptes — état d'implémentation** : `Accounts.uxml` + `AccountsController` couvrent la liste (actifs puis archivés), la création et — en modification — le renommage, la politique de liquidité et l'archivage/restauration ; alimentés par `AccountsViewModelBuilder`, qui ne dépend que d'`AccountService` (pas de tout l'`AppContainer`). Le type et le solde initial d'un compte existant ne sont pas modifiables depuis cet écran — cohérent avec `AccountService`, qui n'expose pas ces mutations une fois le compte créé ; ajuster un solde passe par `RecordOfficialBalance` (pas encore relié à une action d'écran). La vue détail dédiée (au-delà du formulaire d'édition inline) n'existe pas encore.

**Transactions — état d'implémentation** : `Transactions.uxml` + `TransactionsController` couvrent la liste (`MultiColumnListView`, colonnes Date/Compte/Libellé/Catégorie/Montant, filtrable par compte), la création — Dépense, Revenu ou Virement interne (via `InternalTransferService`, jusque-là inutilisé par aucun écran) — et, en modification, la catégorie, le tiers, les notes et l'exclusion du budget ainsi que la suppression, cohérent avec ce qu'expose `TransactionService` (compte, date et montant ne sont pas modifiables après création). La catégorisation assistée (§7) fonctionne dès la saisie du libellé : `TransactionService.CreateManual` normalise et mémorise automatiquement chaque libellé saisi manuellement (`LabelNormalization`, `FinanceOS.App`), donc la suggestion marche sans étape de correction préalable. Alimenté par `TransactionsViewModelBuilder` (`AccountService`/`CategoryService`/`CounterpartyService`/`TransactionService`, pas tout l'`AppContainer`). L'import CSV n'est pas construit (hors périmètre V1, cf. `01-Perimetre.md`).

**Opérations récurrentes — état d'implémentation** : `RecurringOperations.uxml` + `RecurringOperationsController` couvrent la liste (`MultiColumnListView`, colonnes Nom/Type/Compte/Fréquence/Montant/Statut), la création — Dépense, Revenu, Virement épargne ou Virement interne, avec les champs compte(s) qui s'adaptent au type choisi — et, en modification, uniquement le montant attendu et la suspension/reprise, cohérent avec ce qu'expose `RecurringOperationService` (nom, type, comptes, fréquence, date de début, jour du mois, catégorie et tiers ne sont modifiables qu'à la création). `RecurringOperationService.GenerateUpcomingOccurrences` (nouveau) est appelé au démarrage de l'application et après chaque création/reprise, pour que les occurrences futures existent réellement en base sans étape manuelle — jusque-là, seuls les tests appelaient la génération. La création d'une occurrence ponctuelle sans récurrence (§2.7 de `01-Perimetre.md`) n'est pas construite sur cet écran, plutôt prévue pour l'écran Prévisions.

---

# 6. File de vérification et confirmation rapide

Élément central pour transformer le prévisionnel en réel sans ressaisie (cf. `01-Perimetre.md` §2.7, `06-Moteur_de_prevision.md` §7).

- une occurrence dont la date prévue est atteinte ou dépassée, et qui n'est ni rapprochée ni annulée, apparaît dans la file « Opérations à vérifier » ;
- affichée sur le tableau de bord (nombre + aperçu des 3 premières) et en liste complète dans l'écran Prévisions ;
- chaque ligne propose directement les actions **C'est arrivé** et **Pas encore** (repousse silencieusement, reste dans la file) ;
- **C'est arrivé** ouvre un mini-formulaire préempli avec la date et le montant attendus, tous deux modifiables, puis crée la transaction réelle et marque l'occurrence comme rapprochée en une seule confirmation — jamais deux écrans séparés ;
- une occurrence trop ancienne sans confirmation (seuil configurable, ex. 15 jours) passe en état « Manquée » visible mais non bloquante, avec la possibilité de la confirmer tardivement ou de l'annuler.

---

# 7. Catégorisation assistée

À la saisie manuelle ou pendant le mapping d'un import CSV (cf. `01-Perimetre.md` §2.4) :

- si le libellé normalisé d'une transaction correspond à un libellé déjà catégorisé, la catégorie est préremplie automatiquement dans le champ, visuellement marquée comme « suggestion » (ex. léger fond distinct) plutôt que comme un choix déjà validé ;
- un seul clic confirme (ou la validation du formulaire l'accepte implicitement) ; changer la catégorie met à jour la mémorisation pour la prochaine fois ;
- à l'import CSV en masse, les lignes dont le libellé est reconnu affichent déjà leur catégorie suggérée dans l'aperçu, réduisant le travail de tri après import.

---

# 8. Graphiques — rendu maison UI Toolkit

## 8.1 Décision

Tous les graphiques sont des `VisualElement` personnalisés qui redéfinissent `generateVisualContent` et dessinent avec `Painter2D` (cf. `13-Decisions_techniques.md`, ADR-103). Pas de bibliothèque tierce.

## 8.2 Types nécessaires en V1

| Graphique | Type | Écran |
|---|---|---|
| Courbe de trésorerie | Ligne, avec segment plein (réel) puis pointillé (prévu), marqueur du point bas | Tableau de bord, Prévisions |
| Budget prévu/réel/engagé | Barres groupées par catégorie | Budget |
| Répartition des dépenses | Anneau (donut) | Tableau de bord |
| Évolution de l'épargne | Ligne ou barres | Budget |

## 8.3 Composant `LineChartElement` (exemple)

```csharp
public sealed class LineChartElement : VisualElement
{
    public IReadOnlyList<ForecastDayPoint> Points { get; set; } = Array.Empty<ForecastDayPoint>();

    public LineChartElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext context)
    {
        var painter = context.painter2D;
        // tracé du segment "réel" en trait plein, du segment "prévu" en pointillé,
        // marqueur au point bas, grille de fond, axes légendés.
    }
}
```

## 8.4 Exigences conservées de l'ancien projet

- chaque graphique possède un titre et, si utile, une légende ;
- une infobulle apparaît au survol d'un point/d'une barre (valeur exacte, date, détail) ;
- une alternative textuelle existe toujours à côté du graphique (les valeurs principales restent lisibles sans lui) ;
- aucune information n'est transmise uniquement par la couleur (le trait plein/pointillé porte déjà la distinction réel/prévu) ;
- comportement correct géré pour l'absence de données.

---

# 9. Formulaires

Reactive-style : validation locale immédiate (UI Toolkit `INotifyValueChanged` + validation C#), bouton de soumission désactivé pendant le traitement, erreurs affichées près du champ concerné. Montants saisis par l'utilisateur en euros, convertis en centimes avant d'atteindre la couche `App`.

Principe de réduction de friction appliqué systématiquement : chaque champ qui peut avoir une valeur par défaut raisonnable en a une, pré-sélectionnée mais toujours modifiable. Exemple concret : à la création d'un compte, la politique de liquidité (`immediate`/`reserve`/`excluded`) est automatiquement proposée selon le type choisi (courant → immédiat, épargne → réserve, investissement/dette → exclu) — l'utilisateur n'a pas besoin de comprendre le concept pour créer un compte fonctionnel, seulement s'il souhaite l'ajuster.

---

# 10. Tableaux

`MultiColumnListView` pour les transactions et occurrences : tri par colonne, défilement virtualisé (nécessaire dès plusieurs années d'historique), sélection, navigation clavier.

---

# 11. États

Chaque écran prévoit : chargement, succès, aucune donnée (état vide explicatif avec action, jamais un simple « Aucune donnée »), erreur (avec message compréhensible — ici, une erreur signifie presque toujours un problème local disque/fichier, jamais un problème réseau).

---

# 12. Présentation des montants

Composant central `MoneyLabel` : reçoit un montant en centimes + une devise, affiche `1 234,56 €` / `−82,35 €` / `+2 100,00 €`. Aucune vue n'effectue de division par 100 directement.

---

# 13. Accessibilité

Toujours pertinente même sans obligation réglementaire externe : navigation clavier complète, focus visible, contrastes suffisants, labels associés aux champs. UI Toolkit fournit une bonne base native (focus ring, tabulation) à ne pas casser par un style personnalisé excessif.

---

# 14. Ce qui disparaît de l'ancien Frontend

Authentification, guards de route, interceptors HTTP, CSRF, CORS, gestion multi-onglets navigateur, responsive mobile/tablette (l'application cible uniquement une fenêtre desktop redimensionnable), internationalisation (français uniquement, pas anticipé pour l'instant).
