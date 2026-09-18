# Confidentialité et données

# Finance OS Desktop

---

# 1. Principe fondateur

L'application ne transmet **aucune donnée personnelle ou financière sur le réseau**, jamais. Ce n'est pas une politique de confidentialité déclarative : c'est une contrainte d'architecture vérifiable.

- une unique exception, délibérée et scopée précisément (ADR-139) : au lancement, l'application interroge en arrière-plan l'API publique et anonyme des releases GitHub du projet (`UnityWebRequest`, aucune clé, aucun identifiant) pour savoir si une version plus récente existe, et télécharge silencieusement l'installeur correspondant si c'est le cas. Aucune donnée personnelle ou financière ne transite jamais par cet appel — uniquement un numéro de version. **L'installation elle-même n'est en revanche jamais automatique** : rien ne s'exécute, aucun fichier de l'application en cours n'est remplacé, sans une confirmation explicite de l'utilisateur dans une fenêtre dédiée (« Voulez-vous l'installer maintenant ? ») ;
- en dehors de cette vérification de version, aucun autre appel `UnityWebRequest`/`HttpClient` n'est présent dans le code ;
- aucun SDK d'analytics, de crash-reporting ou de télémétrie n'est inclus dans le build ;
- le manifeste réseau du build Windows ne demande que l'accès nécessaire à cette unique vérification.

Conséquence directe : le RGPD ne s'applique pas au fonctionnement de l'application elle-même, puisqu'aucun traitement de données à caractère personnel n'a lieu en dehors de la machine de l'utilisateur, sous sa seule maîtrise.

---

# 2. Emplacement du fichier de données

## 2.1 Mode portable par défaut

Au démarrage, l'application cherche un dossier `data/` à côté de l'exécutable :

```text
FinanceOSDesktop.exe
data/
  financeos.db
```

S'il existe (ou si le dossier d'installation est inscriptible), l'application l'utilise et reste **portable** : copier le dossier suffit à déplacer toutes les données (clé USB, sauvegarde manuelle, autre machine).

## 2.2 Repli standard Windows

Si le dossier d'installation n'est pas inscriptible (ex. `Program Files`), l'application utilise :

```text
%APPDATA%\FinanceOSDesktop\financeos.db
```

Le choix effectif est affiché dans les Paramètres, avec un raccourci pour ouvrir le dossier dans l'explorateur.

---

# 3. Sauvegarde et export

- aucune sauvegarde automatique vers un service tiers, jamais ;
- bouton **Exporter mes données** dans les Paramètres : copie horodatée du fichier `.db` vers un emplacement choisi par l'utilisateur (boîte de dialogue Windows standard) ;
- un export JSON lisible pourra être ajouté en V2 si le besoin se confirme, mais n'est pas nécessaire tant que la copie du fichier SQLite suffit.

La responsabilité de la sauvegarde régulière appartient à l'utilisateur — l'application peut simplement le lui rappeler (ex. bandeau si le fichier n'a pas été exporté depuis longtemps), sans jamais agir à sa place sur le réseau.

---

# 4. Suppression des données

Un bouton **Réinitialiser l'application** dans les Paramètres supprime le fichier `.db` après confirmation forte (ressaisie d'un mot déclencheur). Aucune donnée résiduelle n'est censée rester ailleurs, puisqu'il n'existe ni cache serveur, ni journal distant.

---

# 5. Protection locale

Un verrou applicatif (code PIN ou mot de passe demandé à l'ouverture), un temps envisagé pour protéger l'accès si la machine est partagée, a été écarté définitivement (2026-09-18, ADR-149, `01-Perimetre.md` §3) — décision explicite de l'utilisateur, pas seulement différée.

Le chiffrement du fichier SQLite lui-même (ex. SQLCipher) n'a jamais été retenu, pour les mêmes raisons de complexité et de dépendances natives supplémentaires — sans objet une fois le verrou applicatif lui-même écarté.

---

# 6. Aucune porte d'entrée externe

L'import CSV, un temps envisagé comme seule voie d'entrée de données autre que la saisie manuelle, a été écarté définitivement (2026-09-18, ADR-149, `01-Perimetre.md` §3). La saisie manuelle est donc la seule voie d'entrée de données de l'application — aucun fichier externe n'est jamais lu.

---

# 7. Logs applicatifs

Les journaux Unity (`Logs/`) restent strictement locaux et ne doivent jamais contenir de montant, de libellé de transaction ou d'identifiant de compte — uniquement des informations techniques (erreur SQL, identifiant interne), comme le prévoyait déjà `06-Securite.md` de l'ancien projet pour les logs applicatifs.

---

# 8. Ce qui disparaît des exigences de l'ancien projet

Sans objet dans cette architecture, donc retiré de la documentation : authentification par session, cookies, CSRF, CORS, chiffrement de jetons OAuth, sauvegarde chiffrée externalisée, modèle de menace multi-utilisateur, politique de bêta fermée, en-têtes HTTP de sécurité, HTTPS. Tous ces sujets supposaient un serveur exposé à un réseau, qui n'existe plus.
