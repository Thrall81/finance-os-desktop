# Polices vendorisées

Fichiers statiques uniquement — jamais de police variable (`[wght].ttf`) : l'import direct d'une police variable dans Unity ne rend que son instance par défaut, sans moyen fiable de sélectionner un poids (Bold) sans passer par l'éditeur de Font Asset, non scriptable dans cet environnement (cf. `09-Decisions_techniques.md`, ADR-118). D'où le choix délibéré des dépôts sources statiques ci-dessous plutôt que le mirror `google/fonts`, qui a migré `ibmplexsans`/`ibmplexmono` vers des polices variables.

| Fichier | Source | Licence |
|---|---|---|
| `Spectral-Bold.ttf` | [google/fonts, ofl/spectral](https://github.com/google/fonts/tree/main/ofl/spectral) | SIL OFL 1.1 (`Spectral-OFL.txt`) |
| `IBMPlexSans-Regular.ttf`, `IBMPlexSans-Bold.ttf` | [IBM/plex, packages/plex-sans](https://github.com/IBM/plex/tree/master/packages/plex-sans/fonts/complete/ttf) | SIL OFL 1.1 (`IBMPlexSans-LICENSE.txt`) |
| `IBMPlexMono-Regular.ttf`, `IBMPlexMono-Bold.ttf` | [IBM/plex, packages/plex-mono](https://github.com/IBM/plex/tree/master/packages/plex-mono/fonts/complete/ttf) | SIL OFL 1.1 (`IBMPlexMono-LICENSE.txt`) |

Usage retenu (cf. `docs/07-Interface.md` §8bis et la maquette `docs/mockups/dashboard.html`) : Spectral pour la marque et les titres (page/carte), IBM Plex Sans pour le reste de l'interface, IBM Plex Mono pour tout affichage de montant ou de date (effet « registre comptable »). Chaque classe USS qui référence une police pointe le fichier de poids exact dont elle a besoin (`-unity-font-definition: url("../../Fonts/...")`) — jamais `-unity-font-style: bold` pour simuler un gras à partir d'un fichier Regular quand un vrai fichier Bold est disponible ici.

## Mise à jour

Pas de commande automatisée : retélécharger le(s) fichier(s) `.ttf` statique(s) depuis le dépôt source du tableau ci-dessus (jamais depuis `google/fonts` pour les familles IBM Plex, migrées en police variable), remplacer ici, mettre à jour ce tableau.
