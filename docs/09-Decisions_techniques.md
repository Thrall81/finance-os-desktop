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
5. **Titre de carte caché sous son propre tableau** (« Occurrences prévues » rendu sous la `MultiColumnListView` qui le suit dans le document) — première hypothèse (géométrie de liste obsolète après un cycle `display: none` → `Flex`, corrigée via `Rebuild()`) **infirmée par la capture d'écran suivante** : le titre restait caché après ce correctif. Cause retenue ensuite, cohérente avec les points 3 et 4 ci-dessus : `.card-header` n'avait qu'un `margin-bottom: 10px` sans hauteur minimale propre, insuffisant une fois les métriques réelles de la police en jeu — exactement le même schéma que `.kpi-card`/`.form-row`/`.page-subtitle` mal réemployée. Corrigé par `.card-header { min-height: 24px; margin-bottom: 20px; }` (10px → 20px). Le correctif `Rebuild()` reste en place (inoffensif, utile en soi pour éviter un autre type de données obsolètes) mais n'était probablement pas la vraie cause.

**Conséquences négatives** : les points 3 et 5 sont corrigés à partir d'indices et d'hypothèses raisonnables, pas d'un diagnostic certain — un inspecteur d'UI en direct aurait permis de confirmer la cause exacte en une itération au lieu de plusieurs allers-retours de capture d'écran (le point 5 a d'ailleurs nécessité deux tentatives, la première étant une fausse piste).

**Conditions de réévaluation** : mêmes conditions qu'ADR-112. Si un titre de carte reste caché après ce second correctif, abandonner l'hypothèse « marge insuffisante » et envisager de ne plus cacher les `MultiColumnListView` via `display: none` du tout (garder le contrôle visible en permanence, vide, et ne faire varier que le libellé d'état vide).

**Documents concernés** : aucun autre — plusieurs petits correctifs de code/USS, déjà répercutés dans `Assets/UI/USS/theme.uss` et les contrôleurs concernés.
