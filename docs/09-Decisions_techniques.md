# Décisions techniques

# Finance OS Desktop

---

# 1. Objectif

Journal des décisions structurantes, dans le même format que l'ancien projet (`13-Decisions_techniques.md`). Les décisions ADR-001 à ADR-050 de l'ancien projet restent consultables comme historique mais ne s'appliquent plus telles quelles : ce document démarre une numérotation propre.

---

# 2. Index des décisions

| ID | Décision | Statut |
|---|---|---|
| ADR-101 | Abandonner l'hébergement web au profit d'une application desktop locale | ACCEPTED |
| ADR-102 | Utiliser Unity 6 comme moteur d'application | ACCEPTED |
| ADR-103 | Utiliser UI Toolkit exclusivement, graphiques faits maison via Painter2D | ACCEPTED |
| ADR-104 | Utiliser SQLite embarqué avec identifiants entiers auto-incrémentés | ACCEPTED |
| ADR-105 | Supprimer authentification, session et tout compte utilisateur | ACCEPTED |
| ADR-106 | Windows uniquement pour la V1 | ACCEPTED |
| ADR-107 | Supprimer tous les connecteurs externes | ACCEPTED |
| ADR-108 | Conserver l'import CSV manuel | ACCEPTED |
| ADR-109 | Repli du module Documents et du moteur d'aide à la décision avancé après la V1 | ACCEPTED |
| ADR-110 | Utiliser `DateTime` plutôt que `DateOnly` pour les dates civiles | ACCEPTED |
| ADR-111 | Polyfill `IsExternalInit` pour utiliser `record`/`init` | ACCEPTED |
| ADR-112 | Vérifier l'UI par liaison de données plutôt que par rendu ; différer le graphique | ACCEPTED |
| ADR-113 | Navigation par échange de contenu dans un UIDocument unique (pas de scènes multiples) | ACCEPTED |
| ADR-114 | Éviter les membres .NET Standard 2.1 (ex. `Dictionary.GetValueOrDefault`) dans `FinanceOS.UI` | ACCEPTED |
| ADR-115 | Définition retenue pour « reste à vivre » et « taux d'épargne du mois » | ACCEPTED |
| ADR-116 | Corrections de mise en page trouvées uniquement via captures d'écran réelles | ACCEPTED |
| ADR-117 | Défilement de page sur les 7 écrans ; repli de la section Simulation | ACCEPTED |
| ADR-118 | Polices statiques vendorisées (Spectral, IBM Plex Sans/Mono) plutôt que variables | ACCEPTED |
| ADR-119 | Graphique de trésorerie construit ; pointillé dessiné à la main (pas d'API native) | ACCEPTED |
| ADR-120 | Opérations récurrentes : avertissement de premier cycle sauté + suppression conditionnelle | ACCEPTED |
| ADR-121 | Graphique barres budget construit ; texte des catégories en `Label` superposés, pas en Painter2D | ACCEPTED |
| ADR-122 | Anneau des dépenses construit ; légende textuelle comme réponse à « jamais uniquement par la couleur » | ACCEPTED |
| ADR-123 | Évolution de l'épargne construite (barres) ; historique indépendant de l'existence d'un budget | ACCEPTED |
| ADR-124 | Infobulles au survol sur les quatre graphiques ; détection géométrique statique et publique pour rester testable | ACCEPTED |
| ADR-125 | Reste à vivre sur le Tableau de bord ; placeholder « — » sans budget plutôt qu'un calcul erroné | ACCEPTED |
| ADR-126 | Prochaines opérations sur le Tableau de bord ; badge Attendue/Estimée | ACCEPTED |
| ADR-127 | Synthèse budgétaire sur le Tableau de bord ; barres de progression compactes | ACCEPTED |
| ADR-128 | Alertes sur le Tableau de bord ; solde faible + dépassement de budget, jamais persistées | ACCEPTED |
| ADR-129 | Écran Catégories ajouté après audit du périmètre V1 ; validation « deux niveaux » côté service | ACCEPTED |
| ADR-130 | Parcours de premier lancement ; réutilise le Shell existant plutôt qu'un chemin de démarrage séparé | ACCEPTED |
| ADR-131 | Filtres de transactions (compte, catégorie, période, montant, texte) ; filtrage live, borne non analysable ignorée plutôt que bloquante | ACCEPTED |
| ADR-132 | Détection assistée des virements internes ; proposition recalculée à la volée, rien de persisté avant confirmation/rejet | ACCEPTED |
| ADR-133 | Création d'occurrences ponctuelles depuis l'écran Prévisions ; formulaire repliable, portée volontairement limitée à dépense/revenu | ACCEPTED |
| ADR-134 | Historique de soldes officiels ; seule l'observation la plus récente déplace le point de départ des prévisions | ACCEPTED |
| ADR-135 | Thème sombre ; classe CSS basculée sur .shell-root pour l'USS, propriété DarkTheme par instance pour les graphiques Painter2D | ACCEPTED |
| ADR-136 | Premier installeur Windows (IL2CPP + Inno Setup) ; installation par utilisateur, sans droits admin | ACCEPTED |
| ADR-137 | Correction du compte d'une opération récurrente après création ; occurrences en attente régénérées | ACCEPTED |
| ADR-138 | Fenêtre standard (pas plein écran exclusif) + bouton Quitter dans la barre latérale | ACCEPTED |
| ADR-139 | Vérification et téléchargement automatiques de mise à jour ; installation toujours soumise à confirmation | ACCEPTED |
| ADR-140 | Édition étendue des opérations récurrentes (type, fréquence, jour du mois, catégorie, tiers) ; correction fusionnée type+comptes, un seul vidage/régénération par sauvegarde | ACCEPTED |
| ADR-141 | Verrouillage des saisies numériques (lettres bloquées dans les champs montant/entier) ; datepicker reporté à une prochaine mise à jour | ACCEPTED |
| ADR-142 | Correction d'un compte/opération pendant l'onboarding ; séparation visuelle Ajouter/Valider ; bouton "Nouveau X" dupliqué près des listes ; espacement des cartes uniformisé ; scrollbar amincie et thémée ; popup de changelog reportée | ACCEPTED |
| ADR-143 | Comparaison avant/après explicite dans le résultat de la simulation « Et si ? », conforme à `06-Moteur_de_prevision.md` §9 | ACCEPTED |

---

# 3. ADR-101 — Abandonner l'hébergement web

**Contexte** : le projet initial visait une bêta fermée hébergée (VPS OVHcloud, Docker, Symfony, Angular, PostgreSQL), avec une charge d'exploitation et de conformité importante pour un usage personnel.

**Décision** : construire une application desktop Windows locale, sans serveur, distribuée gratuitement en téléchargement.

**Justification** : les applications de gestion de budget grand public sont aujourd'hui majoritairement gratuites ; héberger un service pour quelques utilisateurs ne se justifie plus. Une application locale supprime aussi intégralement les enjeux RGPD et de sécurité réseau.

**Conséquences positives** : plus de coût d'hébergement, plus de surface d'attaque réseau, données garanties locales, conformité RGPD non applicable.

**Conséquences négatives** : pas d'accès multi-appareil, pas de sauvegarde automatique côté serveur, réécriture complète du frontend et du backend.

**Documents concernés** : `00-Vision.md`, `01-Perimetre.md`.

---

# 4. ADR-102 — Unity 6 comme moteur d'application

**Alternatives envisagées** : Electron, Tauri, .NET MAUI, Avalonia.

**Justification** : maîtrise et rapidité de développement déjà acquises avec Unity ; UI Toolkit suffisamment mature pour une interface de gestion ; un seul écosystème pour l'UI et les graphiques personnalisés.

**Conséquences négatives assumées** : poids du runtime plus élevé qu'une application .NET native minimaliste, conventions desktop natives (menus, accessibilité fine) à recréer manuellement.

**Conditions de réévaluation** : si UI Toolkit se révèle réellement limitant pour les tableaux/formulaires denses, ou si le poids du build devient un obstacle réel à la distribution.

**Documents concernés** : `04-Stack_technique.md`.

---

# 5. ADR-103 — UI Toolkit exclusif, graphiques faits maison

**Alternatives envisagées** : XCharts (bibliothèque uGUI gratuite et éprouvée).

**Justification** : un seul paradigme d'UI dans tout le projet (pas de mélange uGUI/UI Toolkit) ; seulement 4 types de graphiques nécessaires, effort borné ; contrôle total sur les exigences déjà connues (distinction réel/prévu par trait plein/pointillé, infobulle, alternative textuelle) ; cohérent avec le principe du projet d'éviter les dépendances tierces non indispensables.

**Conditions de réévaluation** : si le nombre de types de graphiques augmente significativement (V2 avec le moteur de décision), réévaluer XCharts ou une autre bibliothèque UI Toolkit dédiée si elle existe alors.

**Documents concernés** : `07-Interface.md`.

---

# 6. ADR-104 — SQLite embarqué, identifiants entiers

**Contexte** : l'ancien projet utilisait PostgreSQL (serveur) et des UUID (pensés pour la synchronisation multi-sources et les connecteurs).

**Décision** : un unique fichier SQLite local (bibliothèque `sqlite-net-pcl`), avec des identifiants `INTEGER PRIMARY KEY AUTOINCREMENT`.

**Justification** : plus de serveur, plus de connecteur, plus de synchronisation à anticiper — le contexte qui justifiait les UUID a disparu. Un entier auto-incrémenté est plus simple et idiomatique en SQLite pour un usage strictement local.

**Conséquences négatives** : si une synchronisation multi-appareil devenait un jour un besoin réel, une migration d'identifiants serait nécessaire.

**Documents concernés** : `03-Modele_de_donnees.md`, `05-Conventions_de_code.md`.

---

# 7. ADR-105 — Suppression de l'authentification

**Décision** : aucune authentification, aucune session, aucun compte utilisateur en base. Un verrou local optionnel (code PIN) pourra être ajouté en V2, jamais un compte en ligne.

**Justification** : application strictement mono-utilisateur et locale ; la machine elle-même est déjà le périmètre de confiance.

**Documents concernés** : `08-Confidentialite_et_donnees.md`.

---

# 8. ADR-106 — Windows uniquement

**Décision** : la V1 cible exclusivement Windows 10/11 x64.

**Justification** : c'est la seule plateforme réellement utilisée par l'auteur du projet ; élargir à macOS/Linux/mobile multiplierait l'effort de test et de packaging sans besoin immédiat.

**Conditions de réévaluation** : demande explicite d'usage sur une autre plateforme.

**Documents concernés** : `01-Perimetre.md`, `04-Stack_technique.md`.

---

# 9. ADR-107 — Suppression de tous les connecteurs

**Décision** : aucun connecteur externe (Gmail, Open Banking, dossier surveillé) n'est conservé, y compris comme fonctionnalité différée — le principe même de connecteur est incompatible avec l'objectif « aucune donnée ne quitte ni n'entre automatiquement sur le réseau ».

**Documents concernés** : `00-Vision.md`, `01-Perimetre.md`.

---

# 10. ADR-108 — Conservation de l'import CSV

**Décision** : l'import d'un fichier CSV choisi explicitement par l'utilisateur reste disponible, car ce n'est pas un connecteur (aucune récupération automatique, aucun accès réseau).

**Documents concernés** : `01-Perimetre.md`, `08-Confidentialite_et_donnees.md`.

---

# 11. ADR-109 — Report du module Documents et du moteur de décision avancé

**Décision** : les factures/bulletins/PDF/OCR ainsi que les règles financières, objectifs datés et comparaison de scénarios de l'ancien projet ne font pas partie de la V1.

**Justification** : cohérent avec la priorité absolue de l'ancien projet (« l'automatisation intervient uniquement après validation du socle ») — d'autant plus vraie ici où le socle change entièrement de technologie.

**Conditions de réévaluation** : après une utilisation quotidienne réelle de la V1 sur plusieurs semaines, comme le prévoyait déjà `MVP-010`.

**Documents concernés** : `01-Perimetre.md`.

---

# 12. ADR-110 — `DateTime` plutôt que `DateOnly`

**Contexte** : `05-Conventions_de_code.md` prévoyait initialement `System.DateOnly` (.NET 6) pour toute date civile, par cohérence avec les conventions modernes du langage.

**Décision** : utiliser `System.DateTime` avec une composante horaire systématiquement à zéro pour représenter une date civile, partout dans le modèle `Domain`.

**Justification** : en écrivant les classes du modèle `Domain`, la compilation Unity (batchmode, `error CS0246`) a révélé que `DateOnly` n'existe pas dans le runtime scripting d'Unity 6000.3.12f1, quelle que soit la cible (Editor ou Standalone). Plutôt que de chercher un contournement incertain, `DateTime` est utilisé : disponible partout, sans ambiguïté pour une valeur sans heure tant que la convention « toujours minuit » est respectée.

**Conséquences négatives** : perte de la garantie de type qu'apportait `DateOnly` (rien n'empêche au compilateur d'assigner une heure non nulle par erreur) — à compenser par la discipline et, plus tard, par des tests.

**Conditions de réévaluation** : si une version future d'Unity introduit le support de `DateOnly` dans son runtime scripting, réévaluer une migration.

**Documents concernés** : `05-Conventions_de_code.md`.

---

# 13. ADR-111 — Polyfill `IsExternalInit`

**Contexte** : en écrivant `FinanceOS.Forecast` (`ForecastEvent`, `ForecastRequest`, `ForecastResult`...) sous forme de `record` avec propriétés `init`, la compilation Unity a échoué avec `error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported` — même famille de contrainte que l'ADR-110 : le runtime scripting d'Unity 6000.3 ne fournit pas ce type marqueur requis par le compilateur C# pour les fonctionnalités C# 9 `record`/`init`.

**Décision** : déclarer soi-même le type marqueur vide `System.Runtime.CompilerServices.IsExternalInit`, une classe `internal static` sans membre, dans `Assets/Scripts/Forecast/IsExternalInitPolyfill.cs`. C'est un correctif standard, largement documenté, pour utiliser `record`/`init` sur une cible antérieure à .NET 5 — aucune incidence sur le comportement à l'exécution.

**Conséquences négatives** : le polyfill est `internal`, donc propre à chaque assembly qui l'utilise. Toute nouvelle assembly (`FinanceOS.App`, `FinanceOS.UI`...) qui voudrait des `record`/`init` devra ajouter sa propre copie du fichier.

**Conditions de réévaluation** : si une version future d'Unity fournit nativement ce type, supprimer le polyfill sans changer le code appelant.

**Documents concernés** : `06-Moteur_de_prevision.md`.

---

# 14. ADR-112 — Vérification de l'UI par liaison de données, graphique différé

**Contexte** : contrairement à `Domain`/`Data`/`Forecast`/`App`, l'interface (`FinanceOS.UI`) ne peut pas être entièrement vérifiée par une exécution batch : `Unity.exe -batchmode -nographics` ne dispose d'aucun périphérique de rendu (« Forcing GfxDevice: Null » dans les journaux), donc tout ce qui dépend d'un vrai passage de rendu — en particulier `generateVisualContent`/`Painter2D` pour les graphiques — ne peut ni s'exécuter ni être contrôlé de cette façon. Aucun outil de capture d'écran d'application Unity locale n'est disponible dans cet environnement de développement.

**Décision** : séparer systématiquement la construction des données d'affichage (`XViewModelBuilder`, pur C#) de la liaison aux `VisualElement` (`XController`). Le contrôleur reste vérifiable sans rendu : `VisualTreeAsset.Instantiate()` construit l'arbre en mémoire, et interroger le texte des `Label` après un appel à `Render(...)` prouve que la liaison est correcte, même sans jamais dessiner un pixel. Le graphique de trésorerie (rendu vectoriel `Painter2D`) est différé tant qu'aucun moyen de vérifier visuellement son rendu n'existe dans cet environnement — construire du code de dessin sans jamais pouvoir constater qu'il produit le bon résultat serait imprudent.

**Ce que cette stratégie a concrètement détecté** : en câblant `theme.uss` dans la scène réelle (pas seulement en le faisant analyser par le compilateur), le journal Unity a signalé `Unknown pseudo class "last-child" in StyleSheet theme` — USS (UI Toolkit) ne supporte pas les pseudo-classes structurelles du CSS web (`:last-child`, `:first-child`...). Corrigé en retirant la règle plutôt qu'en la laissant échouer silencieusement.

**Conséquences négatives** : la mise en page, les couleurs et la typographie réelles ne sont vérifiées par personne tant qu'un humain n'a pas ouvert le projet dans l'éditeur Unity et regardé l'écran. Cette étape reste nécessaire et n'est pas remplacée par les vérifications automatisées décrites ici.

**Conditions de réévaluation** : si un outil de capture d'écran pour une application Unity locale devient disponible dans cet environnement, ou lorsqu'une session avec accès visuel prend le relais.

**Mise à jour (2026-09-14)** : condition de réévaluation atteinte, sous une forme manuelle plutôt qu'outillée — l'utilisateur capture désormais lui-même l'éditeur en Play Mode et partage l'image en retour, ce qui joue le rôle du « relais visuel » envisagé ci-dessus. Le graphique de trésorerie (`LineChartElement`, `Painter2D`) a été construit sur cette base : écrit à partir de la meilleure connaissance de l'API, vérifié en batchmode pour tout ce qui est vérifiable sans rendu (compilation, mapping des données `ChartSeries`, absence d'exception à la construction), puis confirmé — ou corrigé — par une capture d'écran de l'utilisateur, exactement comme pour les corrections de mise en page de l'ADR-116. Le reste (barres du budget, anneau des dépenses) suivra le même patron. Voir aussi ADR-119.

**Documents concernés** : `07-Interface.md`.

---

# 15. ADR-113 — Navigation par échange de contenu dans un UIDocument unique

**Contexte** : à partir de deux écrans (Tableau de bord, Comptes), il fallait un mécanisme de navigation. Deux options : plusieurs scènes Unity chargées/déchargées, ou une seule scène avec un `UIDocument` dont le contenu est remplacé dynamiquement.

