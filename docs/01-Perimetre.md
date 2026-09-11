# Périmètre

# Finance OS Desktop — Version 1

---

# 1. Objectif du document

Définir le périmètre exact de la première version distribuable de l'application, dans le même esprit que `12-MVP.md` de l'ancien projet : éviter l'élargissement incontrôlé, la sur-architecture, et toute dépendance externe.

La V1 doit répondre à la question :

> Combien puis-je encore dépenser ce mois-ci sans compromettre mes prélèvements et mon solde minimum ?

---

# 2. Fonctionnalités incluses en V1

## 2.1 Premier lancement (onboarding)

Au tout premier démarrage (aucun compte en base), l'application n'ouvre pas directement le tableau de bord — elle affiche un parcours guidé court en 3 à 4 étapes :

1. créer le ou les premiers comptes et leur solde actuel ;
2. ajouter les charges et revenus récurrents principaux (salaire, loyer, une ou deux factures) — étape explicitement « passable » ;
3. récapitulatif, puis ouverture du tableau de bord, déjà utile grâce aux données saisies.

Ce parcours n'est pas un simple assistant décoratif : sans connecteur pour préremplir quoi que ce soit, une application qui ouvrirait directement sur un tableau de bord vide risque un abandon immédiat. Il peut être quitté à tout moment (les données déjà saisies sont conservées), et n'est jamais réaffiché une fois au moins un compte créé — cf. `07-Interface.md` §4.

## 2.2 Comptes

- création, modification, archivage, restauration d'un compte ;
- types : courant, épargne, espèces, investissement (valorisation globale simple), dette/crédit ;
- solde initial + historique de soldes officiels saisis manuellement ;
- politique de liquidité (`immediate` / `reserve` / `excluded`) pour distinguer le disponible immédiat du reste — une valeur par défaut sûre est appliquée automatiquement selon le type de compte choisi (ex. courant → immédiat, épargne → réserve, investissement/dette → exclu), modifiable ensuite sans obligation de comprendre le concept dès la création.

## 2.3 Catégories

- catégories et sous-catégories (deux niveaux maximum) ;
- catégories système fournies par défaut, modifiables et complétables ;
- types : dépense, revenu, épargne, virement interne.

## 2.4 Catégorisation assistée

- l'application mémorise, pour chaque libellé de transaction déjà catégorisé manuellement, la catégorie choisie ;
- à la saisie manuelle ou à l'import CSV d'une transaction dont le libellé (normalisé) a déjà été vu, la catégorie est **suggérée** automatiquement, jamais imposée silencieusement — un clic confirme ou change ;
- pas de moteur de règles complexes (opérateurs, priorités, expressions régulières) : une mémorisation simple « dernier libellé vu → dernière catégorie utilisée » suffit à l'objectif (réduire la saisie répétitive), sans la complexité de `CategorizationRule` de l'ancien projet.

## 2.5 Tiers (facultatif, simplifié)

- un tiers optionnel associable à une transaction ou une opération récurrente (nom libre, pas de fiche complexe).

## 2.6 Transactions

- création, modification, suppression manuelles ;
- import CSV local (fichier bancaire exporté manuellement par l'utilisateur) avec aperçu, mapping de colonnes et dédoublonnage ;
- recherche, filtres (compte, période, catégorie, montant, texte) ;
- détection assistée des virements internes (deux transactions de montants opposés, dates proches, comptes différents → proposition, jamais automatique) ;
- rapprochement facultatif entre une transaction réelle et une occurrence prévisionnelle (cf. §2.7, file de vérification).

## 2.7 Opérations récurrentes et occurrences prévisionnelles

- opérations récurrentes (fréquence, montant fixe ou estimé, compte, catégorie) ;
- génération idempotente des occurrences futures ;
- occurrences ponctuelles (dépense ou revenu exceptionnel prévu, sans récurrence) ;
- **file de vérification** : dès qu'une occurrence atteint ou dépasse sa date prévue sans avoir été rapprochée, elle rejoint une liste « Opérations à vérifier », visible depuis le tableau de bord et l'écran Prévisions. Un clic sur « C'est arrivé » ouvre un mini-formulaire préempli (date et montant modifiables) qui crée la transaction réelle et rapproche l'occurrence en une seule action — c'est la manière prévue de transformer le prévisionnel en réel sans ressaisie complète, cf. `07-Interface.md` §6.

## 2.8 Moteur de prévision

- projection quotidienne de trésorerie sur un horizon configurable (90 jours par défaut) ;
- solde de fin de mois, point bas et sa date ;
- séparation visuelle stricte entre réel et prévu ;
- simulation ponctuelle non persistante (« et si j'achète ceci ? »).

## 2.9 Budget mensuel

- allocation par catégorie, copie du mois précédent ;
- suivi prévu / réel / engagé / restant ;
- reste à vivre et taux d'épargne du mois.

## 2.10 Tableau de bord

- solde disponible, solde prévisionnel de fin de mois, point bas, reste à vivre ;
- graphique de trésorerie (réel vs prévu) ;
- prochaines opérations, file de vérification en attente, synthèse budgétaire, alertes calculées à l'affichage.

## 2.11 Paramètres

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
| Règles de catégorisation complexes (opérateurs, priorités, expressions régulières, application rétroactive en masse) | Remplacées par la mémorisation simple de §2.4, suffisante pour l'objectif ; le moteur complet de l'ancien projet reste hors périmètre |
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
Premier lancement (onboarding court, cf. §2.1)
    │
    ▼
Création des comptes et saisie des soldes
    │
    ▼
Création des catégories (préremplies, modifiables)
    │
    ▼
Saisie ou import CSV des transactions (catégorisation assistée)
    │
    ▼
Création des opérations récurrentes
    │
    ▼
Génération des occurrences et calcul de la prévision
    │
    ▼
Confirmation des échéances via la file de vérification, au fil de l'eau
    │
    ▼
Création du budget mensuel
    │
    ▼
Consultation régulière du tableau de bord
```

---

# 5. Feuille de route indicative

| Phase | Contenu | Critère de sortie |
|---|---|---|
| Phase 0 — Socle technique | Projet Unity, UI Toolkit, SQLite embarqué, fenêtre principale, navigation entre écrans | L'application démarre, lit/écrit un fichier `.db` local |
| Phase 1 — Comptes et transactions | Onboarding, comptes, catégories (+ catégorisation assistée), transactions manuelles, listes et filtres | Remplace un tableau Excel simple |
| Phase 2 — Prévisionnel | Opérations récurrentes, occurrences, file de vérification, moteur de prévision, graphique de trésorerie | Connaître son solde prévu à une date future, confirmer une échéance en un clic |
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
Confirmer qu'une échéance prévue a bien eu lieu me prend un clic, pas une nouvelle saisie complète.
Je peux définir un budget et savoir ce qu'il me reste réellement.
L'application tient dans un exécutable, ne demande rien sur le réseau, et mes données restent chez moi.
```
