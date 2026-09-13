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

**État d'implémentation** : la zone de contenu de chaque écran est un `ScrollView` (pas une simple `VisualElement`) — un écran chargé de plusieurs cartes peut dépasser la hauteur de la fenêtre, et seul le contenu défile, jamais la barre latérale. Les tableaux internes (`MultiColumnListView`) gardent en plus leur propre défilement pour de longues listes.

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

**État d'implémentation** : `Dashboard.uxml` + `DashboardController` affichent Solde disponible, Solde prévu en fin de mois, Point bas prévisionnel, Reste à vivre, la courbe de trésorerie (`LineChartElement`, §8, ADR-119 — même patron que Prévisions, horizon fin du mois courant), la répartition des dépenses (`ExpenseDonutElement`, §8, ADR-122 — dépenses réelles du mois en cours, catégorie par catégorie, compte principal uniquement) et la file de vérification, alimentés par `DashboardViewModelBuilder` (`Assets/Scripts/UI/`). Reste à vivre réutilise la définition de `BudgetOverview.RemainingToLiveMinor` (ADR-115, `BudgetService.GetOverview`) — donc, contrairement aux autres chiffres de cet écran, ni calculé pour un compte en particulier (un budget ne l'est pas), ni disponible sans qu'un budget existe pour le mois courant : la carte affiche « — » plutôt qu'un calcul silencieusement erroné tant qu'aucun budget n'est créé. « Prochaines opérations » (ADR-126) liste, pour le compte principal, les 5 prochaines occurrences non encore échues (strictement après aujourd'hui — celles échues du jour sont déjà dans la file de vérification juste au-dessus, pas dupliquées ici), avec un badge « Attendue » (liée à une opération récurrente) ou « Estimée » (occurrence ponctuelle sans récurrence), même distinction que `FinanceOS.Forecast.ForecastEventCertainty` en interne. « Synthèse budgétaire » (ADR-127) reprend les mêmes figures que le tableau des catégories de l'écran Budget (`BudgetService.GetSummary`), en barres de progression compactes plutôt qu'en colonnes — même non-disponibilité sans budget créé pour le mois courant que Reste à vivre (§11, état vide explicite plutôt que silencieux). Alertes calculées à l'affichage ne sont pas encore construites.

**Navigation** : une barre latérale (`Shell.uxml` + `ShellController`) donne accès aux sept écrans de §3 — Tableau de bord, Comptes, Transactions, Opérations récurrentes, Prévisions, Budget et Paramètres ; chaque écran est un `VisualTreeAsset` instancié dans la zone de contenu du shell plutôt qu'une scène séparée (cf. `09-Decisions_techniques.md`, ADR-113).

**Comptes — état d'implémentation** : `Accounts.uxml` + `AccountsController` couvrent la liste (actifs puis archivés), la création et — en modification — le renommage, la politique de liquidité et l'archivage/restauration ; alimentés par `AccountsViewModelBuilder`, qui ne dépend que d'`AccountService` (pas de tout l'`AppContainer`). Le type et le solde initial d'un compte existant ne sont pas modifiables depuis cet écran — cohérent avec `AccountService`, qui n'expose pas ces mutations une fois le compte créé ; ajuster un solde passe par `RecordOfficialBalance` (pas encore relié à une action d'écran). La vue détail dédiée (au-delà du formulaire d'édition inline) n'existe pas encore.

**Transactions — état d'implémentation** : `Transactions.uxml` + `TransactionsController` couvrent la liste (`MultiColumnListView`, colonnes Date/Compte/Libellé/Catégorie/Montant, filtrable par compte), la création — Dépense, Revenu ou Virement interne (via `InternalTransferService`, jusque-là inutilisé par aucun écran) — et, en modification, la catégorie, le tiers, les notes et l'exclusion du budget ainsi que la suppression, cohérent avec ce qu'expose `TransactionService` (compte, date et montant ne sont pas modifiables après création). La catégorisation assistée (§7) fonctionne dès la saisie du libellé : `TransactionService.CreateManual` normalise et mémorise automatiquement chaque libellé saisi manuellement (`LabelNormalization`, `FinanceOS.App`), donc la suggestion marche sans étape de correction préalable. Alimenté par `TransactionsViewModelBuilder` (`AccountService`/`CategoryService`/`CounterpartyService`/`TransactionService`, pas tout l'`AppContainer`). L'import CSV n'est pas construit (hors périmètre V1, cf. `01-Perimetre.md`).