**Décision** : une seule scène (`Main.unity`), un seul `UIDocument` chargeant `Shell.uxml` (barre latérale + zone de contenu vide). `AppBootstrap` instancie le `VisualTreeAsset` de l'écran actif (`Dashboard.uxml`/`Accounts.uxml`) via `.Instantiate()` et le place dans la zone de contenu ; chaque écran garde son propre `XController`. Plusieurs scènes auraient dupliqué l'`AppContainer` (base de données, services) et sa durée de vie sans bénéfice — l'application n'a ni URL ni historique de navigateur à représenter (docs/07-Interface.md §14).

**Ce que cette stratégie a concrètement détecté** : une tentative de vérifier le clic sur les boutons de navigation par simulation d'événement (`VisualElement.SendEvent(ClickEvent...)`) sur un arbre construit par `VisualTreeAsset.Instantiate()` mais jamais attaché à un panel n'a déclenché aucun callback (`dashboard=0, accounts=0` dans le journal batchmode) — confirmé empiriquement, pas supposé. `SendEvent` dépend d'un `elementPanel` non nul pour dispatcher, qui n'existe que si l'élément est réellement rattaché à un `UIDocument` en cours d'exécution. Conséquence pratique : comme pour ADR-112, la vérification automatisée de ce projet couvre la structure et la liaison de données (état actif du bouton via `SetActive(...)`, contenu inséré via `SetContent(...)`), pas le déclenchement réel d'un clic — cela reste du ressort d'une vérification visuelle humaine.

**Conséquences négatives** : `ShellController` et les gestionnaires de clic des contrôleurs d'écran (créer/renommer/archiver un compte) ne sont jamais exercés par les tests automatisés — seule la reconstruction et l'affichage du view model le sont.

**Conditions de réévaluation** : mêmes conditions qu'ADR-112.

**Documents concernés** : `07-Interface.md`.

---

# 16. ADR-114 — Éviter les membres .NET Standard 2.1 dans `FinanceOS.UI`

**Contexte** : en écrivant `TransactionsViewModelBuilder`, `Dictionary<int, string>.GetValueOrDefault(key, "—")` (introduit en .NET Standard 2.1) a échoué à la compilation dans `FinanceOS.UI` avec `error CS1061` — alors que le même appel, sur un `Dictionary<int, long>`, compile sans problème dans `BudgetService.cs` (`FinanceOS.App`) au sein du même projet, de la même passe de compilation batchmode. Confirmé empiriquement en isolant l'appel : ce n'est pas un hasard de frappe, `FinanceOS.UI` résout un ensemble de références système plus restreint que `FinanceOS.App`, très probablement parce qu'il référence des modules `UnityEngine.UIElements` dont la résolution d'assembly reste pinée sur un profil .NET Standard 2.0.

**Décision** : dans `FinanceOS.UI` spécifiquement — et par prudence, project-wide plutôt que de mémoriser une liste d'exceptions par assembly — ne pas utiliser de membres BCL apparus en .NET Standard 2.1 ou plus récent (`Dictionary.GetValueOrDefault`, `string.Contains(char)`, etc.). Utiliser systématiquement le motif portable `dict.TryGetValue(key, out var value) ? value : fallback` à la place. Fixé dans `TransactionsViewModelBuilder.cs` en remplaçant l'appel par `TryGetValue`.

**Ce que cette découverte confirme, une troisième fois (cf. ADR-110, ADR-111)** : ne jamais supposer qu'une API .NET récente est disponible dans ce projet sans l'avoir vue compiler ici — et cette fois, la surprise n'est même pas seulement "cette API n'existe pas", mais "elle existe dans un assembly du projet et pas dans un autre", ce qui est plus facile à manquer qu'un simple `error CS0246` global.

**Conséquences négatives** : aucune fonctionnalité perdue — `TryGetValue` fait exactement la même chose, juste plus verbeux.

**Conditions de réévaluation** : si un changement de version Unity ou de configuration de projet fait disparaître cette asymétrie (à vérifier empiriquement avant de relâcher la règle, pas supposer).

**Documents concernés** : `05-Conventions_de_code.md` §12.

---

# 17. ADR-115 — Définition retenue pour « reste à vivre » et « taux d'épargne du mois »

**Contexte** : `01-Perimetre.md` §2.9 exige « reste à vivre et taux d'épargne du mois » sur l'écran Budget, et `07-Interface.md` liste « reste à vivre » sur le Tableau de bord — mais aucun document, jusqu'à l'écran Budget, ne définissait le calcul : seule la maquette (`docs/mockups/dashboard.html`) affichait un nombre sans formule.

**Décision** : `BudgetService.GetOverview` (nouveau) calcule les deux à partir de ce que `GetSummary` et les périodes de transactions/occurrences fournissent déjà, sans nouvelle donnée stockée ni nouveau concept :
- **Reste à vivre** = somme du restant (prévu − réel − engagé) de chaque catégorie **de type Dépense** allouée au budget du mois. Les catégories de type Épargne/Revenu/Virement éventuellement allouées n'y contribuent pas — « reste à vivre » désigne ce qu'il reste à dépenser dans ses enveloppes de dépenses, pas un solde global.
- **Taux d'épargne** = (mouvements réel + engagé vers une catégorie de type Épargne, magnitude) ÷ (mouvements réel + engagé vers une catégorie de type Revenu, magnitude) × 100, sur le mois du budget. 0 % si aucun revenu catégorisé n'existe ce mois (division évitée, pas une erreur).

**Ce que ça implique concrètement** : le taux d'épargne dépend entièrement de la catégorisation — une transaction de revenu ou d'épargne non catégorisée n'y contribue pas. C'est cohérent avec le reste de l'application (`GetSummary` a la même dépendance à la catégorisation) mais mérite d'être su : un utilisateur qui ne catégorise pas ses revenus verra un taux d'épargne à 0 % même s'il épargne réellement.

**Conséquences négatives** : si l'ancien projet ou une future relecture des mockups révèle une définition différente (ex. reste à vivre incluant le solde disponible réel, pas seulement les enveloppes budgétées), cette décision devra être révisée — rien dans la documentation actuelle ne permet de trancher avec certitude, ce choix est le plus simple et explicable compte tenu des primitives déjà construites, pas une certitude tirée d'une spec.

**Conditions de réévaluation** : si l'utilisateur precise une définition différente, ou en écrivant le Tableau de bord complet (qui doit aussi afficher « reste à vivre » — réutiliser cette même définition par cohérence, sauf décision contraire explicite).

**Documents concernés** : `01-Perimetre.md` §2.9, `07-Interface.md`.

---

# 18. ADR-116 — Corrections de mise en page trouvées uniquement via captures d'écran réelles

**Contexte** : ADR-112 documente déjà que ce projet ne peut pas vérifier visuellement l'UI par lui-même — seules la structure et la liaison de données sont vérifiables en batchmode. Une fois l'utilisateur passé en revue chaque écran par capture d'écran (d'abord à vide, puis avec de vraies données), plusieurs défauts de mise en page réels sont apparus qu'aucun test structurel n'aurait jamais pu détecter, puisqu'un champ vide et un champ contenant le mauvais texte, ou une colonne trop étroite et une colonne bien dimensionnée, sont indiscernables pour une assertion `.text == "..."` tant que personne n'a pensé à vérifier précisément la valeur ou la largeur en cause.

**Défauts trouvés et corrigés, par ordre chronologique de découverte** :
1. **Sélecteur de compte vide sur Prévisions** (écran à vide) — corrigé en amont de cet ADR, voir commit `c4ac3be`.
2. **Colonnes de `MultiColumnListView` sans largeur explicite** — aucun des 5 tableaux de l'application (Transactions, Opérations récurrentes, Prévisions ×2, Budget) ne donnait de `width`/`minWidth` à ses colonnes ; Unity les réduisait à quelques pixels chacune, tronquant en-têtes et valeurs. Resté invisible tant que les captures ne montraient que des tableaux vides. Corrigé : largeur + largeur minimale explicites sur chaque colonne, une colonne `stretchable` par tableau pour absorber l'espace restant, hauteur de ligne fixe (`fixedItemHeight = 28`).
3. **Chevauchement de texte généralisé** (chiffres de KPI débordant de leur carte, libellés de formulaire trop proches du champ précédent, légende trop proche du titre de carte) — la cause exacte (métriques réelles de la police par défaut d'Unity, jamais vérifiées visuellement avant) n'a pas pu être confirmée avec certitude sans inspecteur en direct ; corrigé par des marges nettement plus généreuses (`.form-row` 10px → 30px en deux passes, `.kpi-row` 16px → 24px) et, pour le débordement horizontal des valeurs de KPI, par `flex-wrap` + une largeur minimale par carte (`.kpi-card { min-width: 190px }`) plutôt qu'une largeur purement proportionnelle qui s'écrasait à cinq cartes par ligne.
4. **Réemploi incorrect de `.page-subtitle` comme légende de carte** — cette classe est conçue pour un espacement serré directement sous le grand titre de page ; réutilisée telle quelle sous un titre de carte ou un label de formulaire, elle donnait un espacement bien trop faible. Nouvelle classe dédiée `.card-caption`, avec un espacement propre à ce contexte.
5. **Titre de carte caché sous son propre tableau** (« Occurrences prévues » rendu sous la `MultiColumnListView` qui le suit dans le document) — première hypothèse (géométrie de liste obsolète après un cycle `display: none` → `Flex`, corrigée via `Rebuild()`) **infirmée par la capture d'écran suivante** : le titre restait caché après ce correctif. Cause retenue ensuite, cohérente avec les points 3 et 4 ci-dessus : `.card-header` n'avait qu'un `margin-bottom: 10px` sans hauteur minimale propre, insuffisant une fois les métriques réelles de la police en jeu — exactement le même schéma que `.kpi-card`/`.form-row`/`.page-subtitle` mal réemployée. Corrigé par `.card-header { min-height: 24px; margin-bottom: 20px; }` (10px → 20px), **confirmé par la capture suivante**. Le correctif `Rebuild()` reste en place (inoffensif) mais n'était pas la vraie cause.
6. **Même schéma, trois autres endroits, trouvés par l'utilisateur dans la capture qui confirmait le point 5** : le sous-titre de page (« Solde projeté... ») chevauchait la ligne de filtre juste en dessous (`.page-header` avait le même défaut que `.card-header` — corrigé, `min-height: 60px`, `margin-bottom` 16px → 24px, `.page-subtitle` 2px → 6px) ; le contexte de date d'une carte KPI à trois lignes (« Point bas prévisionnel ») passait sous sa propre valeur au lieu d'être dessous (`.kpi-context` 4px → 10px, `.kpi-value` gagne un `margin-bottom: 4px`) ; et les tableaux, resserrés au point précédent (`.transactions-table` réduit à `min-height: 120px` dans ce même ADR) ne montraient plus qu'une ligne ou deux, lus par l'utilisateur comme « coupés » — la réduction depuis 240px était en fait une sur-correction, le vrai problème initial (le point 2) était l'absence de fond/bordure et de largeurs de colonnes, pas la hauteur ; remonté à `min-height: 220px, max-height: 400px`.

**Conséquences négatives** : les points 3, 5 et 6 sont corrigés à partir d'indices et d'hypothèses raisonnables, pas d'un diagnostic certain — un inspecteur d'UI en direct aurait permis de confirmer la cause exacte en une itération au lieu de plusieurs allers-retours de capture d'écran. Le point 6 illustre aussi qu'une correction (réduire `min-height` au point 2) peut elle-même introduire une régression visible seulement à l'itération suivante.

**Conditions de réévaluation** : mêmes conditions qu'ADR-112. Le schéma « boîte trop courte pour son propre contenu textuel » s'étant maintenant répété sur six éléments distincts (`.kpi-card`, `.form-row`, `.card-header`, `.page-header`, `.kpi-context`/`.kpi-value`, et indirectement le réemploi de `.page-subtitle`), il est probable que d'autres éléments non encore repérés dans ce fichier partagent le même défaut — traiter tout nouveau signalement de chevauchement de texte comme une instance de plus de ce même schéma, pas un cas isolé.

**Documents concernés** : aucun autre — plusieurs petits correctifs de code/USS, déjà répercutés dans `Assets/UI/USS/theme.uss` et les contrôleurs concernés.

---

# 19. ADR-117 — Défilement de page et repli de la section Simulation

**Contexte** : après plusieurs itérations de l'ADR-116 à ajuster des marges sur l'écran Prévisions, l'utilisateur a reformulé le vrai problème : ce n'était pas (seulement) des marges mal réglées, c'était trop d'information pour un seul écran — six cartes empilées (synthèse, vérification, occurrences, journal, simulation) sans défilement de page (seuls les tableaux internes défilaient), ce qui rendait chaque petit défaut de marge disproportionnellement visible.

**Décision** :
1. **Défilement de page sur les sept écrans**, pas seulement Prévisions — la zone de contenu de chaque écran (`X-root`) passe de `ui:VisualElement` à `ui:ScrollView` (`mode="Vertical"`), même classe `page` conservée. Défaut structurel partagé par toute l'application (aucun écran n'avait de défilement de page), pas seulement un correctif pour Prévisions — un écran pourrait dépasser la hauteur de fenêtre sur n'importe quel autre écran au fur et à mesure que son contenu grandit.
2. **Section Simulation repliée par défaut** sur Prévisions — remplacée par un bouton « Simuler un scénario » dans l'en-tête de carte, qui affiche le formulaire au clic (`ForecastsController.ToggleSimulationBody`). La doc regroupe explicitement la simulation dans l'écran Prévisions (`01-Perimetre.md`/`07-Interface.md` §3) plutôt que comme écran séparé — un nouvel écran de navigation aurait cassé ce regroupement documenté ; le repli garde tout sur un seul écran sans l'imposer par défaut.

**Découverte empirique en cours de route** : le premier test écrit pour vérifier que la section est repliée par défaut a échoué — `simulation-body` avait `style="display: none;"` en UXML mais son `.style.display` ne valait pas `DisplayStyle.None` une fois l'arbre instancié hors d'un panel réel. Tous les autres éléments « cachés par défaut » de l'application ont toujours leur affichage fixé explicitement par du code C# (typiquement dans `Refresh()`), jamais laissé au seul style UXML — ce cas est le premier à s'appuyer uniquement sur l'attribut UXML, et c'est celui qui a révélé que ça ne suffit pas de façon fiable sans panel réel. Corrigé en fixant `_simulationBody.style.display = DisplayStyle.None;` explicitement dans le constructeur du contrôleur, comme partout ailleurs. Même famille de limitation que ADR-112/ADR-113 (rien de fiable sans panel réel attaché) — retenir la règle générale : ne jamais compter sur un `style="display: none;"` UXML seul pour l'état initial d'un élément dont le contrôleur gère ensuite la visibilité ; le fixer aussi explicitement en C#.

**Conséquences négatives** : le Journal des mouvements prévus reste une carte lourde (tableau + légende) non repliée — si la densité reste un problème une fois un compte réel utilisé au quotidien, il recevra probablement le même traitement.

**Conditions de réévaluation** : si un écran continue de sembler surchargé malgré le défilement de page, envisager de replier d'autres sections secondaires plutôt que d'ajouter encore des ajustements de marge.

**Documents concernés** : `07-Interface.md` §2/§3.

---

# 20. ADR-118 — Polices statiques vendorisées plutôt que variables

**Contexte** : en allant chercher les polices de la maquette (Spectral, IBM Plex Sans, IBM Plex Mono) pour la passe typographie, le mirror `google/fonts` (source évidente pour Spectral) a échoué pour `ibmplexsans`/`ibmplexmono` — vérification faite via l'API GitHub plutôt que supposé, ces deux familles n'existent plus dans ce dépôt qu'en police variable (un seul fichier `[wdth,wght].ttf` couvrant tous les poids par un axe de variation), les fichiers statiques historiques (`IBMPlexSans-Regular.ttf`, `-Bold.ttf`, etc.) ont disparu du dépôt.

**Décision** : ne pas utiliser les polices variables. UI Toolkit référence une police via `-unity-font-definition: url(...)` pointant un `Font` Unity — importer un fichier variable ne rend que son instance par défaut (généralement le poids 400), sans moyen scriptable dans cet environnement de sélectionner un autre poids sur l'axe `wght` (ça demanderait de configurer un Font Asset TextCore par l'inspecteur de l'éditeur, ou une API de script non vérifiée ici). Utilisé à la place le dépôt source canonique d'IBM (`github.com/IBM/plex`, package `plex-sans`/`plex-mono`, dossier `fonts/complete/ttf`), qui distribue encore des fichiers statiques par poids (`IBMPlexSans-Regular.ttf`, `IBMPlexSans-Bold.ttf`, etc.) — la même licence SIL OFL 1.1, juste une distribution différente de celle de Google Fonts.

**Ce que cette vérification a évité** : importer la police variable sans le remarquer aurait probablement semblé fonctionner (aucune erreur de compilation, juste un rendu visuellement plat au poids par défaut) — un défaut impossible à détecter par les tests batchmode existants (ADR-112) et qui n'aurait été visible qu'à la prochaine capture d'écran, potentiellement confondu avec un simple oubli de `-unity-font-style: bold` plutôt qu'un vrai problème de police.

**Conséquences négatives** : dépendance à deux dépôts sources différents (`google/fonts` pour Spectral, `IBM/plex` pour les deux familles Plex) plutôt qu'un seul mirror — légèrement plus de friction pour une future mise à jour de version.

**Conditions de réévaluation** : si Unity ajoute un moyen scriptable et vérifiable dans cet environnement de sélectionner un poids d'une police variable importée, ou si `google/fonts` republie des fichiers statiques pour ces familles.

**Documents concernés** : `07-Interface.md` §8bis, `Assets/Fonts/THIRD-PARTY-NOTICES.md`.

---

# 21. ADR-119 — Graphique de trésorerie construit sur la base d'une vérification par capture d'écran

