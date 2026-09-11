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
