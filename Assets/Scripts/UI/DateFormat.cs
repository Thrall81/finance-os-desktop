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
    }
}
