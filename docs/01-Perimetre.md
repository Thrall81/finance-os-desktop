# Périmètre

# Finance OS Desktop — Version 1

---

# 1. Objectif du document

Définir le périmètre exact de la première version distribuable de l'application, dans le même esprit que `12-MVP.md` de l'ancien projet : éviter l'élargissement incontrôlé, la sur-architecture, et toute dépendance externe.

La V1 doit répondre à la question :

> Combien puis-je encore dépenser ce mois-ci sans compromettre mes prélèvements et mon solde minimum ?

---

# 2. Fonctionnalités incluses en V1

## 2.1 Comptes

- création, modification, archivage, restauration d'un compte ;
- types : courant, épargne, espèces, investissement (valorisation globale simple), dette/crédit ;
- solde initial + historique de soldes officiels saisis manuellement ;
- politique de liquidité (`immediate` / `reserve` / `excluded`) pour distinguer le disponible immédiat du reste.

## 2.2 Catégories

- catégories et sous-catégories (deux niveaux maximum) ;
- catégories système fournies par défaut, modifiables et complétables ;
- types : dépense, revenu, épargne, virement interne.

## 2.3 Tiers (facultatif, simplifié)

- un tiers optionnel associable à une transaction ou une opération récurrente (nom libre, pas de fiche complexe).

## 2.4 Transactions

- création, modification, suppression manuelles ;
- import CSV local (fichier bancaire exporté manuellement par l'utilisateur) avec aperçu, mapping de colonnes et dédoublonnage ;
- recherche, filtres (compte, période, catégorie, montant, texte) ;
- détection assistée des virements internes (deux transactions de montants opposés, dates proches, comptes différents → proposition, jamais automatique) ;
- rapprochement facultatif entre une transaction réelle et une occurrence prévisionnelle.

## 2.5 Opérations récurrentes et occurrences prévisionnelles

- opérations récurrentes (fréquence, montant fixe ou estimé, compte, catégorie) ;
- génération idempotente des occurrences futures ;
- occurrences ponctuelles (dépense ou revenu exceptionnel prévu, sans récurrence).

## 2.6 Moteur de prévision

- projection quotidienne de trésorerie sur un horizon configurable (90 jours par défaut) ;
- solde de fin de mois, point bas et sa date ;
- séparation visuelle stricte entre réel et prévu ;
- simulation ponctuelle non persistante (« et si j'achète ceci ? »).

## 2.7 Budget mensuel

- allocation par catégorie, copie du mois précédent ;
- suivi prévu / réel / engagé / restant ;
- reste à vivre et taux d'épargne du mois.

## 2.8 Tableau de bord

- solde disponible, solde prévisionnel de fin de mois, point bas, reste à vivre ;
- graphique de trésorerie (réel vs prévu) ;
- prochaines opérations, synthèse budgétaire, alertes calculées à l'affichage (pas de persistance d'alertes en V1).

## 2.9 Paramètres

- devise (EUR fixe en V1), horizon de prévision, seuil de solde faible ;
- emplacement du fichier de données, export/sauvegarde manuelle (copie du fichier SQLite ou export JSON).

---

# 3. Fonctionnalités explicitement exclues de la V1

| Fonctionnalité | Raison |
|---|---|
| Connecteur Gmail | Connecteur externe — hors périmètre définitif du projet, pas seulement différé |
| Open Banking / synchronisation bancaire | Connecteur externe — idem |
| Dossier surveillé | Suppose une automatisation en arrière-plan sur le système de fichiers, inutile sans connecteur en amont |
| Documents (factures, bulletins de salaire, PDF, OCR) | Complexité disproportionnée pour une V1 locale ; réévaluable plus tard comme module purement local |
| Règles financières personnelles, objectifs d'épargne datés, comparaison de scénarios, cartes de décision | Fonctionnalités avancées de l'ancien projet (epic DECISION) — repoussées après validation du socle |
| Authentification, compte utilisateur, session | Application mono-utilisateur locale ; un verrou local optionnel (code PIN) peut être ajouté plus tard, jamais un compte en ligne |
| Multi-devise consolidée | EUR uniquement en V1 |
| Multi-utilisateur, multi-appareil, synchronisation | Hors périmètre du projet, pas seulement de la V1 |
| macOS, Linux, mobile | Windows uniquement en V1 (cf. `04-Stack_technique.md`) |
| Mise à jour automatique | Toute mise à jour est un téléchargement manuel d'une nouvelle version |
| Télémétrie, statistiques d'usage | Aucune remontée réseau, jamais |

---

# 4. Parcours principal

```text
Premier lancement
    │
    ▼
Création des comptes et saisie des soldes
    │
    ▼
Création des catégories (préremplies, modifiables)
    │
    ▼
Saisie ou import CSV des transactions
    │
    ▼
Création des opérations récurrentes
    │
    ▼
Génération des occurrences et calcul de la prévision
    │
    ▼
Création du budget mensuel
    │
    ▼
Consultation du tableau de bord
```

---

# 5. Feuille de route indicative

| Phase | Contenu | Critère de sortie |
|---|---|---|
| Phase 0 — Socle technique | Projet Unity, UI Toolkit, SQLite embarqué, fenêtre principale, navigation entre écrans | L'application démarre, lit/écrit un fichier `.db` local |
| Phase 1 — Comptes et transactions | Comptes, catégories, transactions manuelles, listes et filtres | Remplace un tableau Excel simple |
| Phase 2 — Prévisionnel | Opérations récurrentes, occurrences, moteur de prévision, graphique de trésorerie | Connaître son solde prévu à une date future |
| Phase 3 — Budget et tableau de bord | Budgets, reste à vivre, taux d'épargne, dashboard complet, graphiques restants | Décider si une dépense est compatible avec le mois |
| Phase 4 — Import CSV | Prévisualisation, mapping, dédoublonnage, historique des imports | Réimporter un relevé sans créer de doublon |
| Phase 5 — Finitions | Export/sauvegarde, verrou local optionnel, packaging installeur | Version distribuable en téléchargement |

Les phases suivantes (règles financières, objectifs, scénarios) ne sont pas planifiées avant une utilisation réelle de plusieurs semaines de cette V1, dans le même esprit que `MVP-010` de l'ancien projet.

---

# 6. Critères de réussite

La V1 est un succès lorsque l'utilisateur peut dire :

```text
Je n'ai plus besoin de recalculer mentalement ce qu'il me restera après mes prélèvements.
Je peux voir à l'avance le moment où mon compte sera le plus bas.
Je peux définir un budget et savoir ce qu'il me reste réellement.
L'application tient dans un exécutable, ne demande rien sur le réseau, et mes données restent chez moi.
```
