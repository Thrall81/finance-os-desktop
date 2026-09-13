using System;

namespace FinanceOS.UI
{
    /// <summary>
    /// French date display formats, spelled out rather than routed through CultureInfo — same
    /// reasoning as MoneyFormat. See docs/07-Interface.md and the old project's §40 (kept as
    /// reference for the format catalogue; French wording is authored fresh here).
    /// </summary>
    public static class DateFormat
    {
        private static readonly string[] MonthNames =
        {
            "janvier", "février", "mars", "avril", "mai", "juin",
            "juillet", "août", "septembre", "octobre", "novembre", "décembre",
        };

        /// <summary>"27 août" — compact, for lists.</summary>
        public static string Short(DateTime date) => $"{date.Day} {MonthNames[date.Month - 1]}";

        /// <summary>"27 août 2026" — unambiguous, for detail views.</summary>
        public static string Long(DateTime date) => $"{date.Day} {MonthNames[date.Month - 1]} {date.Year}";

        /// <summary>"il y a 5 jours" / "aujourd'hui" / "dans 3 jours" — for the verification queue.
        /// See docs/07-Interface.md §6.</summary>
        public static string RelativeToToday(DateTime date, DateTime today)
        {
            var days = (today.Date - date.Date).Days;
            return days switch
            {
                0 => "aujourd'hui",
                1 => "hier",
                > 1 => $"il y a {days} jours",
                -1 => "demain",
                _ => $"dans {-days} jours",
            };
        }

        /// <summary>"13/09/2026" — the editable form representation, since French users expect
        /// slash-separated numeric dates in a text field rather than a spelled-out month.</summary>
        public static string ForInput(DateTime date) => $"{date.Day:00}/{date.Month:00}/{date.Year:0000}";

        /// <summary>Parses <see cref="ForInput"/>'s format back. No CultureInfo involved — same
        /// reasoning as MoneyFormat.TryParseEurosToMinor.</summary>
        public static bool TryParseInput(string text, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Trim().Split('/');
            if (parts.Length != 3
                || !int.TryParse(parts[0], out var day)
                || !int.TryParse(parts[1], out var month)
                || !int.TryParse(parts[2], out var year))
            {
                return false;
            }

            try
            {
                date = new DateTime(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
