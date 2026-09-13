using System.Text;

namespace FinanceOS.App
{
    /// <summary>
    /// The single normalization function behind "catégorisation assistée" — every label ever
    /// stored in or looked up against `transaction_entry.normalized_label` goes through this, so
    /// matching stays consistent regardless of how a caller typed or cased the text.
    /// See docs/03-Modele_de_donnees.md §6bis and docs/07-Interface.md §7.
    /// </summary>
    public static class LabelNormalization
    {
        public static string Normalize(string label)
        {
            var trimmed = label.Trim().ToLowerInvariant();
            var builder = new StringBuilder(trimmed.Length);
            var lastWasSpace = false;

            foreach (var ch in trimmed)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasSpace)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    builder.Append(ch);
                    lastWasSpace = false;
                }
            }

            return builder.ToString();
        }
    }
}
