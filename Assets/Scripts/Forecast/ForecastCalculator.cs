using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;

namespace FinanceOS.Forecast
{
    public sealed record ForecastRequest(DateTime DateFrom, DateTime DateTo);

    /// <summary>One calendar day of the projection. <see cref="IsActual"/> marks the boundary
    /// between history and projection for the UI (solid vs dashed line).
    /// See docs/07-Interface.md §8.</summary>
    public sealed record ForecastDayPoint(
        DateTime Date,
        long OpeningBalanceMinor,
        long ClosingBalanceMinor,
        bool IsActual,
        IReadOnlyList<ForecastEvent> Events);

    public sealed record ForecastResult(
        DateTime DateFrom,
        DateTime DateTo,
        long OpeningBalanceMinor,
        long ClosingBalanceMinor,
        long LowestBalanceMinor,
        DateTime LowestBalanceDate,
        long ExpectedIncomeMinor,
        long ExpectedExpensesMinor,
        IReadOnlyList<ForecastDayPoint> Timeline,
        IReadOnlyList<string> Warnings);

    /// <summary>
    /// Projects one account's daily balance over a period from its last known balance, real
    /// transactions since then, and unresolved occurrences. Pure computation: never touches
    /// storage, never persists anything — a simulation is just a call with one extra in-memory
    /// occurrence that is discarded afterwards. See docs/06-Moteur_de_prevision.md.
    /// </summary>
    public static class ForecastCalculator
    {
        private const int StaleBalanceWarningDays = 14;

        public static ForecastResult Calculate(
            ForecastRequest request,
            Account account,
            IReadOnlyList<Transaction> transactions,
            IReadOnlyList<ForecastOccurrence> occurrences,
            DateTime today)
        {
            if (request.DateTo < request.DateFrom)
            {
                throw new ArgumentException("DateTo cannot precede DateFrom.", nameof(request));
            }

            var (openingReferenceBalance, openingReferenceDate) = ResolveOpeningBalance(account);
            var warnings = new List<string>();

            var effectiveDateFrom = request.DateFrom > openingReferenceDate ? request.DateFrom : openingReferenceDate;
            if (effectiveDateFrom > request.DateFrom)
            {
                warnings.Add(
                    $"La projection démarre le {effectiveDateFrom:yyyy-MM-dd} (date du dernier solde connu de {account.Name}) plutôt qu'à la date demandée.");
            }

            var staleDays = (today.Date - openingReferenceDate.Date).Days;
            if (staleDays > StaleBalanceWarningDays)
            {
                warnings.Add($"Le dernier solde connu de {account.Name} date de {staleDays} jours.");
            }

            var eventsByDate = ForecastEventBuilder
                .BuildEvents(account.Id, transactions, occurrences)
                .Where(e => e.Date > openingReferenceDate && e.Date <= request.DateTo)
                .ToLookup(e => e.Date);

            var timeline = new List<ForecastDayPoint>();
            var runningBalance = openingReferenceBalance;
            var resultOpeningBalance = openingReferenceBalance;
            var lowestBalance = openingReferenceBalance;
            var lowestBalanceDate = effectiveDateFrom;
            long expectedIncome = 0;
            long expectedExpenses = 0;
            var openingCaptured = false;

            for (var day = openingReferenceDate.AddDays(1); day <= request.DateTo; day = day.AddDays(1))
            {
                if (!openingCaptured && day >= effectiveDateFrom)
                {
                    resultOpeningBalance = runningBalance;
                    lowestBalance = runningBalance;
                    lowestBalanceDate = day;
                    openingCaptured = true;
                }

                var dayOpening = runningBalance;
                var dayEvents = eventsByDate[day].ToList();

                foreach (var evt in dayEvents)
                {
                    runningBalance += evt.AmountMinor;

                    if (day >= effectiveDateFrom && !evt.IsTransfer)
                    {
                        if (evt.AmountMinor > 0)
                        {
                            expectedIncome += evt.AmountMinor;
                        }
                        else
                        {
                            expectedExpenses += evt.AmountMinor;
                        }
                    }
                }

                if (day < effectiveDateFrom)
                {
                    continue;
                }

                if (runningBalance < lowestBalance)
                {
                    lowestBalance = runningBalance;
                    lowestBalanceDate = day;
                }

                timeline.Add(new ForecastDayPoint(day, dayOpening, runningBalance, day <= today, dayEvents));
            }

            if (!openingCaptured)
            {
                // effectiveDateFrom > DateTo: an empty, but well-defined, window.
                resultOpeningBalance = runningBalance;
                lowestBalance = runningBalance;
                lowestBalanceDate = effectiveDateFrom;
            }

            return new ForecastResult(
                effectiveDateFrom,
                request.DateTo,
                resultOpeningBalance,
                runningBalance,
                lowestBalance,
                lowestBalanceDate,
                expectedIncome,
                expectedExpenses,
                timeline,
                warnings);
        }

        /// <summary>Source-of-truth priority for the starting point of a projection:
        /// the account's last known official balance, dated at that observation — or, if
        /// none was ever recorded, the initial balance dated at account creation.
        /// See docs/06-Moteur_de_prevision.md §9.1.</summary>
        public static (long BalanceMinor, DateTime ReferenceDate) ResolveOpeningBalance(Account account) =>
            (account.OfficialBalanceMinor, account.OfficialBalanceDate ?? account.CreatedAt.Date);
    }
}