**Opérations récurrentes — état d'implémentation** : `RecurringOperations.uxml` + `RecurringOperationsController` couvrent la liste (`MultiColumnListView`, colonnes Nom/Type/Compte/Fréquence/Montant/Statut), la création — Dépense, Revenu, Virement épargne ou Virement interne, avec les champs compte(s) qui s'adaptent au type choisi — et, en modification, uniquement le montant attendu et la suspension/reprise, cohérent avec ce qu'expose `RecurringOperationService` (nom, type, comptes, fréquence, date de début, jour du mois, catégorie et tiers ne sont modifiables qu'à la création). `RecurringOperationService.GenerateUpcomingOccurrences` (nouveau) est appelé au démarrage de l'application et après chaque création/reprise, pour que les occurrences futures existent réellement en base sans étape manuelle — jusque-là, seuls les tests appelaient la génération. La création d'une occurrence ponctuelle sans récurrence (§2.7 de `01-Perimetre.md`) n'est pas construite sur cet écran, plutôt prévue pour l'écran Prévisions.

Deux garde-fous ajoutés après un incident réel (ADR-120) : à la création, si la date de début et le jour du mois choisis produisent un premier versement qui saute un cycle entier (ex. jour du mois 28 avec une date de début le 29), un avertissement non bloquant (`form-skip-warning`, ambre — `PreviewFirstOccurrenceDate`, pure prévision, rien n'est persisté) indique la vraie première date avant validation. Et puisque la date de début/le jour du mois restent non modifiables après création, une opération récurrente peut désormais être **supprimée** — mais seulement tant qu'aucune de ses occurrences n'a été confirmée comme réelle (`RecurringOperationService.Delete`, refuse sinon) ; la suppression entraîne celle de ses occurrences non rapprochées (`ON DELETE CASCADE`). C'est aujourd'hui le seul moyen de corriger une erreur de saisie sur ces deux champs — un chemin d'édition plus permissif reste possible plus tard si le besoin se confirme.

**Prévisions — état d'implémentation** : `Forecasts.uxml` + `ForecastsController` couvrent, pour le compte sélectionné, la synthèse (solde actuel, solde prévu en fin d'horizon, point bas, revenus/dépenses attendus, avertissements du moteur), la file de vérification complète (avec un formulaire de confirmation partagé pour « C'est arrivé », préempli et modifiable, plus une action « Annuler » directe), la liste des occurrences prévues (`MultiColumnListView`) et la simulation « Et si ? » (jamais persistée). La courbe de trésorerie (`LineChartElement`, §8, ADR-119) est maintenant construite au-dessus du « Journal des mouvements prévus », qui reste son alternative textuelle explicite (§8.4) — un jour par ligne, uniquement les jours avec un mouvement. Simplification assumée par rapport à §6 : le bouton « Pas encore » n'existe pas séparément — comme il ne change aucune donnée (« repousse silencieusement, reste dans la file »), ne pas ouvrir le formulaire de confirmation produit exactement le même résultat.

Cet écran empile six cartes (synthèse, vérification, occurrences, journal, simulation) — trop pour tenir dans une fenêtre, ce qui rendait chaque petit défaut de marge très visible (`09-Decisions_techniques.md`, ADR-116). La section Simulation est donc **repliée par défaut**, réduite à son titre et un bouton « Simuler un scénario » ; le formulaire ne s'affiche qu'au clic. Le même traitement pourra s'appliquer au Journal des mouvements prévus si la densité reste un problème une fois un vrai compte en usage courant observé.

Deux vraies lacunes fonctionnelles corrigées à l'occasion de cet écran (aucune des deux n'était un problème d'UI) : `ForecastOccurrenceService.MarkStaleAsMissed` (nouveau) fait enfin passer une occurrence non confirmée au-delà du seuil configurable (`AppSettings.MissedThresholdDays`, 15 jours par défaut) à l'état « Manquée » — rien ne le faisait avant, `ForecastOccurrence.MarkAsMissed()` existait mais n'était jamais appelé. Et `ForecastOccurrenceRepository.ListDueForVerification` inclut désormais aussi le statut `missed`, pas seulement `planned` — sinon une occurrence manquée aurait silencieusement disparu de la file de vérification (dashboard compris) au lieu d'y rester visible comme le §6 le décrit. Les deux sont appelées au démarrage de l'application (`AppBootstrap`).

**Paramètres — état d'implémentation** : `Settings.uxml` + `SettingsController` couvrent l'horizon de prévision, le seuil de solde faible, le seuil « Manquée » (§6, `AppSettings.MissedThresholdDays`) et le compte par défaut du tableau de bord — un seul formulaire, un seul bouton Enregistrer, plutôt que le patron créer/modifier des autres écrans puisqu'il n'existe qu'un seul objet réglages. La devise est affichée en lecture seule (EUR fixe en V1, cf. `01-Perimetre.md` §2.11). L'emplacement du fichier de données est affiché (lecture seule, bouton « Copier le chemin ») et une sauvegarde manuelle est disponible : `BackupService` (nouveau, `FinanceOS.App`) copie le fichier SQLite vivant vers un dossier `backups/` horodaté à côté des données — seule la copie de fichier est construite, pas l'export JSON que `01-Perimetre.md` §2.11 propose en alternative (« copie du fichier SQLite **ou** export JSON » — un seul des deux suffit pour ce besoin).

**Budget — état d'implémentation** : `Budgets.uxml` + `BudgetsController` couvrent la navigation par mois (◀ / ▶), la création du budget du mois affiché — avec l'option « copier les montants du mois précédent » (§2.9) — l'activation/clôture, les allocations par catégorie (ajout, modification du seul montant prévu, retrait) avec le suivi prévu/réel/engagé/restant (`MultiColumnListView`), et la synthèse du mois (reste à vivre, revenus, épargne, taux d'épargne). Un mois sans budget affiche un état vide explicite avec l'action de création plutôt que d'en créer un silencieusement — seul le mois courant, au premier affichage de l'écran, est prêt automatiquement. Entre la synthèse et le tableau des catégories : l'évolution de l'épargne sur 6 mois (`SavingsEvolutionElement`, §8, ADR-123), puis les barres prévu/réel/engagé (`BudgetBarChartElement`, §8) — les deux masqués comme le reste tant qu'aucun budget n'existe pour le mois affiché, même si l'évolution de l'épargne, elle, ne dépend d'aucun budget créé pour être calculée (simplification assumée : un seul interrupteur d'affichage pour tout l'écran plutôt qu'un cas particulier pour cette seule carte).

Aucune définition précise de « reste à vivre » et « taux d'épargne du mois » n'existait dans la documentation avant cet écran (seule la maquette montrait un nombre) — définitions retenues, ajoutées à `BudgetService.GetOverview`/`BudgetOverview` : le reste à vivre est la somme du restant (prévu − réel − engagé) de chaque catégorie de type Dépense allouée au budget ; le taux d'épargne est le rapport (mouvements réel+engagé vers une catégorie de type Épargne) / (mouvements réel+engagé vers une catégorie de type Revenu) du mois, 0 % en l'absence de revenu. Les deux se recalculent uniquement à partir de ce que `GetSummary`/les périodes de transactions et occurrences fournissent déjà — aucune nouvelle donnée stockée.

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

| Graphique | Type | Écran | État |
|---|---|---|---|
| Courbe de trésorerie | Ligne, avec segment plein (réel) puis pointillé (prévu), marqueur du point bas | Prévisions, Tableau de bord | **Construit** (`LineChartElement`) |
| Budget prévu/réel/engagé | Barres groupées par catégorie | Budget | **Construit** (`BudgetBarChartElement`) |
| Répartition des dépenses | Anneau (donut) | Tableau de bord | **Construit** (`ExpenseDonutElement`) |
| Évolution de l'épargne | Ligne ou barres | Budget | **Construit** (`SavingsEvolutionElement`, barres) |

La courbe de trésorerie sur Prévisions a été confirmée par capture d'écran le 2026-09-13 (ADR-119) puis répliquée telle quelle sur le Tableau de bord — même composant, mêmes couleurs, horizon différent (fin du mois courant plutôt que l'horizon de prévision réglable).

