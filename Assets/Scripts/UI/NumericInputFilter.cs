using System.Text;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Restricts a TextField meant for numeric input to only ever hold digits (plus an optional
    /// leading '-' and, for money fields, one decimal separator) — corrects on every keystroke by
    /// reverting to a sanitized value rather than intercepting raw key events, since that's the
    /// portable way to do this in UI Toolkit (no reliable low-level per-character interception API
    /// across Editor/Runtime here). Same as every other interactive UI behavior in this project,
    /// this cannot be visually/interactively confirmed from this environment — only that the
    /// sanitizing logic itself is correct (see UISmokeTest.cs). Public (not internal) specifically
    /// so UISmokeTest.cs, in the Editor assembly, can call Sanitize directly — this project has no
    /// [InternalsVisibleTo]. See docs/09-Decisions_techniques.md ADR-141.
    /// </summary>
    public static class NumericInputFilter
    {
        /// <summary>Digits only, plus a leading '-' if <paramref name="allowNegative"/>. For whole-
        /// number fields — day of month, forecast horizon, thresholds.</summary>
        public static void RestrictToInteger(TextField field, bool allowNegative = false) =>
            field.RegisterValueChangedCallback(evt => Enforce(field, evt.newValue, allowDecimalSeparator: false, allowNegative));

        /// <summary>Digits, plus one decimal separator (',' or '.', mirroring
        /// MoneyFormat.TryParseEurosToMinor's acceptance of both) and a leading '-' if
        /// <paramref name="allowNegative"/>. For money fields.</summary>
        public static void RestrictToDecimal(TextField field, bool allowNegative = false) =>
            field.RegisterValueChangedCallback(evt => Enforce(field, evt.newValue, allowDecimalSeparator: true, allowNegative));

        private static void Enforce(TextField field, string value, bool allowDecimalSeparator, bool allowNegative)
        {
            var sanitized = Sanitize(value, allowDecimalSeparator, allowNegative);
            if (sanitized != value)
            {
                field.SetValueWithoutNotify(sanitized);
            }
        }

        /// <summary>Public only so UISmokeTest.cs can exercise the sanitizing logic directly — the
        /// actual keystroke it would normally run from can't be simulated in batchmode (same
        /// "no live panel, no real input events" limitation as every other interaction in this
        /// project, e.g. ADR-113).</summary>
        public static string Sanitize(string value, bool allowDecimalSeparator, bool allowNegative)
        {
            var result = new StringBuilder(value.Length);
            var seenSeparator = false;

            foreach (var c in value)
            {
                if (char.IsDigit(c))
                {
                    result.Append(c);
                }
                else if (allowNegative && c == '-' && result.Length == 0)
                {
                    result.Append(c);
                }
                else if (allowDecimalSeparator && (c == ',' || c == '.') && !seenSeparator)
                {
                    seenSeparator = true;
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
