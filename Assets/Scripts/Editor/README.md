# Editor

Outillage éditeur uniquement (scripts de génération, utilitaires de build) — jamais inclus dans un build joueur.

Convention Unity standard : tout script placé dans un dossier `Editor/` (à n'importe quelle profondeur) en est automatiquement exclu.

`BuildScript.cs` (ADR-136) produit le joueur Standalone Windows IL2CPP x64 que `packaging/FinanceOS.iss` (Inno Setup, hors de `Assets/`, versionné séparément) transforme en installeur — `Unity.exe -batchmode -quit -projectPath . -executeMethod FinanceOS.EditorTools.BuildScript.BuildWindowsPlayer`, sortie dans `Build/Windows/` (gitignored). Le dossier `..._BackUpThisFolder_ButDontShipItWithYourGame` que produit tout build IL2CPP (symboles de debug natifs, souvent plus lourd que le reste du build réuni) est explicitement exclu du `[Files]` de l'installeur, jamais supprimé du dossier de build lui-même — son nom dit bien de le conserver pour soi, seulement de ne pas le distribuer.