## 8.3 Composant `LineChartElement`

```csharp
public sealed class LineChartElement : VisualElement
{
    public IReadOnlyList<ChartPointViewModel> Points { get; set; } = Array.Empty<ChartPointViewModel>();

    public LineChartElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext context)
    {
        var painter = context.painter2D;
        // tracé du segment "réel" en trait plein, du segment "prévu" en pointillé (dessiné à la
        // main — Painter2D n'a pas de propriété de pointillé native, cf. ADR-119),
        // marqueur en losange au point bas.
    }
}
```

`Points` prend un `ChartPointViewModel` (`FinanceOS.UI`, déjà formaté pour l'écran) plutôt qu'un `ForecastDayPoint` brut du moteur (`FinanceOS.Forecast`) — cohérent avec le reste de l'UI, qui ne référence jamais un type du moteur directement (`ForecastsViewModelBuilder` fait la conversion). Pas de grille de fond ni d'axes légendés dans cette première version — jugé pas indispensable une fois le solde déjà affiché en toutes lettres dans la synthèse et le journal juste en dessous.

## 8.3bis Composant `BudgetBarChartElement`

Même famille que `LineChartElement` (`Painter2D`, aucune bibliothèque tierce), pour les barres groupées prévu/réel/engagé du Budget (ADR-121). Différence structurelle : `Painter2D` ne dessine pas de texte, donc les noms de catégorie (indispensables pour lire un graphique à barres, contrairement à la courbe où les KPI environnants suffisaient) sont de vrais `Label` UI Toolkit, enfants du même `VisualElement`, repositionnés sous chaque groupe de barres à chaque changement de géométrie (`GeometryChangedEvent`) — le canevas dessine les barres, les enfants portent le texte. `Groups` prend un `BudgetChartBarGroupViewModel` (nom de catégorie + trois montants bruts) construit par `BudgetsViewModelBuilder` depuis `BudgetService.GetSummary`. Ordre gauche-à-droite fixe par groupe (prévu, réel, engagé), expliqué dans la légende textuelle de la carte plutôt que répété en légende graphique — c'est ce qui tient lieu de distinction « pas uniquement par la couleur » (§8.4) en l'absence d'un équivalent du trait plein/pointillé pour des barres.

## 8.3ter Composant `ExpenseDonutElement`

Troisième graphique de la même famille (`Painter2D`, ADR-103), sur le Tableau de bord. Chaque quartier est un polygone plein approximant un arc (bord extérieur puis retour par le bord intérieur, uniquement `MoveTo`/`LineTo`/`ClosePath`/`Fill`) — `Painter2D.Arc` volontairement évité, même raisonnement que le marqueur en losange de la courbe (ADR-119) : plus grande confiance avec des membres d'API déjà éprouvés qu'avec un jamais utilisé. `Slices` ne prend que des montants bruts (les proportions suffisent au calcul des angles) ; le nom de catégorie et le pourcentage, eux, sont du texte — et `Painter2D` n'en dessine pas. Solution : `DashboardController` construit une légende (`Label` + pastille de couleur, vrais `VisualElement`) à côté de l'anneau plutôt que d'essayer de peindre du texte sur le canevas — même schéma « canevas pour les formes, éléments UI pour le texte » que `BudgetBarChartElement` (ADR-121), mais ici la légende sert aussi de réponse à l'exigence « jamais uniquement par la couleur » (§8.4) : chaque couleur porte son nom de catégorie juste à côté. Alimenté par `DashboardViewModel.ExpenseBreakdown` (`ExpenseCategorySliceViewModel`), construit par `DashboardViewModelBuilder` à partir des dépenses réelles (transactions au montant négatif, hors virement interne, catégorie de type **Dépense** uniquement) du compte principal sur le mois courant — plus étroit que la colonne « réel » de `BudgetService.GetSummary`, qui somme par catégorie allouée sans regarder son type : une transaction catégorisée « Épargne » est un virement vers l'épargne, pas une dépense, donc exclue ici (corrigé en construisant `SavingsEvolutionElement`, où l'oubli est apparu en pratique — cf. ADR-123). Triées du plus gros au plus petit poste, dans le donut comme dans la légende.

