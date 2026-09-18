using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One version's worth of changelog highlights, already in French — see
    /// <see cref="ChangelogEntries"/>.</summary>
    public sealed record ChangelogEntry(string Version, IReadOnlyList<string> Highlights);

    /// <summary>
    /// The app's own changelog, embedded rather than fetched (ADR-150) — no extra network call
    /// beyond the update check already documented as the one exception to "jamais automatique,
    /// jamais silencieux" (ADR-139), and no risk of the in-app text drifting from what actually
    /// shipped. Condensed from the same French text already published on each GitHub release —
    /// kept in sync by hand, one new entry per version. Ordered newest first, matching how it's
    /// displayed.
    /// </summary>
    public static class ChangelogEntries
    {
        public static readonly IReadOnlyList<ChangelogEntry> All = new[]
        {
            new ChangelogEntry("1.0.9", new[]
            {
                "Les filtres de date sur Transactions passent au calendrier, avec un bouton pour les effacer.",
                "L'avertissement de saut de cycle (jour du mois / date de début) s'affiche aussi en modification d'une opération récurrente, pas seulement à la création.",
                "Les nouveautés de chaque mise à jour sont désormais consultables ici même.",
            }),
            new ChangelogEntry("1.0.8", new[]
            {
                "Confirmer une opération à vérifier tient en un clic, avec la date et le montant prévus — \"Modifier\" reste disponible pour les ajuster avant de confirmer.",
            }),
            new ChangelogEntry("1.0.7", new[]
            {
                "Reste à vivre recalculé : net des opérations récurrentes du mois moins le budget total du mois, toutes catégories confondues — disponible même sans budget créé.",
            }),
            new ChangelogEntry("1.0.6", new[]
            {
                "Correction définitive du texte invisible des cases à cocher en mode sombre.",
                "La date de début d'une opération récurrente est modifiable après sa création.",
                "Onboarding : date de départ et catégorie disponibles pour une opération ajoutée.",
            }),
            new ChangelogEntry("1.0.5", new[]
            {
                "Calendrier (sélecteur de date) sur l'historique de solde, la confirmation d'une opération, les occurrences ponctuelles, la simulation « Et si ? » et la création d'une transaction.",
            }),
            new ChangelogEntry("1.0.4", new[]
            {
                "Onboarding : un compte ou une opération ajouté par erreur peut être retiré avant de terminer.",
                "Onboarding : séparation visuelle claire entre « Ajouter » et « Suivant / Valider ».",
                "Bouton « Nouveau X » disponible aussi près de chaque liste, pas seulement en haut de page.",
                "Espacement uniformisé entre les cartes, scrollbar plus fine et thémée.",
                "La simulation « Et si ? » affiche un vrai avant/après (solde et point bas).",
                "Les sélecteurs de compte s'ouvrent présélectionnés sur le compte par défaut réglé dans Paramètres.",
            }),
            new ChangelogEntry("1.0.3", new[]
            {
                "Correction : texte invisible en mode sombre sur la case « Copier les montants du mois précédent ».",
                "Correction : sélecteurs Compte/Catégorie tronqués sur le filtre Transactions.",
                "L'édition d'une opération récurrente couvre désormais tous les champs (type, fréquence, jour du mois, catégorie, tiers).",
                "Verrouillage des saisies numériques : les champs montant/entier n'acceptent plus de lettres.",
            }),
            new ChangelogEntry("1.0.2", new[]
            {
                "Numéro de version affiché dans Paramètres.",
                "Vérification automatique de mise à jour au lancement — installation toujours soumise à confirmation.",
                "Mode fenêtré redimensionnable et bouton pour quitter proprement l'application.",
                "Correction : le compte d'une opération récurrente peut être modifié après création.",
            }),
            new ChangelogEntry("1.0.1", new[]
            {
                "Première diffusion publique.",
            }),
        };

        /// <summary>Entries strictly newer than <paramref name="lastSeenVersion"/> (ordered newest
        /// first, so this is everything before its match in <see cref="All"/>) — what an update
        /// popup should actually show, rather than the whole history every time. Null (never
        /// recorded, e.g. upgrading from a version that predates this feature) or a version this
        /// list doesn't recognize both fall back to the full list — there is no reliable way to
        /// know what such a user has already seen, so showing everything once is the safer
        /// default over showing nothing.</summary>
        public static IReadOnlyList<ChangelogEntry> Since(string? lastSeenVersion)
        {
            if (string.IsNullOrEmpty(lastSeenVersion))
            {
                return All;
            }

            var result = new List<ChangelogEntry>();
            foreach (var entry in All)
            {
                if (entry.Version == lastSeenVersion)
                {
                    return result;
                }

                result.Add(entry);
            }

            return All;
        }
    }
}