**Contexte** : ADR-112 différait le graphique de trésorerie faute de moyen de vérifier visuellement un rendu `Painter2D` en batchmode. Depuis, l'utilisateur capture régulièrement l'éditeur en Play Mode et partage l'image — un relais visuel manuel qui satisfait la condition de réévaluation d'ADR-112 (voir sa mise à jour du 2026-09-14).

**Décision** : `LineChartElement` (`Assets/Scripts/UI/LineChartElement.cs`), une `VisualElement` qui redéfinit `generateVisualContent` et dessine via `context.painter2D`, comme esquissé dans `07-Interface.md` §8.3. Intégré dans la carte « Courbe de trésorerie » de l'écran Prévisions, juste au-dessus du « Journal des mouvements prévus » qui en reste l'alternative textuelle (§8.4). Alimenté par un nouveau champ du view model, `ChartSeries` (`ForecastsViewModelBuilder`) — la série quotidienne **complète** de l'horizon (contrairement à `Timeline`, filtré aux seuls jours avec mouvement pour le journal textuel), avec le solde brut et un indicateur réel/prévu par jour.

**Choix techniques notables** :
- **Aucun `-unity-font-*`/`Painter2D.lineDash` natif** : contrairement à Canvas HTML, `Painter2D` n'a pas de propriété de pointillé. Le segment prévu est dessiné à la main (`DrawDashedLine`) — un seul `BeginPath`/`Stroke` par segment, avec des `MoveTo`/`LineTo` alternés en tirets/espaces le long du vecteur.
- **Marqueur du point bas en losange**, pas en cercle — évite `Painter2D.Arc`/`Angle`, dont la signature exacte était moins certaine de mémoire ; un losange (`MoveTo`/`LineTo` ×4/`ClosePath`/`Fill`) atteint le même objectif avec uniquement les membres de l'API déjà utilisés ailleurs dans ce fichier, donc à plus haute confiance.
- **Couleurs codées en dur** dans la classe C# (pas de lecture des variables USS `--color-*` depuis le code) — reprennent exactement la palette de `theme.uss` (accent navy pour le réel, gris ink-400 pour le prévu, or pour le point bas), au prix d'une duplication à maintenir si la palette change.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : compilation, mapping `ChartSeries` (nombre de points, indicateur réel/prévu par date, cf. `UISmokeTest.cs`), absence d'exception à la construction du contrôleur — tout cela vérifié et vert du premier coup. Le rendu effectif (positions, couleurs, lisibilité du pointillé) ne l'est pas et ne peut pas l'être ici ; seule la capture d'écran suivante de l'utilisateur le confirmera.

**Confirmé exactement par cette première capture — zone de dessin entièrement vide.** Cause réelle, sans rapport avec les tracés/couleurs eux-mêmes : `LineChartElement` était ajouté dans son conteneur (`.chart`, hauteur 200px fixe) sans aucune taille propre — un `VisualElement` neuf a une hauteur de contenu nulle par défaut, donc `contentRect` restait vide et la garde `if (drawableWidth <= 0 || drawableHeight <= 0) return;` empêchait tout tracé avant même d'atteindre le code de dessin. Corrigé par `_cashFlowChart.style.flexGrow = 1;` dans `ForecastsController`, avec un test qui vérifie précisément cette valeur pour empêcher la régression. **Symptomatique du type d'erreur que seule une capture révèle** : rien dans la compilation, le mapping de données ou l'absence d'exception ne pouvait distinguer « la zone de dessin est vide » de « le tracé est correct mais hors champ » ou « les couleurs sont invisibles » — les trois se ressemblent identiquement à un test structurel.