## 8.3quater Composant `SavingsEvolutionElement`

Quatrième et dernier graphique du catalogue (§8.2), sur l'écran Budget — une barre par mois plutôt que des barres groupées (une seule série, donc aucun enjeu « pas uniquement par la couleur » : rien d'autre à distinguer). Mêmes techniques que les graphiques précédents : uniquement `MoveTo`/`LineTo` pour les barres, `Label` enfants repositionnés à chaque `GeometryChangedEvent` pour les libellés de mois (`DateFormat.MonthAbbreviationYear`, ex. « sept. 2026 » — trop étroit pour le nom complet du mois sous six barres). Alimenté par `BudgetService.GetSavingsEvolution` (nouveau), qui calcule l'épargne réelle + engagée des 6 derniers mois **indépendamment de l'existence d'un budget** pour ces mois-là — contrairement à `GetOverview`, dont dépendent les autres cartes de cet écran. Simplification assumée : la carte reste malgré tout masquée tant qu'aucun budget n'existe pour le mois *affiché*, même si la donnée elle-même ne le requiert pas — un seul interrupteur d'affichage pour tout l'écran plutôt qu'un cas particulier pour cette seule carte.

État vide explicite (`savings-empty`, § 11) quand les 6 mois sont tous à zéro — un vrai compte capturé en capture d'écran l'a montré : sans ce message, la zone de graphique reste visuellement vide (seuls les libellés de mois restent visibles), indiscernable d'un graphique cassé. Condition volontairement plus fine que celle du donut (liste vide) : ici la liste de points a toujours 6 éléments par construction, donc le vide est détecté par « aucun montant strictement positif », pas par un compte de zéro.

