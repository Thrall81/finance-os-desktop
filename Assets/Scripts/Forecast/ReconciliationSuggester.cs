using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;

namespace FinanceOS.Forecast
{
    /// <summary>
    /// Suggests which unresolved occurrence a newly entered or imported transaction most likely
    /// completes — confirmation always stays manual (docs/06-Moteur_de_prevision.md §7), this
    /// only narrows down the candidates.
    /// </summary>
    public static class ReconciliationSuggester
    {
        private const int DefaultDateToleranceDays = 3;
        private const long DefaultAmountToleranceMinor = 0;

        public static bool IsCandidate(
            Transaction transaction,
            ForecastOccurrence occurrence,
            int dateToleranceDays = DefaultDateToleranceDays,
            long amountToleranceMinor = DefaultAmountToleranceMinor)
        {
            if (transaction.AccountId != occurrence.AccountId)
            {
                return false;
            }

            if (occurrence.Status is not (ForecastOccurrenceStatus.Planned or ForecastOccurrenceStatus.Missed))
            {
                return false;
            }

            var dateDifferenceDays = Math.Abs((transaction.OperationDate - occurrence.ExpectedDate).Days);
            if (dateDifferenceDays > dateToleranceDays)
            {
                return false;
            }

            var amountDifferenceMinor = Math.Abs(transaction.AmountMinor - occurrence.ExpectedAmountMinor);
            return amountDifferenceMinor <= amountToleranceMinor;
        }

        /// <summary>The closest candidate by date, then by amount — still just a suggestion.</summary>
        public static ForecastOccurrence? FindBestCandidate(
            Transaction transaction,
            IReadOnlyList<ForecastOccurrence> candidates,
            int dateToleranceDays = DefaultDateToleranceDays,
            long amountToleranceMinor = DefaultAmountToleranceMinor) => candidates
            .Where(occurrence => IsCandidate(transaction, occurrence, dateToleranceDays, amountToleranceMinor))
            .OrderBy(occurrence => Math.Abs((transaction.OperationDate - occurrence.ExpectedDate).Days))
            .ThenBy(occurrence => Math.Abs(transaction.AmountMinor - occurrence.ExpectedAmountMinor))
            .FirstOrDefault();
    }
}
