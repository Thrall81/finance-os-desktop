# Vision

# Finance OS Desktop

> Une application Windows locale, gratuite et téléchargeable, permettant de centraliser mes comptes, mes transactions et mes opérations récurrentes afin de disposer d'une vision claire et anticipée de ma situation financière — sans serveur, sans connecteur, sans transmission de la moindre donnée.

---

# Filiation avec l'ancien projet

Ce projet reprend la philosophie et une grande partie du modèle métier de `personal-finance-os` (Symfony/Angular/PostgreSQL), abandonné en tant que produit hébergé le 2026-09-12. Le moteur de prévision, le modèle de données et les principes de conception restent valides ; seule l'exécution technique change radicalement : plus de serveur, plus de base de données réseau, plus de connecteur.

L'ancien dépôt reste conservé tel quel à titre de référence et n'est plus développé.

---

# Pourquoi ce changement de cap

Les applications de gestion de budget grand public sont aujourd'hui majoritairement gratuites. Héberger un service web personnel (VPS, sauvegardes, supervision, conformité RGPD pour des bêta-testeurs externes) représente une charge d'exploitation disproportionnée par rapport à la valeur ajoutée d'une hébergement en ligne pour un outil personnel.

La solution retenue : une application qui tourne **entièrement sur la machine de l'utilisateur**, sans jamais rien envoyer sur le réseau.

---

# Philosophie

Elle ne change pas depuis l'ancien projet :

Finance OS Desktop n'a pas vocation à remplacer une banque ni à devenir un logiciel de comptabilité. C'est un **moteur d'aide à la décision financière personnelle**, qui compare des options et en expose les conséquences selon des règles explicites — sans jamais décider à la place de l'utilisateur, sans conseil financier, fiscal ou juridique, sans pourcentage de confiance inventé.

Le projet privilégie :

- la simplicité d'utilisation et d'installation (un exécutable, aucune dépendance externe) ;
- la transparence des calculs ;
- la maîtrise totale des données (elles ne quittent jamais la machine) ;
- la pérennité (aucune dépendance à un service tiers qui pourrait disparaître).

---

# Objectifs du logiciel

- centraliser les comptes financiers (courant, épargne, espèces, investissement, dette) ;
- suivre les dépenses et les revenus ;
- gérer un budget mensuel par catégorie ;
- calculer une trésorerie prévisionnelle quotidienne ;
- suivre les opérations récurrentes (salaire, loyer, abonnements...) ;
- distinguer le solde bancaire du montant réellement disponible ;
- produire un tableau de bord clair, avec des graphiques lisibles ;
- expliquer chaque calcul et chaque hypothèse.

---

# Principes fondamentaux

## Les données ne quittent jamais la machine

Toutes les données sont stockées dans un unique fichier SQLite local. Aucun appel réseau n'est effectué par l'application, jamais. Ce principe est une contrainte d'architecture documentée dans `08-Confidentialite_et_donnees.md`, pas une simple intention.

## Aucun connecteur

Pas de Gmail, pas d'Open Banking, pas de dossier surveillé, pas de service cloud. La saisie manuelle est la seule voie d'entrée de données — l'import CSV envisagé initialement a été écarté définitivement (2026-09-18, ADR-149, `01-Perimetre.md` §3).

## Le prévisionnel reste le cœur du projet

Connaître son solde aujourd'hui est utile. Connaître son solde dans trois semaines l'est davantage. Le moteur de prévision (`06-Moteur_de_prevision.md`) reste la fonctionnalité centrale de l'application.

## La simplicité avant tout

Application mono-utilisateur, mono-devise (EUR), mono-plateforme (Windows) pour cette première version. Aucune fonctionnalité n'est ajoutée sans besoin concret. Les abstractions inutiles sont évitées (voir `05-Conventions_de_code.md`).

---

# Ce que cette application n'est pas

- une banque, un logiciel de comptabilité d'entreprise ou de facturation ;
- un outil de trading ou de conseil en investissement ;
- une application multi-utilisateur, multi-appareil ou synchronisée ;
- un service en ligne : il n'y a ni compte, ni serveur, ni mise à jour automatique silencieuse.

---

# Vision à long terme

Une fois le socle (comptes, transactions, catégories, récurrences, prévisions, budget) validé par un usage quotidien réel, l'application pourra retrouver certaines ambitions de l'ancien projet — règles financières personnelles, objectifs d'épargne datés, comparaison de scénarios — mais toujours dans un exécutable local, sans jamais réintroduire de serveur ni de connecteur externe.
