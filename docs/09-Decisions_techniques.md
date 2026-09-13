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
