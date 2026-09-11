# Interface

# Finance OS Desktop

---

# 1. Objectif

Adapter les principes d'interface de l'ancien `09-Frontend.md` à une application desktop Unity/UI Toolkit mono-fenêtre, mono-utilisateur, sans navigateur.

Principes inchangés : priorité aux informations essentielles, distinction stricte réel/prévu, jamais d'information transmise uniquement par la couleur, formulaires simples, retour immédiat après chaque action.

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
Tableau de bord
Comptes (liste, création, modification, détail)
Transactions (liste, création, modification, import CSV)
Opérations récurrentes (liste, création, modification)
Prévisions (synthèse, timeline, liste des occurrences, simulation)
Budget mensuel
Paramètres (devise, horizon, seuil, emplacement des données, export)
```

Pas de page de connexion : l'application s'ouvre directement sur le tableau de bord (ou l'écran d'accueil de première utilisation s'il n'existe encore aucun compte).

---

# 4. Tableau de bord

Cartes reprises de l'ancien projet : Solde disponible, Solde prévu en fin de mois, Point bas prévisionnel, Reste à vivre — puis graphique de trésorerie, prochaines opérations, synthèse budgétaire, alertes calculées à l'affichage (solde faible, dépassement de budget).

---

# 5. Graphiques — rendu maison UI Toolkit

## 5.1 Décision

Tous les graphiques sont des `VisualElement` personnalisés qui redéfinissent `generateVisualContent` et dessinent avec `Painter2D` (cf. `13-Decisions_techniques.md`, ADR-103). Pas de bibliothèque tierce.

## 5.2 Types nécessaires en V1

| Graphique | Type | Écran |
|---|---|---|
| Courbe de trésorerie | Ligne, avec segment plein (réel) puis pointillé (prévu), marqueur du point bas | Tableau de bord, Prévisions |
| Budget prévu/réel/engagé | Barres groupées par catégorie | Budget |
| Répartition des dépenses | Anneau (donut) | Tableau de bord |
| Évolution de l'épargne | Ligne ou barres | Budget |

## 5.3 Composant `LineChartElement` (exemple)

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

## 5.4 Exigences conservées de l'ancien projet

- chaque graphique possède un titre et, si utile, une légende ;
- une infobulle apparaît au survol d'un point/d'une barre (valeur exacte, date, détail) ;
- une alternative textuelle existe toujours à côté du graphique (les valeurs principales restent lisibles sans lui) ;
- aucune information n'est transmise uniquement par la couleur (le trait plein/pointillé porte déjà la distinction réel/prévu) ;
- comportement correct géré pour l'absence de données.

---

# 6. Formulaires

Reactive-style : validation locale immédiate (UI Toolkit `INotifyValueChanged` + validation C#), bouton de soumission désactivé pendant le traitement, erreurs affichées près du champ concerné. Montants saisis par l'utilisateur en euros, convertis en centimes avant d'atteindre la couche `App`.

---

# 7. Tableaux

`MultiColumnListView` pour les transactions et occurrences : tri par colonne, défilement virtualisé (nécessaire dès plusieurs années d'historique), sélection, navigation clavier.

---

# 8. États

Chaque écran prévoit : chargement, succès, aucune donnée (état vide explicatif avec action, jamais un simple « Aucune donnée »), erreur (avec message compréhensible — ici, une erreur signifie presque toujours un problème local disque/fichier, jamais un problème réseau).

---

# 9. Présentation des montants

Composant central `MoneyLabel` : reçoit un montant en centimes + une devise, affiche `1 234,56 €` / `−82,35 €` / `+2 100,00 €`. Aucune vue n'effectue de division par 100 directement.

---

# 10. Accessibilité

Toujours pertinente même sans obligation réglementaire externe : navigation clavier complète, focus visible, contrastes suffisants, labels associés aux champs. UI Toolkit fournit une bonne base native (focus ring, tabulation) à ne pas casser par un style personnalisé excessif.

---

# 11. Ce qui disparaît de l'ancien Frontend

Authentification, guards de route, interceptors HTTP, CSRF, CORS, gestion multi-onglets navigateur, responsive mobile/tablette (l'application cible uniquement une fenêtre desktop redimensionnable), internationalisation (français uniquement, pas anticipé pour l'instant).
