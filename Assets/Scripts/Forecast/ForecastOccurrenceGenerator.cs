using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;

namespace FinanceOS.Forecast
{
    /// <summary>
    /// Generates the missing <see cref="ForecastOccurrence"/> instances for a recurring
    /// operation over a horizon. Idempotent by construction: it never returns an occurrence for
    /// a date already present in <c>existingExpectedDates</c>, so calling it again with the same
    /// inputs plus the newly created ones produces nothing further.
    /// See docs/06-Moteur_de_prevision.md §5.
    /// </summary>
    public static class ForecastOccurrenceGenerator
    {
        public static IReadOnlyList<ForecastOccurrence> GenerateMissingOccurrences(
            RecurringOperation operation,
            DateTime horizonStart,
            DateTime horizonEnd,
            IReadOnlyList<DateTime> existingExpectedDates)
        {
            if (!operation.IsActive)
            {
                return Array.Empty<ForecastOccurrence>();
            }

            var existing = new HashSet<DateTime>(existingExpectedDates);
            var (accountId, destinationAccountId) = ResolveAccounts(operation);

            var results = new List<ForecastOccurrence>();
            foreach (var date in EnumerateScheduledDates(operation, horizonStart, horizonEnd))
            {
                if (existing.Contains(date))
                {
                    continue;
                }

                results.Add(new ForecastOccurrence(
                    accountId: accountId,
                    label: operation.Name,
                    expectedDate: date,
                    expectedAmountMinor: operation.ExpectedAmountMinor,
                    recurringOperationId: operation.Id,
                    destinationAccountId: destinationAccountId,
                    categoryId: operation.CategoryId,
                    counterpartyId: operation.CounterpartyId));
            }

            return results;
        }

        /// <summary>Which account the occurrence's required <c>AccountId</c> and optional
        /// <c>DestinationAccountId</c> map to, per operation type.</summary>
        private static (int AccountId, int? DestinationAccountId) ResolveAccounts(RecurringOperation operation) =>
            operation.Type switch
            {
                RecurringOperationType.Income => (operation.DestinationAccountId!.Value, null),
                RecurringOperationType.Expense => (operation.SourceAccountId!.Value, null),
                RecurringOperationType.SavingsTransfer or RecurringOperationType.InternalTransfer =>
                    (operation.SourceAccountId!.Value, operation.DestinationAccountId),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation.Type, "Unhandled recurring operation type."),
            };

        private static IEnumerable<DateTime> EnumerateScheduledDates(RecurringOperation operation, DateTime horizonStart, DateTime horizonEnd)
        {
            var effectiveEnd = operation.EndDate is { } end && end < horizonEnd ? end : horizonEnd;
            if (effectiveEnd < operation.StartDate)
            {
                yield break;
            }

            if (operation.Frequency == RecurringFrequency.Weekly)
            {
                var stepDays = 7 * Math.Max(1, operation.IntervalValue);
                for (var date = operation.StartDate; date <= effectiveEnd; date = date.AddDays(stepDays))
                {
                    if (date >= horizonStart)
                    {
                        yield return date;
                    }
                }

                yield break;
            }

            var stepMonths = MonthsFor(operation.Frequency) * Math.Max(1, operation.IntervalValue);
            var dayOfMonth = operation.ExpectedDayOfMonth ?? operation.StartDate.Day;

            var monthCursor = new DateTime(operation.StartDate.Year, operation.StartDate.Month, 1);
            var endMonthCursor = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);

            while (monthCursor <= endMonthCursor)
            {
                // Clamp to the last real day of the month — a "31st" recurs on the 28th/29th/30th
                // when the month is shorter. See docs/06-Moteur_de_prevision.md §5.
                var daysInMonth = DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month);
                var occurrenceDate = new DateTime(monthCursor.Year, monthCursor.Month, Math.Min(dayOfMonth, daysInMonth));

                if (occurrenceDate >= operation.StartDate && occurrenceDate >= horizonStart && occurrenceDate <= effectiveEnd)
                {
                    yield return occurrenceDate;
                }

                monthCursor = monthCursor.AddMonths(stepMonths);
            }
        }

        private static int MonthsFor(RecurringFrequency frequency) => frequency switch
        {
            RecurringFrequency.Monthly => 1,
            RecurringFrequency.Bimonthly => 2,
            RecurringFrequency.Quarterly => 3,
            RecurringFrequency.Semiannual => 6,
            RecurringFrequency.Yearly => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Not a month-based frequency."),
        };
    }
}
