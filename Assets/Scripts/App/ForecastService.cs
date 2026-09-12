using System;
using System.Collections.Generic;
using FinanceOS.Data;
using FinanceOS.Domain;
using FinanceOS.Forecast;

namespace FinanceOS.App
{
    /// <summary>
    /// Loads the data <see cref="ForecastCalculator"/> needs and calls it — the only place that
    /// touches both a repository and the forecast engine for this purpose.
    /// See docs/02-Architecture.md §3.3 and docs/06-Moteur_de_prevision.md §9.
    /// </summary>
    public sealed class ForecastService
    {
        private readonly AccountRepository _accounts;
        private readonly TransactionRepository _transactions;
        private readonly ForecastOccurrenceRepository _occurrences;

        public ForecastService(
            AccountRepository accounts, TransactionRepository transactions, ForecastOccurrenceRepository occurrences)
        {
            _accounts = accounts;
            _transactions = transactions;
            _occurrences = occurrences;
        }

        public ForecastResult GetForecast(int accountId, DateTime dateFrom, DateTime dateTo, DateTime today)
        {
            var account = RequireAccount(accountId);
            var (_, openingReferenceDate) = ForecastCalculator.ResolveOpeningBalance(account);

            var transactions = _transactions.ListForAccountSince(accountId, openingReferenceDate);
            var occurrences = _occurrences.ListForAccountSince(accountId, openingReferenceDate);

            return ForecastCalculator.Calculate(new ForecastRequest(dateFrom, dateTo), account, transactions, occurrences, today);
        }

        /// <summary>"Et si ?" — the exact same calculation with one extra, never-persisted
        /// occurrence appended in memory. See docs/06-Moteur_de_prevision.md §9.</summary>
        public ForecastResult Simulate(
            int accountId,
            DateTime dateFrom,
            DateTime dateTo,
            DateTime today,
            string simulatedLabel,
            DateTime simulatedDate,
            long simulatedAmountMinor,
            int? simulatedCategoryId = null)
        {
            var account = RequireAccount(accountId);
            var (_, openingReferenceDate) = ForecastCalculator.ResolveOpeningBalance(account);

            var transactions = _transactions.ListForAccountSince(accountId, openingReferenceDate);
            var occurrences = new List<ForecastOccurrence>(_occurrences.ListForAccountSince(accountId, openingReferenceDate))
            {
                new(accountId, simulatedLabel, simulatedDate, simulatedAmountMinor, categoryId: simulatedCategoryId),
            };

            return ForecastCalculator.Calculate(new ForecastRequest(dateFrom, dateTo), account, transactions, occurrences, today);
        }

        private Account RequireAccount(int accountId) =>
            _accounts.FindById(accountId) ?? throw new InvalidOperationException($"Account #{accountId} not found.");
    }
}
