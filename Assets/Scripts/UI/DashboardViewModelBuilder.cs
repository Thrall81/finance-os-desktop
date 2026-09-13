using System;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the dashboard's display-ready data from AppContainer's services — separated from
    /// DashboardController so it can be unit-verified without instantiating any VisualElement.
    /// See docs/07-Interface.md §5.
    /// </summary>
    public static class DashboardViewModelBuilder
    {
        /// <summary>Null means no account exists yet — the UI shows the first-launch onboarding
        /// state instead (docs/07-Interface.md §4), not an empty dashboard.</summary>
        public static DashboardViewModel? Build(AppContainer app, DateTime today)
        {
            var account = ResolvePrimaryAccount(app);
            if (account is null)
            {
                return null;
            }

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var forecast = app.Forecast.GetForecast(account.Id, today, monthEnd, today);

            var verificationQueue = app.ForecastOccurrences.ListDueForVerification(today)
                .Where(o => o.AccountId == account.Id)
                .Select(o => new DashboardVerificationItem(
                    o.Id,
                    o.Label,
                    DateFormat.RelativeToToday(o.ExpectedDate, today),
                    MoneyFormat.Format(o.ExpectedAmountMinor, account.Currency)))
                .ToList();

            var chartSeries = forecast.Timeline
                .Select(day => new ChartPointViewModel(day.Date, day.ClosingBalanceMinor, day.IsActual))
                .ToList();

            return new DashboardViewModel(
                account.Name,
                MoneyFormat.Format(account.OfficialBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.ClosingBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.LowestBalanceMinor, account.Currency),
                DateFormat.Short(forecast.LowestBalanceDate),
                chartSeries,
                verificationQueue);
        }

        private static Account? ResolvePrimaryAccount(AppContainer app)
        {
            var settings = app.Settings.Get();
            if (settings.DefaultCurrentAccountId is { } accountId)
            {
                var preferred = app.Accounts.FindById(accountId);
                if (preferred is { IsArchived: false })
                {
                    return preferred;
                }
            }

            var active = app.Accounts.ListActive();
            return active.FirstOrDefault(a => a.Type == AccountType.Current) ?? active.FirstOrDefault();
        }
    }
}
