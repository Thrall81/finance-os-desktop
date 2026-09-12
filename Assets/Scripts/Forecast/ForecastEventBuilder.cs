using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;

namespace FinanceOS.Forecast
{
    /// <summary>
    /// Converts real transactions and unresolved occurrences into the unified event list the
    /// calculator sweeps through. See docs/06-Moteur_de_prevision.md §4 and §23 (no double counting).
    /// </summary>
    public static class ForecastEventBuilder
    {
        private static readonly ForecastOccurrenceStatus[] ResolvedStatuses =
        {
            ForecastOccurrenceStatus.Matched,
            ForecastOccurrenceStatus.Cancelled,
            ForecastOccurrenceStatus.Ignored,
        };

        /// <summary>
        /// Builds every event that touches <paramref name="accountId"/>, from both sides of a
        /// transfer-shaped occurrence when relevant. Does not sort or filter by date range —
        /// that is <see cref="ForecastCalculator"/>'s job.
        /// </summary>
        public static IReadOnlyList<ForecastEvent> BuildEvents(
            int accountId,
            IReadOnlyList<Transaction> transactions,
            IReadOnlyList<ForecastOccurrence> occurrences)
        {
            var events = new List<ForecastEvent>();

            foreach (var transaction in transactions)
            {
                if (transaction.AccountId != accountId)
                {
                    continue;
                }

                events.Add(new ForecastEvent(
                    accountId,
                    transaction.Id,
                    null,
                    transaction.OperationDate,
                    transaction.AmountMinor,
                    transaction.NormalizedLabel ?? transaction.OriginalLabel,
                    ForecastEventCertainty.Confirmed,
                    transaction.IsInternalTransfer));
            }

            foreach (var occurrence in occurrences)
            {
                if (ResolvedStatuses.Contains(occurrence.Status))
                {
                    // Matched occurrences are superseded by the real transaction they matched
                    // (already included above); cancelled/ignored ones never happen.
                    continue;
                }

                var isTransfer = occurrence.DestinationAccountId.HasValue;
                var certainty = occurrence.RecurringOperationId.HasValue
                    ? ForecastEventCertainty.Expected
                    : ForecastEventCertainty.Estimated;

                if (occurrence.AccountId == accountId)
                {
                    events.Add(new ForecastEvent(
                        accountId, null, occurrence.Id, occurrence.ExpectedDate, occurrence.ExpectedAmountMinor,
                        occurrence.Label, certainty, isTransfer));
                }
                else if (occurrence.DestinationAccountId == accountId)
                {
                    // The destination side of a transfer mirrors the source's signed amount.
                    events.Add(new ForecastEvent(
                        accountId, null, occurrence.Id, occurrence.ExpectedDate, -occurrence.ExpectedAmountMinor,
                        occurrence.Label, certainty, isTransfer));
                }
            }

            return events;
        }
    }
}
