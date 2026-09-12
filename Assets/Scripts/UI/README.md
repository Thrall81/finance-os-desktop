# UI

Contrôleurs qui lient les `VisualElement` (UI Toolkit) aux services applicatifs. Une page = un contrôleur. Aucune logique métier ici. Voir `docs/02-Architecture.md` §3.4 et `docs/07-Interface.md`.

`MoneyFormat`/`DateFormat` centralisent tout affichage de montant/date (jamais de `CultureInfo`, cf. leurs commentaires). `AppBootstrap` (MonoBehaviour) construit l'`AppContainer` au démarrage de la scène. Chaque écran suit le même patron : `XViewModel` (données déjà formatées) + `XViewModelBuilder` (pur C#, construit le view model depuis `AppContainer` — testable sans UI Toolkit) + `XController` (lie le view model aux `VisualElement` — testable sans rendu, cf. `09-Decisions_techniques.md` ADR-112). Seul `Dashboard` existe pour l'instant, sans le graphique de trésorerie (différé, même ADR).