**Conséquences négatives** : les trois autres graphiques du catalogue (§8.2 — barres budget prévu/réel/engagé, anneau des dépenses, évolution de l'épargne) restent à construire, sur le même patron — et devront explicitement inclure `flexGrow`/une taille propre sur leur `VisualElement` dès l'écriture initiale, pas comme correctif après coup. Pas d'infobulle au survol (§8.4) sur ce premier graphique — demanderait une détection de position de pointeur sur un rendu vectoriel, jugé disproportionné avant même d'avoir confirmé que le tracé de base est correct.

**Conditions de réévaluation** : après la prochaine capture d'écran, pour confirmer que le tracé apparaît maintenant — s'il reste absent ou incorrect, corriger sur preuve, pas en devinant une seconde fois à l'aveugle.

**Confirmé le 2026-09-13** : capture d'écran de l'écran Prévisions avec un compte réel (5 occurrences Loyer/Salaire alternées) — courbe en escalier bien lisible, les 5 mouvements distincts confirmés un par un par l'utilisateur (pas de confusion visuelle entre deux mouvements consécutifs à cette échelle), marqueur en losange positionné exactement au point bas (cohérent avec la valeur affichée dans la carte Synthèse), trait entièrement en pointillé — attendu ici, aucun mouvement de la fenêtre affichée n'est encore passé en compte réel (`RecordOfficialBalance` pas encore relié à une action d'écran). Le patron `LineChartElement` (taille explicite via `flexGrow`, pointillé/marqueur dessinés à la main) est donc validé pour les trois graphiques restants (§8.2).

**Répliqué sur le Tableau de bord (2026-09-13)** : même `LineChartElement`, même patron de câblage (`flexGrow = 1` dans le constructeur du contrôleur, `Points` réassigné à chaque `Render`) — `DashboardViewModel.ChartSeries` calculé par `DashboardViewModelBuilder` depuis `forecast.Timeline` de la même façon que `ForecastsViewModelBuilder.ChartSeries`, seul l'horizon diffère (fin du mois courant, déjà celui utilisé pour les autres KPI du tableau de bord — pas l'horizon de prévision réglable des Paramètres). Aucune surprise technique : la seule difficulté du premier graphique (taille du conteneur) était déjà documentée, donc reproduite correctement du premier coup.

**Documents concernés** : `07-Interface.md` §5/§8.

---

# 22. ADR-120 — Opérations récurrentes : avertissement de premier cycle sauté + suppression conditionnelle

**Contexte** : incident réel du 2026-09-13 (voir mémoire de projet) — une opération « Salaire » créée avec une date de début (29/09) un jour après le jour du mois choisi (28) a produit silencieusement zéro occurrence en septembre, la première tombant en octobre, sans aucun avertissement. Cause exacte : `ForecastOccurrenceGenerator.EnumerateScheduledDates` calcule la date candidate du mois de départ à partir du jour du mois (28), puis la rejette car antérieure à la date de début (29) — comportement déterministe et défendable pour les entrées données, mais totalement invisible pour l'utilisateur. Diagnostiqué en interrogeant directement le fichier SQLite réel (`data/financeos.db`) en lecture seule, après qu'une première hypothèse (horizon du Tableau de bord) n'ait expliqué qu'une partie du symptôme. Corrigé dans l'immédiat par une modification directe de la ligne concernée (avec sauvegarde préalable du fichier) faute de tout autre moyen — `RecurringOperationsController` ne permet ni de modifier la date de début/le jour du mois après création, ni de supprimer l'opération.

**Décision** : deux garde-fous, sans étendre l'édition post-création (qui reste volontairement limitée au montant attendu et à la suspension/reprise, cf. `Assets/Scripts/UI/README.md`) :

1. **Avertissement non bloquant à la création.** `ForecastOccurrenceGenerator.EnumerateScheduledDates` a été refactorisée : la logique de calcul pure (fréquence, intervalle, date de début, date de fin, jour du mois) est désormais une surcharge publique indépendante de tout `RecurringOperation` construit, la surcharge existante devenant un simple relais. `RecurringOperationService.PreviewFirstOccurrenceDate(...)` s'appuie dessus pour prévisualiser la première date réellement produite, sans rien persister. `RecurringOperationsController` recalcule cet aperçu à chaque changement de date de début/jour du mois/fréquence en mode création, et affiche `form-skip-warning` (nouvelle classe `.form-warning`, couleur `--color-gold` — distincte du rouge `--color-danger` réservé aux erreurs bloquantes) quand la première date prévue ne tombe pas dans le mois de la date de début. L'utilisateur peut toujours valider malgré l'avertissement : la combinaison reste une entrée valide, seulement potentiellement pas celle voulue.

2. **Suppression conditionnelle.** `RecurringOperationService.Delete(operationId)` (nouveau) refuse si `ForecastOccurrenceRepository.HasMatchedOccurrence` (nouveau) trouve au moins une occurrence au statut `matched` pour cette opération — supprimer effacerait la trace d'un mouvement déjà confirmé comme réel. Sinon, `RecurringOperationRepository.Delete` supprime la ligne ; `forecast_occurrence.recurring_operation_id` porte déjà `ON DELETE CASCADE` (`SchemaMigrations.cs`) et `PRAGMA foreign_keys = ON` est actif (`AppDatabase.cs`), donc les occurrences non rapprochées disparaissent automatiquement, sans SQL supplémentaire côté application. Bouton « Supprimer » (`danger-button`) ajouté au formulaire d'édition, visible uniquement en mode édition — même patron non confirmé que `TransactionsController.DeleteTransaction` (pas de dialogue de confirmation, l'app n'en a pas ailleurs).

**Corrigé en passant** : les cartes de formulaire de `Accounts.uxml`, `Transactions.uxml`, `RecurringOperations.uxml`, `Forecasts.uxml` (deux formulaires) et `Budgets.uxml` plaçaient toutes leur `Label` d'erreur *avant* le bloc `.form-actions` — exactement le défaut qui faisait sauter le bouton Enregistrer des Paramètres (cf. ADR-117 et son historique de correctifs de marge). Repérée en corrigeant celle des Paramètres, vérifiée sur les six autres cartes par une recherche systématique plutôt que corrigée au cas par cas. Déplacées après `.form-actions`, même position que `settings-saved-label`/`backup-result-label` qui n'ont jamais eu ce problème.

**Conséquences négatives** : l'avertissement de premier cycle sauté ne couvre que les fréquences mensuelles et assimilées (bimestrielle, trimestrielle, semestrielle, annuelle) — une fréquence hebdomadaire ne peut structurellement pas produire ce décalage (sa première occurrence est toujours la date de début elle-même), donc rien à avertir là. La suppression reste un correctif d'urgence, pas une vraie édition : si l'utilisateur veut *changer* une date de début plutôt que corriger une erreur de saisie évidente, il doit supprimer et recréer. Pas de confirmation avant suppression (« Supprimer » agit immédiatement) — cohérent avec le reste de l'app, mais à revoir si un utilisateur supprime accidentellement une opération encore utile.

**Documents concernés** : `07-Interface.md` §3, `Assets/Scripts/UI/README.md`.

---

# 23. ADR-121 — Graphique barres budget construit sur le patron `LineChartElement`

**Contexte** : deuxième des quatre graphiques du catalogue (`07-Interface.md` §8.2), une fois la courbe de trésorerie confirmée visuellement et répliquée sur le Tableau de bord (ADR-119). Barres groupées prévu/réel/engagé par catégorie, sur l'écran Budget.

**Décision** : `BudgetBarChartElement` (`Assets/Scripts/UI/BudgetBarChartElement.cs`), même famille que `LineChartElement` — `Painter2D`, aucune bibliothèque tierce (ADR-103). Alimenté par `BudgetsViewModel.ChartGroups` (nouveau champ, `BudgetChartBarGroupViewModel` : nom de catégorie + trois montants bruts), construit par `BudgetsViewModelBuilder` depuis `BudgetService.GetSummary` — déjà des magnitudes positives directement comparables (contrairement au solde signé de la courbe de trésorerie, ce graphique n'a aucun signe à gérer). Carte « Prévu / réel / engagé » insérée entre la synthèse et le tableau des catégories dans `Budgets.uxml`, masquée/affichée comme le reste tant qu'aucun budget n'existe pour le mois affiché (même bascule que `overview-card`/`allocations-card` dans `BudgetsController.Refresh`).

**Différence structurelle avec la courbe** : `Painter2D` ne sait pas dessiner de texte. La courbe s'en passait (les KPI environnants suffisaient à situer les valeurs), mais un graphique à barres sans le nom de chaque catégorie n'est pas lisible. Solution : de vrais `Label` UI Toolkit, enfants du même `VisualElement` que celui qui dessine les barres, repositionnés sous chaque groupe à chaque `GeometryChangedEvent` — le canevas Painter2D reste seul responsable des formes, les enfants UI Toolkit seuls responsables du texte. Pattern standard (canvas pour les formes, DOM/éléments UI pour le texte), toujours sans dépendance externe.

**Distinction « pas uniquement par la couleur » (§8.4) sans équivalent du trait plein/pointillé** : la courbe distingue réel/prévu par la forme du trait (dessiné à la main faute d'API native, cf. ADR-119). Un graphique à barres n'a pas d'équivalent simple. Choix retenu : un **ordre gauche-à-droite fixe** par groupe (prévu, réel, engagé), documenté dans la légende textuelle de la carte (`.card-caption`) plutôt que répété en légende graphique dans le canevas lui-même (aurait demandé, encore, du texte que `Painter2D` ne sait pas dessiner). Couleurs reprises telles quelles de `LineChartElement` (accent navy = prévu, or = réel, gris ink-400 = engagé) — même duplication de palette assumée qu'ADR-119.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : compilation, mapping `ChartGroups` (montants bruts corrects, cf. `UISmokeTest.cs`), présence de l'élément et `flexGrow == 1f` (même garde-fou que la courbe, ADR-119), absence d'exception sans budget/sans allocation — tout vert du premier coup. Le rendu effectif (proportions des barres, lisibilité des labels de catégorie repositionnés) ne l'est pas ; à confirmer par capture d'écran.

**Conséquences négatives** : pas d'infobulle au survol (§8.4), même report qu'ADR-119. Avec beaucoup de catégories allouées, les groupes de barres et leurs labels vont se resserrer — pas de défilement horizontal ni de regroupement/agrégation prévu dans cette première version, à revoir si un usage réel avec de nombreuses catégories le rend illisible. Le graphique de répartition des dépenses (anneau, Tableau de bord) et celui de l'évolution de l'épargne (Budget) restent à construire.

**Documents concernés** : `07-Interface.md` §5/§8.

---

# 24. ADR-122 — Anneau des dépenses construit sur le patron `Painter2D` établi

**Contexte** : troisième et avant-dernier graphique du catalogue (`07-Interface.md` §8.2), sur le Tableau de bord — répartition des dépenses du mois par catégorie.

**Décision** : `ExpenseDonutElement` (`Assets/Scripts/UI/ExpenseDonutElement.cs`), même famille `Painter2D` que les deux précédents (ADR-103/119/121). Chaque quartier est un polygone (bord extérieur puis bord intérieur, uniquement `MoveTo`/`LineTo`/`ClosePath`/`Fill`) approximant un arc en `MinSegmentAngleRadians` (0,05 rad, ≈2,9°) par segment — `Painter2D.Arc` de nouveau volontairement évité (même raisonnement qu'ADR-119 pour le marqueur en losange). `Slices` ne prend que les montants bruts, la palette de 8 couleurs (`ExpenseDonutElement.Palette`, statique et publique) est partagée avec le contrôleur pour que chaque quartier et sa ligne de légende utilisent exactement la même couleur — cycle (modulo) au-delà de 8 catégories, limite assumée pour cette première version comme les autres graphiques.

**Alimentation** : `DashboardViewModel.ExpenseBreakdown` (nouveau champ, `ExpenseCategorySliceViewModel`), construit par `DashboardViewModelBuilder.BuildExpenseBreakdown` à partir des transactions réelles du compte principal sur le mois courant (montant négatif, hors virement interne, catégorie assignée) — même filtre que la colonne « réel » de `BudgetService.GetSummary`, restreint à un seul compte puisque le Tableau de bord l'est déjà pour tout le reste. Trié par montant décroissant, cohérent entre l'anneau et la légende.

**« Jamais uniquement par la couleur » (§8.4) sans équivalent trait plein/pointillé ni ordre de groupe fixe** : contrairement aux barres (ordre gauche-à-droite fixe, §ADR-121), un anneau n'a pas de position stable par catégorie — le nombre et l'identité des quartiers changent d'un mois à l'autre. Seule solution robuste : une **légende textuelle** (pastille de couleur + nom de catégorie + montant + pourcentage), construite par `DashboardController` comme de vrais `Label`/`VisualElement`, pas peinte sur le canevas. Elle sert doublement : distinction non basée sur la seule couleur, et alternative textuelle du graphique (§8.4) — les deux exigences satisfaites par le même composant plutôt que par deux mécanismes séparés.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : compilation, mapping `ExpenseBreakdown` (montants, tri, pourcentages — vérifié contre de vraies transactions de test, pas seulement asserté), présence de l'élément et `flexGrow == 1f` (même garde-fou que les deux graphiques précédents), nombre de lignes de légende, absence d'exception sans dépense ce mois-ci — tout vert du premier coup. Le rendu effectif (quartiers correctement proportionnés, légende lisible, couleurs cohérentes entre anneau et légende) ne l'est pas ; à confirmer par capture d'écran, comme les deux précédents.

**Conséquences négatives** : pas d'infobulle au survol (§8.4), même report que les deux graphiques précédents. Palette limitée à 8 couleurs distinctes — au-delà, deux catégories peuvent partager visuellement la même couleur (le cycle modulo), acceptable pour une première version mais à revoir si un usage réel avec plus de 8 catégories dépensières le rend ambigu. Le graphique d'évolution de l'épargne (Budget) reste le dernier du catalogue à construire.

**Documents concernés** : `07-Interface.md` §5/§8.

---

# 25. ADR-123 — Évolution de l'épargne construite (barres), dernier graphique du catalogue

**Contexte** : quatrième et dernier graphique de `07-Interface.md` §8.2, sur l'écran Budget — le catalogue laissait le choix entre ligne et barres.

**Décision** : `SavingsEvolutionElement` (`Assets/Scripts/UI/SavingsEvolutionElement.cs`), barres plutôt que ligne — une barre par mois se lit plus naturellement qu'une courbe pour six valeurs mensuelles indépendantes (pas un solde cumulé continu comme la courbe de trésorerie). Même famille `Painter2D` (ADR-103/119/121/122), même technique que `BudgetBarChartElement` pour les libellés (`Label` enfants repositionnés au `GeometryChangedEvent`, `Painter2D` ne dessinant pas de texte) — nouveau `DateFormat.MonthAbbreviationYear` (« sept. 2026 ») pour tenir sous une barre étroite, là où `DateFormat.MonthYear` (« Septembre 2026 ») sert déjà d'en-tête de page. Une seule série de barres (pas de groupe) : aucun enjeu « jamais uniquement par la couleur » puisqu'il n'y a rien d'autre à distinguer visuellement — première fois qu'un graphique de ce catalogue n'a pas eu besoin d'une réponse dédiée à cette exigence.

**`BudgetService.GetSavingsEvolution` (nouveau)** : calcule l'épargne réelle + engagée (même définition que `BudgetOverview.SavingsMinor`, juste répétée par mois) pour les 6 derniers mois, **sans exiger qu'un `Budget` existe pour chacun** — contrairement à `GetOverview`, qui lève si aucune ligne `budget` n'existe pour le mois demandé. Un utilisateur qui vient de commencer à utiliser l'app n'a probablement pas créé de budget pour les mois précédents ; leur historique d'épargne réelle (transactions déjà enregistrées) doit rester visible malgré tout. Réutilise la même logique privée `SumForCategoryType` que `GetOverview`, appelée une fois par mois plutôt qu'une seule fois.

**Corrigé en construisant ce graphique** : en écrivant son test, une transaction catégorisée « Épargne » serait apparue à tort dans l'anneau des dépenses (`ExpenseDonutElement`, ADR-122) — celui-ci ne filtrait que par signe (montant négatif) et présence d'une catégorie, pas par type de catégorie. Un virement vers l'épargne n'est pas une dépense. `DashboardViewModelBuilder.BuildExpenseBreakdown` restreint désormais explicitement aux catégories de type `Expense`. Repéré par effet de bord en assemblant les données de test de ce graphique-ci, pas par une revue a posteriori du précédent — worth noting que les quatre graphiques partagent assez de logique de filtrage par catégorie pour que ce genre d'angle mort se révèle seulement une fois plusieurs types de mouvement testés ensemble.

**Affichage** : carte « Évolution de l'épargne » entre la synthèse et les barres prévu/réel/engagé sur l'écran Budget, masquée par le même interrupteur `BudgetExists` que le reste de l'écran — **simplification assumée** : la donnée elle-même ne dépend d'aucun budget créé (voir ci-dessus), mais un cas particulier pour cette seule carte n'a pas été jugé utile pour une première version ; à revoir si un utilisateur sans budget pour le mois courant se plaint de ne pas voir son historique d'épargne.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : compilation, mapping `SavingsEvolution` (6 points, mois correctement abrégés, montant réel du mois testé vérifié contre une vraie transaction — pas seulement asserté), présence de l'élément et `flexGrow == 1f`, absence d'exception sans budget — tout vert du premier coup. Le rendu effectif (hauteur des barres, lisibilité des libellés de mois) ne l'est pas ; à confirmer par capture d'écran, comme les trois précédents.

**Conséquences négatives** : pas d'infobulle au survol (§8.4), même report que les trois graphiques précédents. Fenêtre fixe de 6 mois, non réglable dans cette première version.

**Confirmé le 2026-09-13, avec un vrai bug trouvé** : première capture d'écran du Budget avec un compte réel — les barres prévu/réel/engagé s'affichent correctement (Alimentation, Transport), mais la carte « Évolution de l'épargne » était **totalement vide** (aucune barre, seuls les libellés de mois visibles), cohérent avec « Épargne du mois : 0,00 € » dans la synthèse — donc pas un défaut de calcul, mais l'absence d'un état vide explicite (`§11` : « aucune donnée » doit toujours être un message, jamais une zone silencieusement vide). `LineChartElement`/`BudgetBarChartElement`/`ExpenseDonutElement` n'avaient jamais été testés avec zéro donnée réelle *en même temps qu'un écran par ailleurs peuplé* — le donut a bien un `donut-empty`, mais la savings-evolution ne l'avait pas encore. Corrigé avec `savings-empty` (même classe `empty-state` que le reste de l'app), basculé non pas sur « liste vide » (la liste a toujours 6 points par construction) mais sur « aucun montant strictement positif parmi les 6 » — condition différente du donut, ajoutée comme cas de test dédié (mois d'août, budget créé, fenêtre de 6 mois ne recoupant aucune transaction d'épargne réelle) plutôt que supposée couverte par les tests existants.

**Documents concernés** : `07-Interface.md` §5/§8.

---

# 26. ADR-124 — Infobulles au survol sur les quatre graphiques

**Contexte** : dernière exigence de `07-Interface.md` §8.4 restée différée depuis ADR-119 sur chacun des quatre graphiques du catalogue — « une infobulle apparaît au survol d'un point/d'une barre », volontairement reportée jusqu'à ce que le tracé de base de chaque graphique soit confirmé visuellement, ce qui est désormais le cas des quatre (ADR-119/121/122/123).

**Décision** : `ChartTooltip` (`Assets/Scripts/UI/ChartTooltip.cs`, `internal static`) factorise uniquement la partie strictement commune — créer un `Label` flottant (`.chart-tooltip`, nouvelle classe dans `theme.uss`), l'afficher/le positionner près du pointeur en le contenant dans les limites du graphique (marge estimée, aucune mesure de layout réelle disponible avant le prochain passage — approximation assumée, pas un calcul exact), le masquer. Chaque graphique garde sa propre détection géométrique (quel point/quelle barre/quel quartier est sous le pointeur) : `LineChartElement.FindNearestPointIndex`, `BudgetBarChartElement.FindBarUnderPointer`, `ExpenseDonutElement.FindSliceUnderPointer`, `SavingsEvolutionElement.FindBarUnderPointer` — quatre géométries différentes, pas de tentative de les unifier au-delà de ce que `ChartTooltip` fait déjà.

**Rendu testable malgré l'absence de souris réelle en batchmode** : chaque fonction de détection est **statique, publique et pure** (aucune dépendance à l'état de l'instance), exposée spécifiquement pour que `UISmokeTest.cs` puisse l'appeler directement avec des coordonnées choisies à la main plutôt que de simuler un `PointerMoveEvent` — batchmode ne peut ni ouvrir de fenêtre ni simuler d'interaction pointeur. Même logique que `RecurringOperationService.PreviewFirstOccurrenceDate` (ADR-120) : une fonction rendue publique uniquement pour la rendre vérifiable indépendamment du mécanisme qui l'invoque en usage réel (ici, `RegisterCallback<PointerMoveEvent>`, jamais exercé par les tests). Chaque géométrie de test reproduit à la main les constantes privées du composant (`GroupGap`, `BarGap`, `TopPadding`, etc., lues dans le fichier source, pas devinées) pour calculer des coordonnées de survol precises et leurs résultats attendus — vérifie aussi bien les cas positifs (sur une barre) que négatifs (dans un espacement, au-dessus d'une barre trop courte, hors zone) plutôt que seulement l'existence de l'élément.

**`ExpenseDonutElement.Slices` change de type à cette occasion** : `IReadOnlyList<long>` (montants bruts seuls, décision initiale d'ADR-122 — « le donut n'a besoin que des proportions ») devient `IReadOnlyList<ExpenseCategorySliceViewModel>`, parce que l'infobulle a besoin du nom de catégorie et du texte déjà formaté (montant, pourcentage) que la légende externe possédait déjà mais que l'élément n'avait jamais reçu. `DashboardController` simplifié d'autant (plus de `.Select(s => s.AmountMinor)` intermédiaire). Les trois autres graphiques n'ont pas eu besoin d'un changement de type équivalent : leurs view models portaient déjà tout le texte nécessaire à l'infobulle.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : la géométrie de détection (les quatre fonctions statiques, positif et négatif), la présence du `Label` d'infobulle et son état masqué par défaut — tout vert du premier coup, quatre géométries différentes toutes validées sans ajustement après écriture. Ce qui ne l'est pas, comme toujours : le rendu visuel réel de l'infobulle (position, lisibilité, chevauchement éventuel avec le bord du graphique) — nécessite une interaction souris réelle en Play Mode.

**Confirmé le 2026-09-13** : l'utilisateur a testé le survol des quatre graphiques en Play Mode et confirmé que les infobulles fonctionnent correctement.

**Conséquences négatives** : positionnement de l'infobulle approximatif (marge estimée, pas de mesure de layout réelle) — pourrait déborder légèrement dans un cas limite non testé. Pas de support tactile (sans objet, application desktop souris/clavier uniquement).

**Documents concernés** : `07-Interface.md` §8.3quinquies/§8.4.

---

# 27. ADR-125 — Reste à vivre sur le Tableau de bord

**Contexte** : dernier des quatre KPI listés pour le Tableau de bord dans `07-Interface.md` §5 (« Solde disponible, Solde prévu en fin de mois, Point bas prévisionnel, Reste à vivre ») à ne pas être construit.

**Décision** : réutilisation directe de `BudgetOverview.RemainingToLiveMinor` (ADR-115, `BudgetService.GetOverview`) — même définition qu'affichée sur l'écran Budget, pas de second calcul parallèle. `DashboardViewModelBuilder` cherche le budget du mois courant (`BudgetService.FindByYearMonth`, jamais `GetOrCreate` — cohérent avec le reste du builder, qui ne doit rien créer comme effet de bord d'un simple affichage) ; s'il existe, en tire le reste à vivre formaté ; sinon, `"—"`.

**Une différence structurelle avec le reste de l'écran, assumée plutôt que masquée** : chaque autre chiffre du Tableau de bord est calculé pour le compte principal résolu (`ResolvePrimaryAccount`). Le reste à vivre ne l'est pas — un budget n'est jamais rattaché à un compte, il agrège toutes les catégories de type Dépense allouées, tous comptes confondus. La carte affiche donc, à dessein, un chiffre qui n'est pas dans le même périmètre que ses trois voisines — cohérent avec ce que l'écran Budget affiche déjà, pas une incohérence nouvelle.

**Pourquoi un placeholder plutôt qu'un calcul silencieux** : contrairement à l'évolution de l'épargne (ADR-123), qui reste calculable sans `Budget` créé (simple somme de transactions/occurrences réelles), le reste à vivre est structurellement une notion budgétaire — sans allocation prévue par catégorie, il n'y a rien de sensé à soustraire. Pas de `GetOrCreate` déguisé ni de valeur à zéro trompeuse : `"—"`, cohérent avec le principe déjà énoncé au §11 (« aucune donnée » doit être un message explicite, jamais une valeur silencieusement fausse).

**Vérifié en batchmode** : les deux branches (avec et sans budget pour le mois affiché), plus la cohérence de valeur entre le Tableau de bord et l'écran Budget pour le même mois (même montant formaté des deux côtés) — vert du premier coup.

**Documents concernés** : `07-Interface.md` §5.

---

# 28. ADR-126 — Prochaines opérations sur le Tableau de bord

**Contexte** : avant-dernière carte du Tableau de bord listée dans `07-Interface.md` §5 (« puis graphique de trésorerie, file de vérification..., prochaines opérations, synthèse budgétaire, alertes ») à ne pas être construite. La maquette (`docs/mockups/dashboard.html`) montre une liste courte — date, libellé, montant signé, badge « Attendue »/« Estimée » par ligne.

**Décision** : `DashboardViewModelBuilder.BuildUpcomingOperations` prend les occurrences non encore échues du compte principal (`ForecastOccurrenceService.ListForAccount`, borne basse `today + 1 jour` — celles échues aujourd'hui sont déjà dans la file de vérification juste au-dessus, pas dupliquées ici ; borne haute l'horizon de prévision réglé dans les Paramètres), triées par date, limitées aux 5 premières — une prévisualisation, pas la liste complète (qui existe déjà sur l'écran Prévisions, « Occurrences prévues »). Le badge « Attendue »/« Estimée » reprend directement `occurrence.RecurringOperationId.HasValue` — exactement la même règle qu'utilise `ForecastEventBuilder` en interne pour distinguer `ForecastEventCertainty.Expected`/`Estimated` (`06-Moteur_de_prevision.md`), reformulée ici en une ligne plutôt que réutilisée telle quelle : le moteur de prévision construit des `ForecastEvent` fusionnant transactions et occurrences, plus qu'il n'en faut pour une simple étiquette sur une ligne de liste. Nouvelle classe `.badge-muted` (gris neutre) pour « Estimée », distincte du `.badge` doré déjà utilisé ailleurs (compteur de vérification, statut de budget) — cohérent avec la maquette, qui distingue visuellement les deux badges.

**Une différence structurelle de plus, comme Reste à vivre (ADR-125)** : les montants sont affichés avec `forceSign: true` (signe + explicite sur un revenu à venir, ex. Salaire) — contrairement à la file de vérification juste au-dessus, qui n'utilise pas `forceSign`. Choix délibéré pour coller à la maquette et à la convention déjà utilisée pour les figures « attendues » ailleurs (Revenus/Dépenses attendus sur Prévisions), pas une incohérence : la file de vérification n'a simplement jamais eu d'exemple positif à afficher jusqu'ici pour que la question se pose.

**Testé sur un jeu de données isolé plutôt que le scénario partagé** : au point du scénario de test où assez d'opérations récurrentes existent pour avoir de vraies occurrences à venir, plusieurs (Loyer, Salaire, Épargne mensuelle) tombent toutes dans les mêmes prochains mois — rendre l'ordre/le compte exact vérifiable aurait demandé de dépendre d'un ordre de tri secondaire (deux occurrences à la même date) non garanti par la requête SQL. Un `AppContainer` temporaire dédié, une opération récurrente (« Attendue ») + une occurrence ponctuelle (« Estimée ») à des dates distinctes, évite entièrement le problème — même stratégie que le cas « suppression bloquée par une occurrence rapprochée » d'ADR-120.

**Documents concernés** : `07-Interface.md` §5.

---

# 29. ADR-127 — Synthèse budgétaire sur le Tableau de bord

**Contexte** : avant-dernière carte du Tableau de bord listée dans `07-Interface.md` §5 à ne pas être construite (il ne reste alors que les alertes calculées à l'affichage). La maquette (`docs/mockups/dashboard.html`, carte « Budget — septembre ») montre une barre de progression par catégorie allouée : nom, figures réel/prévu, barre, note (« X € restants » ou « Dépassement de X € »).

**Décision** : `DashboardViewModelBuilder.BuildBudgetSummary` reprend telles quelles les figures déjà calculées par `BudgetService.GetSummary` (même source de vérité que le tableau de l'écran Budget, pas un second calcul) — nom de catégorie, réel, prévu, et un texte de note déjà composé en phrase complète (`NoteText`) plutôt qu'un montant signé brut, pour que le contrôleur n'ait qu'à assigner du texte, jamais à construire de phrase. `SpentRatio` (réel seul / prévu, borné à [0, 1]) est lui aussi calculé dans le builder — c'est une donnée dérivée des mêmes montants bruts que le reste de la ligne, pas un souci d'affichage propre au contrôleur — et sert directement de largeur de barre (`style.width` en pourcentage) côté `DashboardController`. Même non-disponibilité sans budget que Reste à vivre (ADR-125) : liste vide, état vide explicite (`budget-summary-empty`) plutôt qu'une carte silencieusement vide.

**Simplification assumée par rapport à la maquette** : la barre ne représente que le réel rapporté au prévu, pas réel+engagé — la maquette elle-même est ambiguë sur ce point pour sa ligne « Logement » (barre à 100 % avec puce « Engagé » alors que la ligne juste au-dessus n'a que du réel). Plutôt que de deviner une sémantique visuelle mixte non documentée, le réel seul reste cohérent avec ce que la barre représente pour toutes les autres lignes, et l'écran Budget affiche déjà le détail complet (prévu/réel/engagé/restant, plus les barres groupées `BudgetBarChartElement`) pour qui veut la vue complète — cette carte est une synthèse, pas un doublon.

**Vérifié en batchmode** : les deux branches (avec et sans budget pour le mois affiché), plus un cas concret réel dépassant son enveloppe (réutilise l'allocation Logement déjà présente dans le scénario de test partagé — prévu 700,00 €, réel 90,00 €, engagé 650,00 €, restant −40,00 € — donc pas besoin d'un jeu de données isolé ici, une seule catégorie suffit à ne laisser aucune ambiguïté d'ordre) : ratio de la barre, classe CSS de dépassement, texte de note composé — vert du premier coup.

**Documents concernés** : `07-Interface.md` §5.

---

# 30. ADR-128 — Alertes sur le Tableau de bord

**Contexte** : dernière carte de la liste originale du Tableau de bord (`01-Perimetre.md` §2.10 : « prochaines opérations, file de vérification en attente, synthèse budgétaire, alertes calculées à l'affichage »). `03-Modele_de_donnees.md` écarte explicitement l'entité `Alert` de l'ancien modèle : « alertes calculées à l'affichage, non persistées, en V1 » — rien à stocker, tout à recalculer à chaque rendu.

**Décision** : `DashboardViewModelBuilder.BuildAlerts` calcule deux types, chacun réutilisant une donnée déjà calculée ailleurs sur le même écran plutôt qu'un second calcul :
- **Solde faible** — se déclenche si le point bas prévisionnel du mois (`forecast.LowestBalanceMinor`, la même valeur déjà affichée par la carte KPI « Point bas prévisionnel ») passe sous `AppSettings.LowBalanceThresholdMinor` (Paramètres, 200,00 € par défaut). Comme « aujourd'hui » fait partie de la fenêtre sur laquelle ce point bas est calculé, un solde déjà bas maintenant et un creux encore à venir sont couverts par la même vérification — un seul seuil, pas deux alertes séparées « solde actuel bas » / « creux à venir ».
- **Dépassement de budget** — une alerte par ligne déjà marquée `IsOverBudget` dans `DashboardBudgetRowViewModel` (Synthèse budgétaire, ADR-127), message composé à partir du nom de catégorie et du `NoteText` déjà phrasé (« Dépassement de X € ») — aucune nouvelle requête, aucun nouveau calcul.

Chaque alerte porte `IsSevere` : dépassement de budget (danger/rust, cohérent avec `.amount-negative`/`.budget-summary-note-over` déjà utilisés ailleurs pour signaler un problème déjà arrivé) contre solde faible (or/avertissement, cohérent avec `.form-warning` d'ADR-120 pour un signal préventif, pas encore un problème). Nouvelle classe `.alert-dot`/`.alert-dot-severe` (pastille colorée) plutôt qu'un badge textuel — l'écran a déjà plusieurs pastilles de légende (donut) et badges (Attendue/Estimée), une pastille simple suffit ici et reste visuellement distincte.

**Premier vrai usage du seuil de solde faible** : `AppSettings.LowBalanceThresholdMinor` existe et est réglable depuis l'écran Paramètres depuis le tout début du projet (`SettingsController`), mais rien ne le consommait jusqu'ici — cette carte est le premier code qui lit réellement ce réglage pour produire un effet visible, pas seulement pour le stocker.

**Vérifié en batchmode** : aucune alerte tant qu'aucun budget n'existe et que le solde reste confortable (scénario partagé) ; exactement une alerte de dépassement une fois le budget Logement créé (réutilise le même cas qu'ADR-127, prévu 700 € / réel 90 € / engagé 650 €) ; l'alerte de solde faible, elle, vérifiée sur un `AppContainer` isolé (compte à 50,00 €, sous le seuil par défaut de 200,00 €) — le scénario partagé ne fait jamais chuter le solde du compte principal près de ce seuil, même stratégie d'isolation qu'ADR-126. Vert du premier coup.

**Avec cette carte, les sept cartes originales du Tableau de bord (`01-Perimetre.md` §2.10) sont toutes construites** — dernière pièce du Tableau de bord tel que spécifié avant le pivot vers les graphiques puis les captures d'écran réelles.

**Conséquences négatives** : pas de seuil réglable séparé pour « dépassement de budget » (toute catégorie en négatif déclenche, pas de marge de tolérance) — cohérent avec l'absence de tolérance déjà dans `BudgetCategorySummary.RemainingAmountMinor`, pas une lacune propre à cette carte. Pas de regroupement si plusieurs catégories dépassent en même temps (une alerte par catégorie, pas de résumé « 3 catégories en dépassement ») — jugé suffisamment lisible pour le nombre de catégories qu'un budget personnel alloue typiquement.

**Documents concernés** : `07-Interface.md` §5.

---

# 31. ADR-129 — Écran Catégories

**Contexte** : après avoir terminé les sept cartes du Tableau de bord (ADR-125 à ADR-128), l'utilisateur a demandé si la V1 était terminée sans l'import CSV. Plutôt que de répondre de mémoire, un audit du code (pas seulement des docs) contre `01-Perimetre.md` §2 a été fait — il a trouvé plusieurs écarts réels, dont l'absence totale d'un écran Catégories : `CategoryService` (Create/Rename/MoveUnder/Archive/Restore) existe et fonctionne depuis le tout début du projet, mais aucun écran ne l'exposait, seulement des menus déroulants en lecture seule ailleurs. `07-Interface.md` §3 (liste des écrans, écrite avant le code) ne mentionnait d'ailleurs jamais de Catégories — l'écart existait aussi bien côté doc que côté code, pas seulement une implémentation en retard sur une spec déjà écrite.

**Décision** : `Categories.uxml` + `CategoriesController`, même patron que `AccountsController` (le plus proche structurellement : liste + création + modification limitée + archivage/restauration, pas de suppression). Huitième écran, ajouté à `ShellScreen`, `Shell.uxml`, `AppBootstrap` et `SceneWiringTools` — même câblage mécanique répété sept fois déjà pour les écrans précédents.

**Validation « deux niveaux maximum » ajoutée à `CategoryService`, pas seulement à l'écran** : `Category.MoveUnder` (le type domaine) se contente de réassigner `ParentId`, sans aucune vérification — cohérent avec le principe du domaine qui ne touche jamais un repository. Mais « une catégorie ne peut pas avoir de sous-sous-catégorie » a besoin de savoir si une catégorie a des enfants, ce qui suppose une requête. Ajouté à `CategoryService.MoveUnder`/`Create` (nouveau `RequireTopLevelParent`, plus une vérification « n'a pas déjà d'enfants » et « n'est pas son propre parent ») — la première fois que ce service valide quoi que ce soit sur le paramètre `parentId`, jusque-là accepté tel quel. Le formulaire filtre déjà le menu déroulant à des catégories de premier niveau du même type, donc ce garde-fou ne se déclenche normalement jamais depuis l'écran lui-même — mais protège l'invariant documenté même si `CategoryService` est un jour appelé autrement (test, futur écran, futur import).

**Un vrai bug d'ordre de mutation trouvé et corrigé en écrivant le contrôleur** : la première version de `SubmitForm` appelait `Rename` puis `MoveUnder` en modification. Si `MoveUnder` refusait (violation deux-niveaux), le renommage avait déjà été appliqué et persisté — le formulaire affiche une erreur et reste ouvert, donnant l'impression que rien ne s'est passé, alors qu'un renommage silencieux a bien eu lieu. Ordre inversé : `MoveUnder` (la mutation qui peut réellement échouer) avant `Rename` (qui ne peut plus échouer à ce stade, le nom étant déjà validé côté formulaire) — aucune mutation partielle possible.

**Simplification assumée** : liste plate avec une colonne « Catégorie parente », pas un arbre visuel repliable — deux niveaux maximum rend un arbre disproportionné pour une première version. Le tri place chaque sous-catégorie juste après sa catégorie parente pour rester lisible malgré l'absence d'indentation visuelle.

**Vérifié en batchmode** : mapping du view model (sous-catégorie créée, tri parent/enfant, catégorie déjà sous-catégorisée exclue des choix de parent possibles), les trois refus de validation (troisième niveau, catégorie avec enfants devenant elle-même enfant, catégorie devenant son propre parent), le refus d'archivage d'une catégorie système (comportement déjà existant sur `Category.Archive`, jamais testé jusqu'ici), présence/rendu du contrôleur (peuplé et vide — ce dernier cas différent de partout ailleurs : c'est l'absence de catégories elles-mêmes qui est testée, pas l'absence de comptes), navigation du shell. Vert du premier coup, y compris le correctif d'ordre de mutation ci-dessus (trouvé et corrigé avant le premier passage batchmode, pas après un échec).

**Documents concernés** : `07-Interface.md` §3/§3bis.

---

# 32. ADR-130 — Parcours de premier lancement

**Contexte** : deuxième des cinq lacunes trouvées par l'audit du périmètre V1 (ADR-129) — `07-Interface.md` §4 décrivait déjà le parcours en détail (écrit avant le code, comme la plupart de la documentation d'interface), mais rien n'était construit.

**Décision** : `Onboarding.uxml` + `OnboardingController`, quatre étapes (Bienvenue, Premier compte, Charges et revenus facultatif, Récapitulatif) dans une seule carte affichée/masquée par étape — pas un neuvième écran de navigation, pas de scène séparée. `AppBootstrap.Awake()` teste `Container.Accounts.ListAll().Count == 0` ; si vrai, appelle `ShellController.SetContent` avec le contenu d'onboarding (exactement le même mécanisme que n'importe quel autre écran) et une nouvelle méthode `ShellController.SetSidebarVisible(false)` pour masquer la barre latérale — empêcher de naviguer ailleurs pendant un parcours guidé, sans réécrire la logique d'affichage de contenu qui existe déjà. Aucun drapeau « onboarding terminé » séparé : la condition (aucun compte) devient fausse dès la création du premier compte, ce qui suffit déjà à satisfaire « ne réapparaît jamais, même si seule l'étape 2 a été complétée » (`07-Interface.md` §4) sans état supplémentaire à maintenir.

**« Quitter à tout moment conserve les données » est vrai par construction, pas par un mécanisme dédié** : chaque compte/opération ajouté pendant l'onboarding est persisté immédiatement via `AccountService.CreateAccount`/`RecurringOperationService.Create` — les mêmes appels que n'importe quel autre écran, rien de mis en tampon en attendant une validation finale à l'étape 4 (« Terminer » n'appelle d'ailleurs aucune mutation, seulement le callback de fin). Fermer l'application à n'importe quelle étape laisse donc exactement ce qui a déjà été ajouté, sans code spécifique à écrire ou tester pour cette garantie.

**Formulaire de l'étape 3 délibérément plus minimal que l'écran Opérations récurrentes** : seulement Dépense/Revenu (pas Épargne/Virement interne — hors du « salaire, loyer, une ou deux factures » du périmètre), pas de champ jour du mois ni date de début. Chaque opération créée ici démarre aujourd'hui, donc son jour du mois (jamais demandé, déduit de la date de début par `ForecastOccurrenceGenerator`) correspond toujours exactement à la date de début — élimine structurellement le piège date-de-début/jour-du-mois qu'ADR-120 a dû corriger ailleurs, plutôt que de le réintroduire dans un nouveau formulaire.

**Public uniquement pour la testabilité, même logique qu'ADR-120/124/129** : `Button.clicked` est un `event` C# classique, invocable seulement depuis la classe qui le déclare — impossible de simuler un clic depuis `UISmokeTest.cs` (assembly différent). `GoToStep`, `AddAccount`, `AddOperation`, `TryAdvanceFromAccountStep` et `Finish` sont donc publics, uniquement pour que les tests puissent piloter tout le parcours directement plutôt que de se limiter à l'état initial du constructeur — la glue des boutons (`clicked +=`) reste privée. Première fois que ce patron s'applique à une machine à états à plusieurs écrans plutôt qu'à un pur calcul (détection de survol, aperçu de date) : le principe reste le même — exposer uniquement ce que le déclencheur réel (ici, une interaction utilisateur) ne permet pas de vérifier autrement.

**Un bug de validation qui aurait pu passer inaperçu, écarté par construction plutôt que par un test après coup** : si le formulaire avait exposé le jour du mois comme sur l'écran Opérations récurrentes, la même classe de bug que celle documentée dans le journal du projet (une opération créée le 29 avec un jour du mois 28 saute silencieusement le premier mois) aurait pu se reproduire ici, dans le tout premier écran vu par l'utilisateur. En ne demandant jamais ce champ et en fixant toujours la date de début à aujourd'hui, le problème ne peut structurellement pas se poser — un choix de conception plutôt qu'une correction.

**Simplification assumée** : « navigation clavier complète » et « `aria`-équivalent » (`07-Interface.md` §4) s'appuient sur l'ordre de tabulation et le focus natifs de UI Toolkit plutôt qu'une implémentation ARIA dédiée — UI Toolkit n'a pas de système ARIA à proprement parler. Pas de bouton « Quitter l'onboarding » explicite non plus : fermer l'application (ou, actuellement, naviguer autrement n'est pas possible tant que la barre latérale est masquée) est le seul moyen de sortir avant l'étape 4, cohérent avec « quitter à tout moment » du §4 qui ne décrit qu'une garantie de non-perte de données, pas une action de sortie dédiée.

**Vérifié en batchmode** : les quatre étapes pilotées de bout en bout (bienvenue → compte → refus d'avancer sans compte → compte créé → avance → opération facultative ajoutée → récapitulatif → terminer), le texte du bouton « Passer »/« Suivant » qui change selon qu'une opération a été ajoutée, les données réellement persistées à chaque étape (pas seulement affichées), le masquage/affichage de la barre latérale. Vert du premier coup malgré la taille de la machine à états.

**Documents concernés** : `07-Interface.md` §4.

---

# 33. ADR-131 — Filtres de transactions

**Contexte** : troisième des cinq lacunes trouvées par l'audit du périmètre V1 (ADR-129) — `01-Perimetre.md` §2.6 exige « recherche, filtres (compte, période, catégorie, montant, texte) », mais l'écran Transactions n'avait qu'un filtre par compte depuis le tout début du projet.

**Décision** : nouveau record `TransactionFilter` (toutes les six dimensions optionnelles : `AccountId`, `CategoryId`, `DateFrom`, `DateTo`, `AmountMinMinor`, `AmountMaxMinor`, `Text`) remplace le paramètre `int? accountFilter` de `TransactionsViewModelBuilder.Build`, qui l'applique comme une chaîne de `.Where(...)`. `TransactionsController` garde une valeur par champ (`_filterAccountId`, `_filterCategoryId`, …) reconstruite en un `TransactionFilter` à chaque `Refresh()`. Pas de nouveau champ `CategoryFilterOptions` sur le view model : la liste `Categories` déjà présente (menu déroulant du formulaire de création) sert aussi au filtre, avec un placeholder différent selon le contexte (« Aucune » en création, « Toutes les catégories » en filtre) — même liste, même construction, deux menus déroulants distincts.

**Comparaison par magnitude, pas par montant signé** : `AmountMinMinor`/`AmountMaxMinor` se comparent à `Math.Abs(t.AmountMinor)`, jamais au montant signé — « entre 50 et 100 € » doit retrouver aussi bien une dépense qu'un revenu de cette taille, sans que l'utilisateur ait à connaître le signe à l'avance pour formuler sa recherche.

**Filtrage live, jamais bloquant** : chaque champ (compte, catégorie, dates, montants, texte) déclenche un nouveau filtrage à chaque changement, sans bouton « Rechercher » séparé. Une date ou un montant qui ne s'analyse pas (`DateFormat.TryParseInput`/`MoneyFormat.TryParseEurosToMinor` renvoient `false`, ex. pendant la frappe) est traité comme « aucune contrainte sur ce champ » plutôt que de bloquer tout le filtre ou d'afficher une erreur — cohérent avec une recherche incrémentale plutôt qu'un formulaire soumis, où interrompre l'utilisateur à chaque caractère serait la pire expérience possible.

**Recherche texte** : `OriginalLabel.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0`, pas l'overload `string.Contains(string, StringComparison)` — repéré et écarté avant toute tentative de compilation, la même famille de risque que `Dictionary.GetValueOrDefault` déjà documentée par ADR-114 pour cet assembly (`FinanceOS.UI`).

**`OnFilterChanged` public uniquement pour la testabilité, même logique qu'ADR-120/124/129/130** : un `VisualTreeAsset` instancié sans panel attaché (le cas de `UISmokeTest.cs`) ne dispatch jamais le `ChangeEvent` qu'une affectation `.value` normale envoie — `RegisterValueChangedCallback` ne se déclenche donc pas, la même limitation que `Button.clicked`, mais pour un champ plutôt qu'un bouton. Trouvé en pratique (pas anticipé) : un premier passage batchmode a échoué sur une assertion qui, en réalité, ne testait rien — le filtre texte n'avait jamais été appliqué, la liste étant restée non filtrée par coïncidence sur le premier cas testé (« carrefour » correspondait de toute façon). Corrigé en rendant `OnFilterChanged` public et en l'appelant directement depuis le test après un `SetValueWithoutNotify`, plutôt qu'en comptant sur l'assignation `.value` pour déclencher le callback.

**Vérifié en batchmode** : chacune des six dimensions du filtre côté `TransactionsViewModelBuilder` (compte, catégorie, période, montant par magnitude, texte insensible à la casse), plus un passage par les champs réels de `Transactions.uxml` (texte qui filtre puis se réinitialise, date qui exclut puis qu'une valeur non analysable rend inoffensive, montant minimum qui exclut). Vert après correction du piège panel-less ci-dessus.

**Documents concernés** : `01-Perimetre.md` §2.6.

---

# 34. ADR-132 — Détection assistée des virements internes

**Contexte** : quatrième des cinq lacunes trouvées par l'audit du périmètre V1 (ADR-129) — `01-Perimetre.md` §2.6 exige une « détection assistée des virements internes (deux transactions de montants opposés, dates proches, comptes différents → proposition, jamais automatique) ». Le modèle domaine la prévoyait déjà en totalité (`TransferLink` avec trois statuts Suggested/Confirmed/Rejected, `TransferLink.Reject()` jamais appelé nulle part jusqu'ici) et le commentaire même d'`InternalTransferService` anticipait explicitement « la détection assistée d'une paire déjà importée » — seul le calcul de détection et son écran manquaient.

**Décision** : nouveau service `TransferDetectionService` (`FinanceOS.App`), pris par `TransactionsController` en plus des services déjà étroits qu'il détient (même choix de dépendance minimale que le reste de cet écran). `DetectCandidates()` recalcule la liste à chaque appel — aucun état « suggéré » n'est jamais écrit en base ; seules `Confirm(...)` et `Reject(...)` persistent quoi que ce soit, et seulement sur action explicite de l'utilisateur, ce qui satisfait littéralement le « jamais automatique » du §2.6 : rien n'existe en base tant que personne n'a cliqué. Nouvelle carte « Virements internes suggérés » en haut de l'écran Transactions, même gabarit visuel que la file de vérification des Prévisions (`.verification-row`/`.verification-row-label`/`.verification-row-date`/`.verification-row-amount`, classes déjà existantes, aucune nouvelle règle USS nécessaire) — deux boutons par ligne, « Confirmer » (marque les deux transactions `IsInternalTransfer` et persiste un `TransferLink` Confirmed, exactement l'état final d'`InternalTransferService.CreateTransfer`) et « Ignorer » (persiste un `TransferLink` Rejected sans toucher aux transactions, pour que cette paire précise ne soit plus jamais proposée).

**Algorithme de détection, assumé simple plutôt que générique** : transactions non déjà marquées virement et non déjà liées (recherche par `TransferLinkRepository.ListAll()`, tous statuts confondus — un rejet doit rester définitif), triées par date, appariées par un parcours glouton (première correspondance valide = comptes différents, même devise, montants strictement opposés, écart de date ≤ 3 jours). "Dates proches" interprété comme 3 jours plutôt que le même jour : un virement bancaire réel peut se valoriser à un jour d'écart de chaque côté. Ce glouton peut en théorie rater un appariement optimal si trois transactions candidates existent dans la même fenêtre — jugé acceptable pour une application personnelle où ce cas est rare, plutôt que d'introduire un solveur d'appariement pour un scénario qui ne s'est jamais présenté.

**Nouvelle méthode `TransferLinkRepository.ListAll()`** — la seule addition côté Data ; tout le reste existait déjà (`TransferLink.Reject()`, les trois statuts, `TransactionRepository.Update` pour la pose du flag). Aucune requête SQL n'a été nécessaire pour le calcul lui-même : à l'échelle de données d'une application personnelle, un appariement en mémoire sur `ListAll()` est plus simple qu'une requête d'auto-jointure, cohérent avec le choix déjà fait ailleurs (`BudgetService`, `ForecastCalculator`) de préférer du C# en mémoire à du SQL complexe quand le volume le permet.

**Vérifié en batchmode** : côté `TransferDetectionService` (une paire réelle détectée, l'ordre de création n'influence pas la résolution sortant/entrant, un appel répété ne consomme ni ne duplique, un écart de dates trop grand n'est jamais suggéré, une paire sur le même compte n'est jamais suggérée, un rejet rend la paire définitivement invisible sans toucher aux transactions, une confirmation marque les deux jambes et fait disparaître la paire des suggestions futures) et côté `TransactionsController` (compteur et état vide corrects avec zéro candidat, une vraie paire non liée rendue comme une ligne, le rejet actualise l'écran) — cette dernière sur une fixture isolée (compte/solde propres), même raisonnement que les autres blocs isolés de ce fichier de test : une paire de virement ajoutée à la fixture partagée aurait changé des soldes déjà vérifiés par d'autres assertions plus loin dans le même fichier.

**Documents concernés** : `01-Perimetre.md` §2.6.

---

# 35. ADR-133 — Création d'occurrences ponctuelles

**Contexte** : cinquième et dernière lacune trouvée par l'audit du périmètre V1 (ADR-129) — `01-Perimetre.md` §2.7 prévoit des « occurrences ponctuelles (dépense ou revenu exceptionnel prévu, sans récurrence) ». `ForecastOccurrenceService.Create` existe depuis la construction du moteur de prévision (il sert notamment à `ConfirmAsTransaction` et à la génération récurrente), mais son seul appelant dans toute l'application était du code de fixture de test — aucun écran n'y menait.

**Décision** : formulaire de création directement sur la carte « Occurrences prévues » de l'écran Prévisions, plutôt qu'un nouvel écran ou une nouvelle carte séparée — l'occurrence créée y apparaît immédiatement après rafraîchissement, donc la garder au même endroit que là où elle se lit ensuite évite une navigation superflue. Repliée par défaut derrière un bouton « + Nouvelle occurrence » dans l'en-tête de carte, même patron que le bouton « Simuler un scénario » déjà en place sur ce même écran (ADR-117) — cette carte est déjà dense, et la création reste une action occasionnelle. Le formulaire crée toujours pour le compte actuellement sélectionné dans le sélecteur de l'écran (pas de champ compte séparé) : l'écran entier est déjà scopé à un compte à la fois, et les occurrences affichées juste en dessous le sont déjà pour ce même compte — ajouter un second sélecteur de compte aurait introduit une incohérence (« pour quel compte la liste se rafraîchit-elle après création ? ») sans bénéfice réel.

**Portée volontairement limitée à Dépense/Revenu, pas de virement ponctuel** : `ForecastOccurrenceService.Create` accepte un `destinationAccountId` optionnel (utilisé ailleurs pour les occurrences de virement récurrent), mais `01-Perimetre.md` §2.7 ne mentionne que « dépense ou revenu exceptionnel » pour ce cas précis — un virement ponctuel exceptionnel n'est pas un besoin exprimé. Le formulaire n'expose donc que Type (Dépense/Revenu), pas de compte destination, même choix de minimalisme assumé que le formulaire d'onboarding (ADR-130) pour une raison différente : ici, coller à la formulation exacte de la spec plutôt que d'exposer tout ce que le service peut techniquement faire.

**Aucun changement côté `App`/`Domain`/`Data`** — première des cinq lacunes de l'audit à ne nécessiter aucune addition en dessous de la couche UI, `ForecastOccurrenceService.Create` couvrant déjà exactement ce dont le formulaire a besoin (libellé, date, montant signé, compte, catégorie optionnelle, tiers optionnel via `CounterpartyService.FindOrCreateByName`, même résolution que `TransactionsController`).

**Vérifié en batchmode, mais volontairement pas par un clic simulé** : comme pour la file de vérification de ce même écran (`RunSimulation`, jamais cliquée par les tests non plus), les boutons du nouveau formulaire ne sont pas rendus publics pour être invoqués directement — cohérence avec le choix déjà établi sur cet écran précis plutôt qu'avec le patron « public pour la testabilité » utilisé ailleurs (Transactions, Catégories, Onboarding). À la place : `ForecastOccurrenceService.Create` a reçu une couverture directe côté `AppSmokeTest.cs` (catégorie transmise, `RecurringOperationId` bien nul, apparition dans `ListForAccount`), et `UISmokeTest.cs` vérifie l'état structurel du formulaire (replié par défaut, bouton activé une fois un compte sélectionné, menu catégorie peuplé) puis qu'une occurrence créée directement via le service (le même chemin que « Créer » emprunterait) apparaît bien dans la liste après `Refresh()` — la liaison d'affichage est donc prouvée, le déclenchement par clic ne l'est pas, exactement la même limitation documentée depuis ADR-112/113.

**Documents concernés** : `01-Perimetre.md` §2.7.

---

# 36. ADR-134 — Historique de soldes officiels

**Contexte** : sixième lacune, trouvée par un second audit du périmètre V1 demandé explicitement par l'utilisateur une fois les cinq lacunes d'ADR-129 closes — `01-Perimetre.md` §2.2 exige « solde initial + historique de soldes officiels saisis manuellement ». `Account.RecordOfficialBalance`/`AccountService.RecordOfficialBalance` existaient depuis le tout début du projet (schéma, repository, service), mais leurs seuls appelants dans toute l'application étaient des fixtures de test — même schéma que quatre des cinq lacunes précédentes, mais cette fois sans aucune ADR ni aucune mention dans `01-Perimetre.md` §3 pour signaler un report délibéré : un vrai oubli, pas une simplification assumée.

**Décision** : une section « Nouveau solde officiel » + « Historique » ajoutée directement au formulaire de modification de l'écran Comptes (`AccountsController`/`Accounts.uxml`), visible uniquement en édition (masquée à la création, où le solde ne se saisit qu'une fois) — pas un nouvel écran « détail de compte » séparé, pour rester au plus près de ce que `01-Perimetre.md` §2.2 demande sans réintroduire la notion de « détail » que `07-Interface.md` §3 mentionne en passant mais qu'aucune autre partie de la documentation ne détaille. Nouvelle méthode `AccountService.ListBalanceHistory` (simple passe-plat vers `AccountBalanceSnapshotRepository.ListForAccount`, déjà trié plus-récent-d'abord).

**Un vrai bug de correction rendu atteignable en exposant cette méthode à une saisie libre, pas seulement une fonctionnalité manquante** : `Account.RecordOfficialBalance` écrasait jusqu'ici `OfficialBalanceMinor`/`OfficialBalanceDate` sans condition — inoffensif tant que le seul appelant (les tests) ne l'appelait qu'une fois par compte, dans l'ordre. Mais `OfficialBalanceDate` est le point de départ des prévisions (`ForecastCalculator.ResolveOpeningBalance`, doc : « le dernier solde officiel connu ») — un formulaire qui laisse l'utilisateur saisir n'importe quelle date rend immédiatement possible un solde « rattrapé » antérieur au point de départ actuel, qui aurait fait reculer ce point de départ et compté deux fois les transactions réelles entre les deux dates. Corrigé avant tout passage batchmode, pas après un échec constaté : `Account.RecordOfficialBalance` ne déplace désormais le point de départ que si la nouvelle observation est la plus récente connue (`balanceDate >= OfficialBalanceDate`, ou si aucune n'existe encore) ; une entrée plus ancienne (rattrapage d'historique) est tout de même conservée par `AccountBalanceSnapshotRepository.Insert`, simplement sans déplacer le point de départ. Même occasion, même méthode : un solde officiel daté dans le futur est désormais refusé (`ArgumentException`), par analogie directe avec la même règle déjà appliquée aux transactions manuelles (`Transaction`, `01-Perimetre.md` §2.6) — « officiellement observé » n'a pas de sens pour une date qui n'est pas encore arrivée.

**Public pour la testabilité, comme la majorité des écrans plutôt que comme Prévisions** : contrairement à ADR-133 (formulaire de Prévisions, boutons volontairement laissés privés par cohérence avec `RunSimulation` déjà non testé par clic sur ce même écran), `AccountsController.OpenEditForm`/`SubmitBalanceHistory` sont rendus publics — cet écran n'avait justement jamais fait ce choix explicite auparavant (son formulaire d'édition n'était testé par aucun clic, dans aucun sens), donc pas de convention locale à respecter ; le patron par défaut du projet (Transactions/Catégories/Onboarding) s'applique.

**Vérifié en batchmode** : `DatabaseSmokeTest`, `AppSmokeTest` (une observation plus récente déplace le point de départ, un rattrapage plus ancien ne le déplace pas mais reste dans l'historique, un solde futur est refusé et n'est pas non plus enregistré comme historique), `ForecastSmokeTest`, et `UISmokeTest` sur une fixture isolée (déplacer le point de départ du compte partagé aurait faussé les nombreuses assertions de prévision/budget que ce compte alimente plus loin dans le même fichier) — section masquée avant édition, peuplée à l'ouverture, une soumission réelle qui met à jour le solde du compte et la liste, une soumission invalide qui affiche l'erreur du formulaire sans ajouter de ligne. Vert au premier passage, y compris le correctif d'ordre de la date, trouvé et corrigé avant l'écriture des tests plutôt qu'après un échec.

**Documents concernés** : `01-Perimetre.md` §2.2.

---

# 37. ADR-135 — Thème sombre

**Contexte** : demande directe de l'utilisateur, pas un écart de spec — `01-Perimetre.md` §2.11 ne mentionnait aucun thème jusqu'ici (ajouté par cet ADR). La maquette originale (`docs/mockups/dashboard.html`) prévoyait déjà une palette sombre complète (`:root[data-theme="dark"]`, jamais implémentée) — réutilisée comme référence de couleurs plutôt que d'en inventer une nouvelle.

**Décision, deux mécanismes séparés selon où vit la couleur** :
1. **USS** (l'immense majorité de l'interface) : les dix jetons `--color-*` de `theme.uss` sont redéfinis dans un bloc `.shell-root.theme-dark { ... }`, basculé par `ShellController.SetTheme(AppTheme)` (`EnableInClassList("theme-dark", ...)`) sur le même élément qui les définit déjà — la cascade USS résout la redéfinition pour tout descendant automatiquement, sans toucher un seul autre fichier UXML/USS. Nouveau jeton `--color-on-accent` (blanc en clair, `--color-ink-900` sombre en foncé) — nécessaire uniquement parce que `.primary-button` avait jusqu'ici un `color: #FFFFFF;` codé en dur : une fois `--color-accent` devenu clair en mode sombre, du texte blanc dessus serait devenu illisible. Seul hex en dur trouvé hors du bloc de jetons sur l'ensemble du fichier (recherche exhaustive avant d'écrire quoi que ce soit d'autre).
2. **Painter2D** (les quatre graphiques) : une propriété `DarkTheme` par instance (même patron répété sur les quatre classes, pas de base commune — cohérent avec le reste de ces fichiers qui dupliquent déjà leurs constantes de marge plutôt que d'en extraire une classe parente), chaque couleur passant d'une constante unique à une paire `XxxLight`/`XxxDark` résolue par une petite propriété calculée. Une variable CSS n'est pas lisible depuis C#, donc ce deuxième mécanisme est nécessaire quel que soit le soin apporté au premier — confirmé en relisant ADR-119 (« couleurs codées en dur dans la classe C#, pas de lecture de theme.uss »), qui avait déjà anticipé cette dette sans la payer.

**D'où viennent les couleurs sombres** : converties directement depuis la palette dark de la maquette (`--bg`/`--surface`/`--border`/`--ink-900`/`--ink-600`/`--ink-400`/`--accent`/`--gold`/`--gold-soft`, `--color-danger` mappé sur `--negative`) — vérifié en reconvertissant d'abord les valeurs *claires* déjà existantes dans le code (`new(0.180f, 0.227f, 0.349f)` etc.) vers l'hex de la maquette pour confirmer que la méthode de conversion donne bien les constantes déjà en place, avant de l'appliquer aux valeurs sombres. Les huit couleurs supplémentaires de l'anneau des dépenses (sage/mauve/teal/ochre/clay, sans équivalent dans la maquette) ont des contreparties sombres choisies à la main, pas dérivées algorithmiquement — cohérent avec le fait que ces huit couleurs elles-mêmes n'ont jamais eu de source de vérité autre que ce fichier.

**Application live, séparée du reste du formulaire Paramètres** : contrairement à horizon/seuil/compte par défaut (qui n'ont d'effet visuel qu'après « Enregistrer »), le champ Thème persiste et s'applique dès la sélection dans le menu déroulant — choisir Sombre puis fermer l'app sans cliquer Enregistrer ne doit jamais silencieusement revenir au clair au prochain lancement. `SettingsController` reçoit un callback `onThemeChanged` optionnel, câblé par `AppBootstrap` directement vers `ShellController.SetTheme`. Les contrôleurs de Dashboard/Budget/Prévisions n'ont en revanche pas besoin d'un mécanisme de mise à jour à chaud pour leurs graphiques : `AppBootstrap` reconstruit systématiquement chaque écran de zéro à chaque navigation, donc lire `AppSettings.Theme` une seule fois au constructeur (ou, pour Prévisions qui détient déjà tout `AppContainer`, directement à la construction du graphique) suffit — le prochain passage sur cet écran verra déjà la bonne valeur.

**Non couvert par le batchmode, assumé comme telle** : la conformité exacte des teintes (contraste, lisibilité) n'est vérifiable que par une capture d'écran réelle de l'utilisateur — même limitation qu'ADR-112/116, juste étendue à « la couleur choisie est-elle la bonne » plutôt que seulement « la mise en page tient-elle ». Ce qui est vérifié en batchmode : la classe `.theme-dark` bascule bien sur `.shell-root`, le champ Thème du formulaire Paramètres offre exactement Clair/Sombre et persiste/déclenche le callback dès la sélection (pas seulement au clic sur Enregistrer), et chaque graphique reçoit bien `DarkTheme = true` quand son écran est construit avec le thème sombre actif (structurel — la propriété est correctement câblée, pas que le rendu Painter2D produise exactement les bons pixels).

**Mise à jour (2026-09-14) — un troisième mécanisme, trouvé après coup par une vraie capture d'écran** : la première capture sombre de l'utilisateur (écran Transactions) a montré l'en-tête du tableau (`MultiColumnListView`) illisible — texte pâle sur un fond resté clair. Un premier correctif en USS (`.transactions-table .unity-multi-column-view__header-container { background-color: var(--color-border); }`) a compilé sans erreur mais n'a rien changé à l'écran, confirmé par une seconde capture identique. Diagnostiqué avec l'aide de l'utilisateur via le UI Toolkit Debugger de Unity (Window > UI Toolkit > Debugger, outil « Pick Element ») : chaque en-tête de colonne (`MultiColumnHeaderColumn`, classe `unity-multi-column-header__column`) porte son propre `background-color` **en style inline**, pas via la feuille de style du thème par défaut comme supposé — un style inline gagne toujours contre n'importe quelle règle USS, aussi spécifique soit-elle, donc aucune règle CSS ne pouvait jamais fonctionner ici, quel que soit le sélecteur choisi. Le nom de classe du conteneur trouvé la première fois (via une recherche Unity Discussions) était d'ailleurs correct — le problème n'était pas le nom, mais que ce conteneur n'est pas ce qui peint le fond visible : ses enfants (les colonnes d'en-tête) le recouvrent chacun avec leur propre fond.

**Correctif réel** : nouvelle classe utilitaire `TableHeaderTheme` (`FinanceOS.UI`), appelée depuis chaque contrôleur possédant un `MultiColumnListView` (Transactions, Catégories, Opérations récurrentes, Budget, et les deux tableaux de Prévisions) juste après avoir récupéré la référence à la liste. Elle enregistre un gestionnaire `GeometryChangedEvent` qui réécrit explicitement le `style.backgroundColor` de chaque élément `.unity-multi-column-header__column` — un événement de géométrie plutôt qu'un appel direct, parce qu'un `MultiColumnListView` ne construit son en-tête qu'après un vrai passage de mise en page (confirmé une quatrième fois, après ADR-112/113/117 : une tentative d'introspection en batchmode juste après `Rebuild()` n'a trouvé aucun enfant du tout). `TransactionsController` et `CategoriesController`, qui n'avaient jusqu'ici aucune notion de thème, reçoivent désormais un `bool isDarkTheme = false` optionnel comme `DashboardController`/`BudgetsController` ; `RecurringOperationsController` et `ForecastsController` réutilisent l'`AppSettingsService`/`AppContainer` qu'ils détenaient déjà.

**Leçon générale pour la suite** : quand l'introspection en batchmode d'un contrôle Unity intégré ne révèle rien (aucun enfant construit), ne pas insister localement (grep du binaire, etc.) — une recherche web (Unity Discussions, en particulier) trouve presque toujours quelqu'un ayant déjà buté sur le même besoin de personnalisation, plus fiable qu'une rétro-ingénierie locale. Mais une fois le bon sélecteur trouvé, vérifier aussi *où* vit la couleur (feuille de style vs. style inline) avant d'écrire la règle USS — un style inline est un mur qu'aucune spécificité CSS ne franchit, symétrique à la leçon ADR-119 (« Painter2D ne lit pas les variables CSS ») : ici, c'est un élément *Unity lui-même*, pas notre propre code, qui contourne la cascade USS de la même façon.

**Documents concernés** : `01-Perimetre.md` §2.11, `docs/mockups/dashboard.html` (palette source).

---

# 38. ADR-136 — Premier installeur Windows

**Contexte** : demande directe de l'utilisateur (« faire tester mon application à mon entourage »), pas un écart de spec — `04-Stack_technique.md` §9 décrivait déjà ce choix en détail avant tout code (build Standalone Windows IL2CPP x64, packaging via Inno Setup, script versionné dans `packaging/`), mais rien n'était construit ; Phase 5 du plan (`01-Perimetre.md` §3) restait à son tout début.

**Deux outils absents de la machine, installés avant de pouvoir continuer** : ni le module Windows IL2CPP d'Unity, ni Inno Setup lui-même n'étaient présents. Plutôt que de basculer silencieusement sur Mono (le seul backend déjà installé) ou sur un simple zip portable (l'alternative que `04-Stack_technique.md` §9 autorise explicitement), les deux options ont été posées explicitement à l'utilisateur avant d'agir — un téléchargement de plusieurs Go (le module IL2CPP) et l'installation d'un nouvel outil sur sa machine ne sont pas des actions à prendre par défaut. Choix confirmé : coller à la doc dès ce premier build plutôt que couper les coins. Module IL2CPP installé via `Unity Hub.exe -- --headless install-modules --version 6000.3.12f1 --module windows-il2cpp`, Inno Setup via `winget install -e --id JRSoftware.InnoSetup` (ni l'un ni l'autre n'avait de prompt UAC/EULA bloquant en mode silencieux).

**`Assets/Scripts/Editor/BuildScript.cs`** (nouveau) : un unique point d'entrée `[MenuItem]`/batchmode, `FinanceOS.EditorTools.BuildScript.BuildWindowsPlayer`, qui fixe `companyName`/`productName` (`DefaultCompany`/`finance-os-desktop` n'étaient que des valeurs par défaut de projet, jamais changées jusqu'ici) et le scripting backend en IL2CPP via l'API `PlayerSettings` avant d'appeler `BuildPipeline.BuildPlayer` sur `Assets/Scenes/Main.unity`, cible `StandaloneWindows64`. Sortie dans `Build/Windows/` (gitignored, comme `packaging/Output/`) — jamais versionné, reconstruit à chaque fois depuis les sources.

**Un vrai piège de packaging trouvé avant de le découvrir en production, pas après** : le dossier de sortie IL2CPP contient `FinanceOS_BackUpThisFolder_ButDontShipItWithYourGame` — des symboles de debug natifs, **527 Mo sur 608 Mo de sortie totale**, soit la quasi-totalité du poids du build. Le nom du dossier le dit lui-même : à conserver pour un futur débogage natif, jamais à distribuer. Exclu explicitement dans `packaging/FinanceOS.iss` (`Excludes:`) avant la première compilation de l'installeur — repéré en inspectant la taille du dossier de build juste après le premier build réussi, pas après avoir livré un installeur de 500+ Mo par erreur.

**Installation par utilisateur, pas par machine** : `PrivilegesRequired=lowest` + `DefaultDirName={userpf}\Finance OS` — aucune invite UAC, aucun mot de passe administrateur requis. Choix délibéré pour ce public précis (des proches testant sur leur propre PC, pas un déploiement d'entreprise) : demander un mot de passe admin à un non-technicien pour tester une application de gestion de finances personnelles aurait été une friction disproportionnée. Cohérent avec le choix déjà fait pour l'emplacement des données (`AppDatabasePath.Resolve()`, portable-first, repli sur `%APPDATA%` si le dossier d'installation n'est pas modifiable) — un utilisateur non-admin installé dans son propre dossier utilisateur reste pleinement fonctionnel sans repli nécessaire.

**Vérifié, mais pas entièrement** : l'installeur compilé a été testé de bout en bout en mode silencieux (`/VERYSILENT /SUPPRESSMSGBOXES`, PowerShell — Git Bash réécrit silencieusement les arguments `/FLAG` de style Windows en chemins MSYS, un piège rencontré et contourné en cours de route) — installation réelle dans un dossier temporaire (les bons fichiers présents, le dossier de symboles bien absent), puis désinstallation via `unins000.exe`, dossier entièrement supprimé ensuite. Ce qui reste **non vérifié depuis cet environnement** : que `FinanceOS.exe` lui-même se lance et s'affiche correctement une fois installé — aucun affichage ni GPU disponible ici pour un exécutable joueur réel, même limitation qu'ADR-112 mais appliquée cette fois à un build fini plutôt qu'à l'éditeur. L'utilisateur doit encore lancer l'application une première fois lui-même avant de la transmettre à son entourage.

**Simplification assumée** : pas d'icône personnalisée (ni pour l'exécutable ni pour l'installeur — icônes par défaut d'Unity/Inno Setup), pas de page de licence dans l'assistant d'installation. Aucun des deux n'est nécessaire pour un premier partage à des proches ; à revisiter avant une diffusion publique plus large (GitHub Releases, per `04-Stack_technique.md` §9).

**Documents concernés** : `04-Stack_technique.md` §9, `01-Perimetre.md` §3 (Phase 5).

---

# 39. ADR-137 — Correction du compte d'une opération récurrente après création

**Contexte** : premier vrai retour terrain après l'installeur ADR-136 — l'utilisateur s'est trompé de compte en créant une opération récurrente pendant l'onboarding, et n'a jamais pu la corriger, même une fois l'onboarding terminé. `RecurringOperationService.Delete` existait déjà comme réponse documentée à ce type d'erreur (« la seule façon aujourd'hui d'annuler une erreur de saisie »), mais supprimer puis recréer tout un salaire ou un loyer pour corriger un seul champ est une réponse disproportionnée à une simple faute de frappe — et ce n'est pas ce que l'utilisateur a cherché à faire (« je n'ai jamais pu le **modifier** »).

**Décision** : `RecurringOperation.ChangeAccounts` (Domain, nouveau) lève les setters `private` sur `SourceAccountId`/`DestinationAccountId` (jusqu'ici de simples accesseurs `{ get; }`, réellement immuables) et réutilise la même validation que le constructeur (extraite dans `ValidateAccounts`, partagée entre les deux plutôt que dupliquée) — une dépense a toujours besoin d'un compte source, etc. `RecurringOperationService.ChangeAccounts` persiste le changement puis appelle une nouvelle `ForecastOccurrenceRepository.DeletePendingForRecurringOperation` : les occurrences déjà générées avec l'ancien compte (`planned`/`missed` uniquement — jamais `matched`, `cancelled` ni `ignored`, qui représentent une décision déjà prise et ne doivent pas être effacées) sont supprimées plutôt que laissées à traîner avec le mauvais compte à côté des futures occurrences correctement régénérées. Le contrôleur régénère ensuite immédiatement (`GenerateUpcomingOccurrences`), même geste que déjà fait après création ou reprise — le correctif est visible tout de suite, pas seulement pour les occurrences futures.

**Un compte destination corrigé nécessitait un vrai stockage** : `RecurringOperationRepository.Update` n'écrivait jusqu'ici ni `source_account_id` ni `destination_account_id` en base — logique tant que ces champs étaient immuables, mais un piège prêt à mordre silencieusement (une correction qui semble réussir en mémoire mais ne survit pas au prochain chargement) si quelqu'un avait ajouté cette capacité sans y penser. Colonnes déjà présentes dans le schéma depuis le tout début (utilisées par `Insert`) ; seul `UPDATE` manquait ces deux colonnes.

**Écran** : dans le formulaire de modification, le ou les comptes redeviennent des menus déroulants modifiables (au lieu du résumé lecture-seule précédent, désormais entièrement retiré de l'UXML et du contrôleur) — même visibilité conditionnelle selon le type (`AccountRequirementsFor`, extrait en une seule méthode partagée par la création, l'édition et l'affichage du formulaire, là où la même logique « dépense a besoin d'un compte source » était dupliquée trois fois avant ce correctif) que lors de la création, pré-sélectionnés sur le compte actuel. Type, fréquence, date de début, jour du mois restent en lecture seule — seule la portée de l'édition s'élargit, pas le principe de limiter l'édition aux champs où une correction a un sens sans tout redemander.

**Vérifié en batchmode** : côté service (trois occurrences générées sur le mauvais compte, `ChangeAccounts` les supprime et persiste le bon compte, la régénération produit trois occurrences fraîches sur le bon compte, la validation « une dépense a besoin d'un compte source » s'applique toujours après coup) sur la fixture partagée existante — sur une opération dédiée, pas `rent`, pour ne pas perturber les assertions déjà faites sur ses occurrences d'octobre/novembre passées en « Manquée » plus loin dans le même fichier. Côté UI, sur une fixture isolée (même raisonnement) : le champ compte s'ouvre bien modifiable et pré-sélectionné sur le mauvais compte, la soumission via le formulaire persiste la correction et fait disparaître l'ancienne occurrence du mauvais compte. `OpenEditForm`/`SubmitForm` rendus publics pour la testabilité — cet écran n'avait jamais fait de choix explicite dans un sens ou l'autre avant ce jour, donc le patron par défaut du projet s'applique (comme pour `AccountsController`, ADR-134).

**Documents concernés** : `07-Interface.md` §3, `Assets/Scripts/UI/README.md`.

---

# 40. ADR-138 — Fenêtre standard et bouton Quitter

**Contexte** : deuxième retour terrain, dans le même message que ADR-137 — « impossibilité de quitter l'application proprement ». Cause trouvée en inspectant `ProjectSettings.asset` : aucun réglage de plein écran n'y avait jamais été fixé explicitement, donc le comportement par défaut d'un nouveau projet Unity s'appliquait — plein écran exclusif/sans bordure, pensé pour un jeu, jamais adapté à un utilitaire de bureau. Sans barre de titre ni bouton de fermeture visibles, et selon la configuration, Alt+F4 lui-même peut se comporter différemment en plein écran exclusif — l'utilisateur n'avait tout simplement aucun moyen visible de fermer l'application.

**Décision, deux correctifs complémentaires plutôt qu'un seul** :
1. `BuildScript.BuildWindowsPlayer` fixe désormais explicitement `PlayerSettings.fullScreenMode = FullScreenMode.Windowed` et `resizableWindow = true`, aux côtés des autres réglages déjà posés là (nom de société/produit, backend IL2CPP) — persisté dans `ProjectSettings.asset` comme les précédents, pas seulement pour ce build.
2. Un bouton « Quitter » ajouté en bas de la barre latérale (`Shell.uxml`, toujours visible depuis n'importe quel écran, séparé visuellement des éléments de navigation par une bordure et une marge plutôt qu'un style alarmant — quitter n'est pas une action destructive). Décision délibérée de ne pas se reposer uniquement sur le correctif n°1 : même une fois une vraie fenêtre avec bordures obtenue, une action de sortie explicite et déclarée dans l'interface reste une meilleure pratique d'UX de bureau, indépendamment de ce que l'OS fournit par ailleurs.

**`Application.Quit()` ne fait rien dans l'éditeur** (comportement documenté de Unity, pas une découverte) — `AppBootstrap.QuitApplication` bascule donc sur `UnityEditor.EditorApplication.isPlaying = false` sous `#if UNITY_EDITOR`, `Application.Quit()` sinon ; `OnApplicationQuit()` (déjà existant, dispose déjà `Container`) se déclenche dans les deux cas sans changement.

**Reconstruit et republié** : ce correctif ne changeait rien pour l'utilisateur tant que l'installeur ADR-136 n'était pas régénéré — nouveau build IL2CPP, nouvel installeur compilé et revérifié (installation/désinstallation silencieuses), version montée à 1.0.1 pour distinguer ce correctif du tout premier envoi.

**Documents concernés** : `07-Interface.md` §2, `09-Decisions_techniques.md` ADR-136.

---

# 41. ADR-139 — Vérification et téléchargement automatiques de mise à jour

**Contexte** : demande directe de l'utilisateur, explicitement posée comme « un temps de réflexion » plutôt qu'une simple implémentation — quatre souhaits (numéro de version affiché, notification de nouvelle version, mise à jour si possible silencieuse, popup + choix de redémarrer). Tension frontale avec un principe déjà écrit noir sur blanc dans `08-Confidentialite_et_donnees.md` §1 : « aucun appel `UnityWebRequest`/`HttpClient`... sauf éventuellement un bouton **manuel**... jamais automatique, jamais silencieux ». Signalé explicitement à l'utilisateur avant d'écrire quoi que ce soit, avec les raisons concrètes de s'y attarder (un vrai auto-update silencieux demande un processus « updater » séparé, Unity ne pouvant pas remplacer son propre exécutable en cours d'exécution ; un programme qui télécharge et exécute un binaire seul, en silence, est aussi le genre de comportement que Defender/antivirus surveillent — on venait déjà de voir SmartScreen réagir à un installeur téléchargé **manuellement**). Deux arbitrages demandés explicitement par l'utilisateur, pas devinés : vérification automatique au lancement (pas un bouton manuel — un vrai changement de politique documentée, assumé) ; téléchargement automatique mais confirmation explicite **avant** d'installer (pas un « installé puis redémarrer » entièrement silencieux comme demandé au départ).

**Décision** :
1. **Détection** : `UpdateChecker.CheckAndDownload` (`FinanceOS.UI`, nouveau), une coroutine Unity lancée par `AppBootstrap.Awake()` une fois le premier écran déjà affiché (une vérification lente ou hors-ligne ne retarde jamais le démarrage). Interroge `https://api.github.com/repos/Thrall81/finance-os-desktop/releases/latest` — API publique, anonyme, sans clé — et compare le tag de la release (`vX.Y.Z`) à `Application.version` via `System.Version`. Aucune donnée personnelle ou financière ne transite jamais par cet appel, seulement un numéro de version — la vraie garantie de `08-Confidentialite_et_donnees.md` (« aucune donnée perso/financière sur le réseau ») reste donc intacte, seule la clause littérale « jamais automatique » cède, sciemment et de façon scopée.
2. **Téléchargement** : si une version plus récente existe, l'installeur (le premier asset `.exe` de la release) est téléchargé silencieusement vers `Application.temporaryCachePath` — toujours sans aucune interaction utilisateur à ce stade.
3. **Installation, jamais automatique malgré tout ce qui précède** : une fois le téléchargement terminé, `ShellController.ShowUpdateReady` affiche une fenêtre modale (« La version X a été téléchargée. Voulez-vous l'installer maintenant ? »), superposée à l'écran actuel quel qu'il soit (élément enfant direct de `.shell-root`, pas de `.content-area`, pour survivre à la navigation). Rien ne s'exécute avant un clic explicite sur « Installer et redémarrer » — c'est le point de contrôle humain qui rend la première clause de `08-Confidentialite_et_donnees.md` toujours vraie dans l'esprit, même si sa lettre a changé.

**Toute défaillance du contrôle de version reste silencieuse, jamais une erreur affichée** : hors-ligne, GitHub injoignable, réponse malformée, aucun asset `.exe` trouvé, échec de téléchargement — chaque chemin d'échec dans `UpdateChecker.CheckAndDownload` sort simplement sans rien faire (`yield break`). Un contrôle de version qui échoue ne doit jamais ressembler à un bug de l'application, et ne doit jamais réessayer de façon agressive — le prochain contrôle naturel est simplement le prochain lancement.

**Logique de décision séparée du réseau, délibérément, pour rester testable** : `IsNewerVersion`/`FindInstallerDownloadUrl` sont des méthodes statiques pures (aucune dépendance à `UnityWebRequest`), rendues `public` (plutôt qu'`internal`) uniquement pour que `UISmokeTest.cs` puisse les appeler directement — le flux réseau/coroutine réel, lui, ne peut tout simplement pas s'exécuter en batchmode, et ne devrait de toute façon jamais dépendre d'un vrai accès internet pour être vérifié.

**Installation silencieuse mais fiable, grâce à Inno Setup plutôt qu'à une orchestration C# fragile** : lancer l'installeur `/VERYSILENT` pendant que `FinanceOS.exe`/`GameAssembly.dll` sont potentiellement encore verrouillés par le processus en cours aurait pu échouer silencieusement. Plutôt que d'orchestrer à la main un arrêt précis de l'application avant de lancer l'installeur (risque réel de conditions de course), `packaging/FinanceOS.iss` active `CloseApplications`/`RestartApplications` — Windows Restart Manager détecte et ferme le processus concerné automatiquement, puis relance l'application une fois l'installation terminée, y compris en mode silencieux. `AppBootstrap.InstallUpdateAndRestart` se contente de lancer l'installeur puis de quitter proprement (`QuitApplication`, qui dispose déjà `Container`) — la mécanique de coordination délicate est confiée à un outil déjà conçu pour ça plutôt que réinventée.

**Mise en place concrète, pas seulement documentée** : ce projet n'avait jamais eu de dépôt distant (`git remote -v` vide) — le plan « GitHub Releases » de `04-Stack_technique.md` §9 n'était qu'une intention depuis le début. Dépôt public créé et le code poussé (`github.com/Thrall81/finance-os-desktop`), `gh` CLI installé et authentifié pour l'occasion, première release `v1.0.1` publiée avec l'installeur déjà construit comme asset — sans quoi la vérification de version n'aurait rien eu à trouver.

**Affichage de version** : `Application.version` (lit `PlayerSettings.bundleVersion`) affiché dans une nouvelle carte « À propos » de Paramètres — lu une seule fois à la construction du contrôleur plutôt qu'à chaque `Refresh()`, puisqu'il ne change jamais en cours de session, contrairement au reste de cet écran.

**Documents concernés** : `08-Confidentialite_et_donnees.md` §1, `04-Stack_technique.md` §7/§9, `01-Perimetre.md` §2.11.

---

# 42. ADR-140 — Édition étendue des opérations récurrentes

**Contexte** : deuxième vrai retour terrain sur cet écran après ADR-137 (compte seul) — l'utilisateur, en éditant une opération réelle (« Facture Electricité »), a constaté que le type, la fréquence, la catégorie et le tiers restaient tous en lecture seule, sans pouvoir demander concrètement à quoi correspondait « Tiers » (réponse : le nom libre optionnel du bénéficiaire/émetteur, `01-Perimetre.md` §2.5 — pas une fiche complexe).

**Décision** : tous les champs deviennent modifiables en édition, sauf le nom et la date de début. La date de début reste immuable dans `RecurringOperation` par conception (`{ get; }`, pas de `private set`) ; le nom n'a simplement jamais eu de besoin remonté — aucun des deux n'a été retiré du périmètre pour une raison technique bloquante, juste laissé de côté.

**`RecurringOperation.Type` était littéralement immuable** (`{ get; }`), pas seulement restreint côté UI — passé à `{ get; private set; }`. `ChangeAccounts(sourceAccountId, destinationAccountId)` est remplacé par `ChangeType(type, sourceAccountId, destinationAccountId)` : valide la combinaison (type, comptes) **finale** en un seul appel plutôt qu'en deux étapes séparées (type puis comptes, ou l'inverse), ce qui aurait pu échouer transitoirement si les anciens comptes ne satisfont pas le nouveau type ou vice versa. Re-normalise aussi le signe du montant attendu pour le nouveau type (`NormalizeAmountSign` : `Math.Abs` puis re-signe selon le type — idempotent, donc sans risque d'ordre avec `UpdateExpectedAmount` appelé juste après dans le même passage).

**Bug latent trouvé en vérifiant le repository avant d'écrire le nouveau code, même silhouette qu'ADR-137** : le `UPDATE` SQL de `RecurringOperationRepository` n'a jamais inclus la colonne `type` — inoffensif tant que `Type` était immuable (rien n'essayait jamais de le persister), aurait silencieusement empêché tout changement de type de survivre à un redémarrage sinon. Corrigé dans le même passage, avec une assertion de non-régression dédiée dans `AppSmokeTest.cs` (relit l'opération après un changement de type et vérifie que `Type` a bien changé).

**Un seul vidage/régénération par sauvegarde, pas un par champ** : type, comptes, montant, fréquence, jour du mois, catégorie et tiers sont tous gravés dans chaque `ForecastOccurrence` déjà générée au moment de sa création (`ForecastOccurrenceGenerator` lit l'opération telle qu'elle est à cet instant) — un changement sur n'importe lequel de ces champs rend donc les occurrences encore `planned`/`missed` obsolètes, pas seulement un changement de compte comme ADR-137 l'avait d'abord isolé. Plutôt que d'exposer sept méthodes de service qui videraient chacune séparément (et régénéreraient donc plusieurs fois pour une seule sauvegarde), `RecurringOperationService.UpdateOperation` (remplace `ChangeAccounts`/`UpdateExpectedAmount` côté service) applique toutes les mutations Domain d'abord, persiste, puis ne vide qu'une seule fois.

**Refactorisation côté UI** : `RecurringOperationsController.SubmitCreate`/`SubmitEdit` partageaient déjà presque exactement la même lecture/validation de champs (type, montant, fréquence, jour du mois, comptes, catégorie, tiers) — extraite en `TryGatherOperationFields`, appelée par les deux. Les lignes « lecture seule » Type/Fréquence/Catégorie/Tiers (`form-type-readonly-row`, etc.) ne sont plus jamais affichées une fois tous ces champs éditables des deux côtés (création et édition) — supprimées du C# et de l'UXML plutôt que laissées mortes, conformément à la convention du projet de ne pas garder de code silencieusement inutilisé. Seule la ligne de lecture seule de la date de début reste, encore réellement utilisée en édition.

**Conséquence négative assumée, pas corrigée dans ce passage** : `UpdateSkipWarning` (l'avertissement de saut de premier cycle, ADR-120) reste explicitement limité à la création (`if (_editingOperationId is not null) return;`) — l'étendre à l'édition serait un vrai gain maintenant que la fréquence/le jour du mois y sont éditables aussi (le même piège qu'ADR-120 avait trouvé en création reste possible en édition), mais n'a pas été demandé et aurait élargi encore le périmètre de ce passage. À revoir si un incident similaire se reproduit en édition.

**Documents concernés** : `07-Interface.md` §3/§9/§10, `Assets/Scripts/UI/README.md`.

---

# 43. ADR-141 — Verrouillage des saisies numériques ; datepicker reporté

**Contexte** : troisième retour terrain du même jour — les champs montant/entier (`TextField` partout, jamais de contrôle numérique natif Unity à cause du formatage `MoneyFormat` déjà en place) acceptent n'importe quel caractère tapé, lettres comprises, jusqu'à l'échec de parsing à la soumission. Demande explicite couplée à un datepicker pour les champs date — scindée en deux, l'utilisateur ayant lui-même invité à reporter ce qui serait trop conséquent pour la session du jour.

**Décision** : traiter les deux demandes séparément plutôt que comme un seul lot.
1. **Verrouillage numérique — fait maintenant.** `NumericInputFilter` (nouveau, `FinanceOS.UI`) — `RestrictToInteger(field, allowNegative)`/`RestrictToDecimal(field, allowNegative)`, posés sur les 15 `TextField` numériques de l'app (montants de création/édition, filtres min/max, soldes de compte, seuils et horizon des Paramètres, jour du mois).
2. **Datepicker — reporté.** Aucun contrôle calendrier natif à réutiliser dans UI Toolkit runtime (contrairement aux champs numériques) ; un composant popup complet (grille de mois, navigation, positionnement, fermeture au clic extérieur) est une ampleur de travail comparable à un des graphiques `Painter2D` déjà construits — jugé disproportionné pour la même session que la fonctionnalité ci-dessus et l'extension de l'édition des opérations récurrentes (ADR-140). Les champs date restent des `TextField` au format jj/mm/aaaa (`DateFormat.TryParseInput`), non touchés par ce passage.

**Mécanisme choisi, pas une interception de touche** : `NumericInputFilter` corrige la valeur à chaque `RegisterValueChangedCallback` (revert vers une version assainie via `SetValueWithoutNotify`) plutôt que d'intercepter les `KeyDownEvent` caractère par caractère — aucune API bas niveau fiable pour bloquer une frappe avant insertion n'est documentée de façon certaine pour UI Toolkit dans cet environnement, alors que la correction post-frappe est le patron portable et documenté (Editor comme Runtime). En pratique, la lettre apparaît et disparaît en un seul cycle de rafraîchissement — fonctionnellement indistinguable d'un blocage pour un usage réel, sans parier sur une API non confirmée.

**Séparation entier/décimal, plus un drapeau `allowNegative`** : `RestrictToInteger` (jour du mois, horizon de prévision, seuil « Manquée ») n'autorise que des chiffres (+ signe `-` en tête si `allowNegative`) ; `RestrictToDecimal` (tous les montants) autorise en plus un unique séparateur décimal, `,` ou `.` — exactement ce qu'accepte déjà `MoneyFormat.TryParseEurosToMinor`, pas une règle inventée séparément. `allowNegative` n'est activé que pour les deux champs de solde de compte (`AccountsController._balanceField`/`_balanceHistoryAmountField`, `OnboardingController._accountBalanceField`) — un compte dette/crédit a un solde légitimement négatif ; tous les autres montants de l'app (opérations, transactions, allocations budgétaires, filtres, simulation) sont vérifiés comme des magnitudes positives à la soumission (`magnitude <= 0` rejeté), donc le signe négatif y est bloqué à la saisie même, pas seulement à la validation.

**Ce qui reste vérifiable en batchmode, et ce qui ne l'est pas** : la logique de nettoyage elle-même (`NumericInputFilter.Sanitize`, rendue `public` uniquement pour ça) est testée directement dans `UISmokeTest.cs` avec des cas positifs et négatifs (lettres retirées, deuxième séparateur décimal ignoré, espaces de groupement retirés, signe négatif accepté/rejeté selon la position et le drapeau). Le comportement réel au clavier — la frappe elle-même — ne peut pas être simulé ici (même limitation que tout le reste de l'interaction dans ce projet, ADR-113) ; à confirmer par l'utilisateur en usage réel.

**Documents concernés** : `07-Interface.md`, `Assets/Scripts/UI/README.md`.

---

# 44. ADR-142 — Corrections d'usage réel post-v1.0.3 : onboarding, boutons, espacement, scrollbar

**Contexte** : premier vrai lot de retours utilisateur (l'utilisateur + des testeurs externes) sur v1.0.3, plusieurs points distincts regroupés en une seule session de corrections. Deux décisions de périmètre posées explicitement à l'utilisateur avant de commencer : la popup de changelog post-mise-à-jour (+ accès à la demande) est la fonctionnalité la plus conséquente du lot (nouveau système : stockage de la dernière version vue, source du contenu, nouvelle UI modale) — reportée à une prochaine mise à jour, même logique que le datepicker (ADR-141). Le reste est traité le jour même.

**1. Correction d'un compte/opération pendant l'onboarding.** Avant ce correctif, un compte ou une opération ajouté pendant l'onboarding (`OnboardingController.AddAccount`/`AddOperation`) n'avait strictement aucune affordance de retrait — `BuildAccountRow`/`BuildOperationRow` ne rendaient que du texte, sans bouton, jusqu'à ce correctif. Deux traitements différents, pas un seul, parce que `AccountService` n'a **aucun** chemin de suppression définitive nulle part dans l'application (seulement `Archive`/`Restore`) : retirer une opération utilise `RecurringOperationService.Delete` (suppression réelle, sûre ici puisqu'aucune occurrence prévisionnelle n'est jamais générée pendant l'onboarding lui-même) ; retirer un compte utilise `Archive` — une vraie suppression aurait été dangereuse, puisqu'un utilisateur peut revenir en arrière depuis l'étape 3 (« Précédent ») vers l'étape 1 après avoir déjà créé une opération référençant ce compte, ce qui aurait laissé une référence orpheline. `OnboardingViewModelBuilder` est passé de `ListAll()` à `ListActive()` pour les comptes, précisément pour qu'un compte archivé disparaisse de toutes les listes de l'onboarding (liste de l'étape 1, menu déroulant compte de l'étape 2, récapitulatif) — indistinguable d'une vraie suppression du point de vue de ce flux. Les lignes du récapitulatif (étape 4) restent volontairement en lecture seule : cette étape est une relecture finale avant « Terminer », pas un nouvel endroit pour éditer — une erreur repérée là renvoie l'utilisateur en arrière via « Précédent ».

**2. Séparation visuelle Ajouter/Valider dans l'onboarding.** Root cause identifiée avant de corriger, pas devinée : le bouton d'ajout (« Ajouter ce compte »/« Ajouter ») et la navigation (« Précédent »/« Suivant ») sont séparés par la liste des éléments déjà ajoutés — mais cette liste est **vide** la toute première fois, exactement le moment où la confusion se produit (l'utilisateur remplit le formulaire, voit deux rangées de boutons collées l'une à l'autre sans aucun élément de liste entre les deux pour créer un vrai espace visuel). Nouvelle classe `.onboarding-nav-actions` (bordure supérieure + marge de 24px) appliquée uniquement à la rangée de navigation des étapes 2 et 3, indépendamment du contenu de la liste. Les boutons d'ajout gagnent aussi le préfixe « + » déjà utilisé partout ailleurs pour une action d'ajout (« + Nouvelle transaction », etc.) — cohérence en plus de la lisibilité.

**3. Bouton "Nouveau X" dupliqué près des listes.** Retour direct des testeurs : le bouton primaire en haut de page (`new-account-button`, etc.) était vu mais pas réflexe — les utilisateurs le cherchaient plutôt près de la liste elle-même. Même bouton, même gestionnaire de clic, dupliqué en style secondaire dans l'en-tête de la carte de liste (Comptes, Transactions, Opérations récurrentes, Catégories) — pas une nouvelle action, juste un second point d'entrée vers `OpenCreateForm`. Là où le bouton du haut se désactive sans compte existant (Transactions, Opérations récurrentes), le nouveau bouton suit exactement le même état (`SetEnabled` appliqué aux deux ensemble) pour ne jamais proposer deux affordances incohérentes entre elles.

**4. Espacement des cartes uniformisé.** Root cause : `.card` n'avait jamais eu de `margin-bottom` du tout — seul `.kpi-row` en avait un, donnant l'illusion d'un espacement voulu juste sous les soldes du Tableau de bord alors que toutes les cartes suivantes (courbe de trésorerie, répartition des dépenses, opérations à vérifier, etc.) se touchaient sans aucun écart. Corrigé une seule fois, dans `.card` lui-même plutôt qu'écran par écran, puisque les sept écrans partagent la même classe (ADR-117) — corrige donc l'app entière en un seul changement, pas seulement le Tableau de bord signalé dans le retour. `.form-card` (les cartes de formulaire créer/éditer) garde sa propre valeur via l'ordre de déclaration dans la feuille de style, sans conflit.

**5. Équilibre gauche/droite des `ScrollView`.** Cause réelle, pas supposée : le padding de `.page` est posé sur le `ScrollView` lui-même, qui contient à la fois la zone de contenu **et** la scrollbar verticale comme éléments frères dans la même boîte paddée — la scrollbar occupe donc de la largeur à l'intérieur même du padding droit, faisant lire cet écart comme plus large que le padding gauche (28px). Corrigé par une approximation délibérée (padding droit réduit à 16px, scrollbar amincie à 10px, cf. point 6) plutôt qu'une restructuration de chaque UXML avec un conteneur interne dédié — plus risqué pour un gain marginal sur ce lot. À affiner avec une vraie capture d'écran, même réserve que tout changement visuel de ce projet.

**6. Scrollbar plus discrète et thémée.** Noms de classes USS confirmés via les pages manuel officielles de `ScrollView`/`Scroller`/`Slider` (pas devinés) — même discipline qu'ADR-116 : `.unity-scroll-view__vertical-scroller` (le conteneur), `.unity-scroller__low-button`/`--high-button` (les boutons fléchés aux extrémités, masqués), `.unity-base-slider__tracker` (la piste, rendue transparente) et `.unity-base-slider__dragger` (le curseur, recoloré avec `--color-border`/`--color-ink-400` au survol). Toutes les règles sont scopées sous `.unity-scroll-view__vertical-scroller` plutôt qu'appliquées aux classes Unity génériques directement, au cas où un `Slider` autre qu'un scroller serait ajouté un jour.

**Ce qui reste hors périmètre de ce passage, documenté plutôt que silencieux** : la popup de changelog post-mise-à-jour et l'accès au changelog à la demande (reportés, voir contexte ci-dessus). Le rendu réel des points 2, 3, 4, 5 et 6 ne peut pas être confirmé depuis cet environnement (même limitation ADR-112 que tout changement visuel de ce projet) — seule la structure/logique est vérifiée en batchmode (`UISmokeTest.cs` : retrait d'un compte/d'une opération, existence et parité d'état des boutons dupliqués).

**Documents concernés** : `07-Interface.md` §4, `Assets/Scripts/UI/README.md`.

---

# 45. ADR-143 — Comparaison avant/après dans la simulation « Et si ? »

**Contexte** : l'utilisateur a demandé une explication du fonctionnement de la section Simulation de l'écran Prévisions pour vérifier sa cohérence avec ce qui était prévu. En relisant `06-Moteur_de_prevision.md` §9 (« comparaison avant/après sur le solde de fin de période et le point bas ») face au code réel, un vrai écart a été trouvé, pas supposé : `ForecastsController.RunSimulation` n'appelait que `ForecastService.Simulate` (le scénario « après ») et n'affichait jamais de chiffre « avant » à côté — la comparaison n'existait que si l'utilisateur remontait lui-même aux KPI de synthèse déjà affichés plus haut sur le même écran, pas comme un résultat de simulation autonome. Signalé explicitement avant de choisir : corriger le code pour respecter la doc, ou corriger la doc pour refléter le code. L'utilisateur a choisi le premier.

**Décision** : `RunSimulation` appelle désormais aussi `ForecastService.GetForecast` (le calcul « avant », sans le mouvement simulé) sur exactement la même fenêtre que `Simulate` (aujourd'hui → aujourd'hui + horizon de prévision réglé dans Paramètres), recalculé à chaque clic sur « Simuler » plutôt que réutilisé depuis le dernier rendu de l'écran — garantit que la base de comparaison reste correcte même si le compte sélectionné ou les réglages ont changé depuis le chargement de la page, au prix d'un second appel en mémoire, sans coût réel puisque rien n'est persisté ni l'un ni l'autre. Chaque carte KPI du résultat de simulation gagne une ligne `.kpi-context` supplémentaire (« Avant : *montant* (écart : ±*montant*) ») sous la valeur « après » déjà affichée — réutilise le patron déjà établi (grande valeur + contexte en petit) plutôt que d'inventer une nouvelle disposition.

**Couverture de test** : `AppSmokeTest.cs` avait déjà un scénario `GetForecast`/`Simulate` sur le même compte et la même fenêtre (achat simulé de 850,00 €) — une seule assertion ajoutée confirme que l'écart entre les deux égale exactement le montant simulé, ce qui valide directement le calcul que les nouvelles étiquettes affichent. `RunSimulation` reste privée et non testée par clic, cohérent avec le choix déjà établi pour cet écran (ADR-133) — seule la logique pure (`ForecastService`) est vérifiée.

**Documents concernés** : `06-Moteur_de_prevision.md` §9 (aucun changement — le code s'aligne maintenant sur ce texte), `Assets/Scripts/UI/README.md`.