## 8.3quinquies Infobulles au survol (`ChartTooltip`)

Dernière case du §8.4 restée cochée « pas encore construite », faite une fois les quatre graphiques confirmés visuellement (ADR-124). `ChartTooltip` (`Assets/Scripts/UI/ChartTooltip.cs`, classe interne à `FinanceOS.UI`) factorise la seule partie réellement commune aux quatre : créer le `Label` flottant (classe `.chart-tooltip`, fond `--color-ink-900`), l'afficher/le positionner près du pointeur en le contenant dans les limites du graphique, le masquer. Chaque graphique garde son propre calcul géométrique — trouver le point/la barre/le quartier sous le pointeur diffère entièrement d'un graphique à l'autre, ça n'aurait pas eu de sens de le factoriser.

Chaque graphique expose sa fonction de détection (`FindNearestPointIndex`, `FindBarUnderPointer` ×2, `FindSliceUnderPointer`) comme méthode **statique et publique**, pure — aucune dépendance à l'instance — précisément pour rester vérifiable en batchmode : le mode batch ne peut pas simuler un `PointerMoveEvent` réel, donc `UISmokeTest.cs` appelle directement ces fonctions avec des coordonnées choisies plutôt que de simuler une interaction souris. C'est la même logique que `RecurringOperationService.PreviewFirstOccurrenceDate` (ADR-120) : rendre une fonction pure et publique spécifiquement pour la rendre testable sans dépendre du mécanisme qui l'invoque normalement.

