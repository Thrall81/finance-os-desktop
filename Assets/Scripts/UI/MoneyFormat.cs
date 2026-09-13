using System;
using System.Text;

namespace FinanceOS.UI
{
    /// <summary>
    /// Centralizes every conversion from a minor-unit amount to display text — the only place
    /// dividing by 100. Deliberately does not use CultureInfo/NumberFormatInfo: two BCL gaps
    /// already found in this project (DateOnly, IsExternalInit) make it worth not gambling that
    /// Unity's runtime ships full "fr-FR" culture data. See docs/07-Interface.md §12.
    /// </summary>
    public static class MoneyFormat
    {
        private const char ThousandsSeparator = ' '; // non-breaking space, as in "1 234,56 €"
        private const char MinusSign = '−'; // real minus sign, not a hyphen

        public static string Format(long amountMinor, string currency = "EUR", bool forceSign = false)
        {
            var isNegative = amountMinor < 0;
            var magnitude = Math.Abs(amountMinor);
            var integerPart = magnitude / 100;
            var centsPart = magnitude % 100;

            var sign = isNegative ? MinusSign.ToString() : (forceSign ? "+" : string.Empty);
            return $"{sign}{GroupThousands(integerPart)},{centsPart:D2} {SymbolFor(currency)}";
        }

        private static string GroupThousands(long value)
        {
            var digits = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (digits.Length <= 3)
            {
                return digits;
            }

            var builder = new StringBuilder(digits.Length + digits.Length / 3);
            var firstGroupLength = digits.Length % 3;
            if (firstGroupLength == 0)
            {
                firstGroupLength = 3;
            }

            builder.Append(digits, 0, firstGroupLength);
            for (var i = firstGroupLength; i < digits.Length; i += 3)
            {
                builder.Append(ThousandsSeparator);
                builder.Append(digits, i, 3);
            }

            return builder.ToString();
        }

        private static string SymbolFor(string currency) => currency switch
        {
            "EUR" => "€",
            _ => currency,
        };

        /// <summary>Parses a user-typed euro amount ("1 234,56", "1234.56", "-12") into minor
        /// units. Accepts both ',' and '.' as the decimal separator since French keyboards type
        /// a comma but users may paste a dot. Uses InvariantCulture, not CurrentCulture — unlike
        /// full "fr-FR" culture data, the invariant culture ships everywhere, so this doesn't risk
        /// the kind of runtime gap ADR-110/111 already found twice.</summary>
        public static bool TryParseEurosToMinor(string text, out long minor)
        {
            minor = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var cleaned = text.Trim()
                .Replace(ThousandsSeparator.ToString(), string.Empty)
                .Replace(" ", string.Empty)
                .Replace(',', '.');

            if (!double.TryParse(
                    cleaned,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var euros))
            {
                return false;
            }

            minor = (long)Math.Round(euros * 100, MidpointRounding.AwayFromZero);
            return true;
        }
    }
}
