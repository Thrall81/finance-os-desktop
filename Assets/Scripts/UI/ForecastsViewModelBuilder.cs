using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the Prévisions screen's display-ready data. Takes the whole AppContainer, like
    /// DashboardViewModelBuilder — this screen genuinely synthesizes across accounts, the
    /// forecast engine, occurrences, categories and settings, so a narrower dependency list
    /// (as used by AccountsViewModelBuilder etc.) would not be honest about what it needs.
    /// See docs/07-Interface.md §3/§5.
    /// </summary>
    public static class ForecastsViewModelBuilder
    {
        public static ForecastsViewModel Build(AppContainer app, int? selectedAccountId, DateTime today)
        {
            var activeAccounts = app.Accounts.ListActive();
            var accountOptions = activeAccounts.Select(a => new DropdownOption(a.Id, a.Name)).ToList();
            var categoryOptions = app.Categories.ListActive().OrderBy(c => c.Name)
                .Select(c => new DropdownOption(c.Id, c.Name)).ToList();

            var account = selectedAccountId is int requestedId
                ? activeAccounts.FirstOrDefault(a => a.Id == requestedId)
                : ResolvePrimaryAccount(app, activeAccounts);

            if (account is null)
            {
                return new ForecastsViewModel(
                    accountOptions, null, null,
                    Array.Empty<ChartPointViewModel>(),
                    Array.Empty<ForecastTimelineRowViewModel>(),
                    Array.Empty<ForecastOccurrenceRowViewModel>(),
                    Array.Empty<VerificationRowViewModel>(),
                    categoryOptions);
            }

            var horizonDays = app.Settings.Get().ForecastHorizonDays;
            var horizonEnd = today.AddDays(horizonDays);
            var forecast = app.Forecast.GetForecast(account.Id, today, horizonEnd, today);

            var synthesis = new ForecastSynthesisViewModel(
                MoneyFormat.Format(account.OfficialBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.ClosingBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.LowestBalanceMinor, account.Currency),
                DateFormat.Short(forecast.LowestBalanceDate),
                MoneyFormat.Format(forecast.ExpectedIncomeMinor, account.Currency, forceSign: true),
                MoneyFormat.Format(forecast.ExpectedExpensesMinor, account.Currency, forceSign: true),
                forecast.Warnings);

            var chartSeries = forecast.Timeline
                .Select(day => new ChartPointViewModel(day.Date, day.ClosingBalanceMinor, day.IsActual))
                .ToList();

            var timeline = forecast.Timeline
                .Where(day => day.Events.Count > 0)
                .Select(day => new ForecastTimelineRowViewModel(
                    DateFormat.Short(day.Date),
                    MoneyFormat.Format(day.ClosingBalanceMinor, account.Currency),
                    string.Join(" · ", day.Events.Select(e =>
                        $"{e.Label} ({MoneyFormat.Format(e.AmountMinor, account.Currency, forceSign: true)})"))))
                .ToList();

            var occurrences = app.ForecastOccurrences.ListForAccount(account.Id, today, horizonEnd)
                .Select(o => new ForecastOccurrenceRowViewModel(
                    o.Id,
                    DateFormat.Short(o.ExpectedDate),
                    o.Label,
                    MoneyFormat.Format(o.ExpectedAmountMinor, account.Currency, forceSign: true),
                    StatusText(o.Status),
                    o.Status == ForecastOccurrenceStatus.Missed))
                .ToList();

            var verificationQueue = app.ForecastOccurrences.ListDueForVerification(today)
                .Where(o => o.AccountId == account.Id)
                .Select(o => new VerificationRowViewModel(
                    o.Id,
                    o.Label,
                    DateFormat.RelativeToToday(o.ExpectedDate, today),
                    MoneyFormat.Format(o.ExpectedAmountMinor, account.Currency, forceSign: true),
                    o.ExpectedDate,
                    o.ExpectedAmountMinor,
                    o.Status == ForecastOccurrenceStatus.Missed))
                .ToList();

            return new ForecastsViewModel(
                accountOptions, account.Id, synthesis, chartSeries, timeline, occurrences, verificationQueue, categoryOptions);
        }

        private static Account? ResolvePrimaryAccount(AppContainer app, IReadOnlyList<Account> activeAccounts)
        {
            var settings = app.Settings.Get();
            if (settings.DefaultCurrentAccountId is { } accountId)
            {
                var preferred = activeAccounts.FirstOrDefault(a => a.Id == accountId);
                if (preferred is not null)
                {
                    return preferred;
                }
            }

            return activeAccounts.FirstOrDefault(a => a.Type == AccountType.Current) ?? activeAccounts.FirstOrDefault();
        }

        private static string StatusText(ForecastOccurrenceStatus status) => status switch
        {
            ForecastOccurrenceStatus.Planned => "Prévue",
            ForecastOccurrenceStatus.Matched => "Confirmée",
            ForecastOccurrenceStatus.Cancelled => "Annulée",
            ForecastOccurrenceStatus.Missed => "Manquée",
            ForecastOccurrenceStatus.Ignored => "Ignorée",
            _ => status.ToString(),
        };
    }
}