`ExpenseDonutElement.Slices` change de type à cette occasion : de simples montants bruts (`IReadOnlyList<long>`) vers `IReadOnlyList<ExpenseCategorySliceViewModel>` — l'infobulle a besoin du nom de catégorie et du texte déjà formaté (montant, pourcentage), que la légende externe possédait déjà mais que l'élément lui-même n'avait pas. Les trois autres graphiques n'ont pas eu besoin de ce genre de changement, leurs view models portaient déjà tout le texte nécessaire.

## 8.4 Exigences conservées de l'ancien projet

Les quatre graphiques du catalogue (§8.2) sont désormais tous construits.

- chaque graphique possède un titre et, si utile, une légende ;
- une infobulle apparaît au survol d'un point/d'une barre (valeur exacte, date, détail) — **construite sur les quatre graphiques** (`ChartTooltip`, §8.3quinquies, ADR-124), une fois le tracé de base de chacun confirmé visuellement ;
- une alternative textuelle existe toujours à côté du graphique (les valeurs principales restent lisibles sans lui) — le « Journal des mouvements prévus » pour la courbe de trésorerie, le tableau des catégories (prévu/réel/engagé/restant) pour les barres du budget, la légende (nom + montant + pourcentage) pour l'anneau, les libellés de mois + la valeur d'épargne du mois courant déjà affichée dans la synthèse pour l'évolution de l'épargne ;
- aucune information n'est transmise uniquement par la couleur — le trait plein/pointillé porte la distinction réel/prévu sur la courbe, l'ordre gauche-à-droite fixe porte la distinction prévu/réel/engagé sur les barres du budget, le nom de catégorie en toutes lettres dans la légende porte l'identification de chaque quartier de l'anneau, et l'évolution de l'épargne n'a qu'une seule série (rien à distinguer) ;
- comportement correct géré pour l'absence de données — aucun des quatre graphiques ne lève d'exception sans données (`LineChartElement` : moins de deux points ; `BudgetBarChartElement` : pas de groupe ou valeur maximale nulle ; `ExpenseDonutElement` : pas de quartier ou total nul ; `SavingsEvolutionElement` : pas de point ou valeur maximale nulle).

---

# 8bis. Typographie

Identité retenue depuis la maquette (`docs/mockups/dashboard.html`), maintenant vendorisée et appliquée dans `Assets/UI/USS/theme.uss` :

- **Spectral** (empattements) pour la marque (« Finance OS » dans la barre latérale) et tous les titres — de page (`.page-title`) et de carte (`.card-title`) ;
- **IBM Plex Sans** pour le reste de l'interface — c'est la police de base, posée une seule fois sur `.shell-root` et héritée partout où rien d'autre n'est précisé ;
- **IBM Plex Mono** pour tout affichage de montant ou de date — jamais pour un champ de saisie libre (`.form-field` reste en Plex Sans, seul l'affichage l'exige, pas la frappe) : chiffres de KPI, montants et dates en tableau ou en liste, contexte de date sous un KPI.

Fichiers statiques uniquement (jamais de police variable) — voir `09-Decisions_techniques.md` ADR-118 pour pourquoi, et `Assets/Fonts/THIRD-PARTY-NOTICES.md` pour la provenance et les licences (SIL OFL 1.1 pour les trois familles). Chaque règle USS qui a besoin d'un poids précis pointe directement le fichier correspondant (`-unity-font-definition: url("../../Fonts/...")`) plutôt que de simuler un gras avec `-unity-font-style: bold` à partir d'un fichier Regular.

**Simplification assumée** : `.form-readonly-value` (valeurs en lecture seule dans les formulaires d'édition) reste en Plex Sans partout, y compris quand elle affiche un montant ou une date — cette classe sert aussi bien à afficher un nom de compte ou de catégorie, et la distinguer selon le contenu affiché aurait demandé une classe par contexte pour un gain visuel mineur à ce stade.

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
