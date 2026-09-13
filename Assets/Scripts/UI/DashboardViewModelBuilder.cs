using System;
using System.Collections.Generic;
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

            var expenseBreakdown = BuildExpenseBreakdown(app, account, monthStart, monthEnd);
            var remainingToLiveText = BuildRemainingToLiveText(app, today);
            var upcomingOperations = BuildUpcomingOperations(app, account, today);
            var budgetSummary = BuildBudgetSummary(app, today);

            return new DashboardViewModel(
                account.Name,
                MoneyFormat.Format(account.OfficialBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.ClosingBalanceMinor, account.Currency),
                MoneyFormat.Format(forecast.LowestBalanceMinor, account.Currency),
                DateFormat.Short(forecast.LowestBalanceDate),
                remainingToLiveText,
                chartSeries,
                expenseBreakdown,
                verificationQueue,
                upcomingOperations,
                budgetSummary);
        }

        /// <summary>Empty when no budget exists for the current month — same reasoning as
        /// <see cref="BuildRemainingToLiveText"/>, and the same underlying figures as the Budget
        /// screen's own allocations table (`BudgetService.GetSummary`), just as compact progress
        /// bars instead of four columns. Not account-scoped, same as reste à vivre.</summary>
        private static IReadOnlyList<DashboardBudgetRowViewModel> BuildBudgetSummary(AppContainer app, DateTime today)
        {
            var budget = app.Budget.FindByYearMonth(today.Year, today.Month);
            if (budget is null)
            {
                return Array.Empty<DashboardBudgetRowViewModel>();
            }

            var categoryNames = app.Categories.ListActive().ToDictionary(c => c.Id, c => c.Name);

            return app.Budget.GetSummary(budget.Id)
                .Select(s =>
                {
                    var spentRatio = s.PlannedAmountMinor > 0
                        ? Math.Clamp((float)s.ActualAmountMinor / s.PlannedAmountMinor, 0f, 1f)
                        : 0f;
                    var isOverBudget = s.RemainingAmountMinor < 0;
                    var noteText = isOverBudget
                        ? $"Dépassement de {MoneyFormat.Format(-s.RemainingAmountMinor)}"
                        : $"{MoneyFormat.Format(s.RemainingAmountMinor)} restants";

                    return new DashboardBudgetRowViewModel(
                        categoryNames.TryGetValue(s.CategoryId, out var name) ? name : "—",
                        MoneyFormat.Format(s.ActualAmountMinor),
                        MoneyFormat.Format(s.PlannedAmountMinor),
                        noteText,
                        isOverBudget,
                        spentRatio);
                })
                .OrderBy(r => r.CategoryName)
                .ToList();
        }

        private const int UpcomingOperationsLimit = 5;

        /// <summary>The next few occurrences still ahead of today — deliberately excludes today
        /// itself (an occurrence due today already belongs in the verification queue above, not
        /// here too) and anything already overdue. Capped to a small count rather than a date
        /// range: this card previews what's coming, it isn't meant to be the full list (that's
        /// the Prévisions screen's "Occurrences prévues").</summary>
        private static IReadOnlyList<DashboardUpcomingItem> BuildUpcomingOperations(AppContainer app, Account account, DateTime today)
        {
            var horizonDays = app.Settings.Get().ForecastHorizonDays;
            return app.ForecastOccurrences.ListForAccount(account.Id, today.AddDays(1), today.AddDays(horizonDays))
                .OrderBy(o => o.ExpectedDate)
                .Take(UpcomingOperationsLimit)
                .Select(o => new DashboardUpcomingItem(
                    o.Label,
                    DateFormat.Short(o.ExpectedDate),
                    MoneyFormat.Format(o.ExpectedAmountMinor, account.Currency, forceSign: true),
                    o.RecurringOperationId.HasValue ? "Attendue" : "Estimée"))
                .ToList();
        }

        /// <summary>"—" when no budget exists for the current month — unlike the savings-evolution
        /// chart (ADR-123), reste à vivre is inherently budget-shaped: it sums the remaining
        /// (prévu − réel − engagé) of each allocated Expense-type category, so with no allocation
        /// there is nothing to sum in the first place, not just an empty history. Same definition
        /// as `BudgetOverview.RemainingToLiveMinor` (ADR-115) — user-wide, not account-scoped,
        /// unlike every other Dashboard figure, because a budget itself isn't account-scoped
        /// either.</summary>
        private static string BuildRemainingToLiveText(AppContainer app, DateTime today)
        {
            var budget = app.Budget.FindByYearMonth(today.Year, today.Month);
            if (budget is null)
            {
                return "—";
            }

            var overview = app.Budget.GetOverview(budget.Id);
            return MoneyFormat.Format(overview.RemainingToLiveMinor, forceSign: true);
        }

        /// <summary>Real expense transactions only (negative amounts, no internal transfer, an
        /// Expense-type category assigned — a negative amount categorized as Épargne is money
        /// moved to savings, not a "dépense") for the account's own currency. Narrower than
        /// `BudgetService.GetSummary`'s "réel" column, which sums by whatever category an
        /// allocation happens to target regardless of type: this donut is specifically about
        /// spending, and this screen is already scoped to one account, unlike the budget. Sorted
        /// by amount descending so both the donut's slice order and the legend below it read the
        /// same way: biggest expense first.</summary>
        private static IReadOnlyList<ExpenseCategorySliceViewModel> BuildExpenseBreakdown(
            AppContainer app, Account account, DateTime monthStart, DateTime monthEnd)
        {
            var expenseCategories = app.Categories.ListActive()
                .Where(c => c.Type == CategoryType.Expense)
                .ToDictionary(c => c.Id, c => c.Name);

            var byCategory = app.Transactions.ListForAccount(account.Id)
                .Where(t => t.OperationDate >= monthStart && t.OperationDate <= monthEnd)
                .Where(t => !t.IsInternalTransfer && t.CategoryId.HasValue && t.AmountMinor < 0)
                .Where(t => expenseCategories.ContainsKey(t.CategoryId!.Value))
                .GroupBy(t => t.CategoryId!.Value)
                .Select(g => (CategoryId: g.Key, AmountMinor: -g.Sum(t => t.AmountMinor)))
                .Where(g => g.AmountMinor > 0)
                .OrderByDescending(g => g.AmountMinor)
                .ToList();

            var total = byCategory.Sum(g => g.AmountMinor);
            if (total <= 0)
            {
                return Array.Empty<ExpenseCategorySliceViewModel>();
            }

            return byCategory
                .Select(g => new ExpenseCategorySliceViewModel(
                    expenseCategories.TryGetValue(g.CategoryId, out var name) ? name : "—",
                    g.AmountMinor,
                    MoneyFormat.Format(g.AmountMinor, account.Currency),
                    FormatPercent(100.0 * g.AmountMinor / total)))
                .ToList();
        }

        /// <summary>"12,3 %" — same rounding/formatting choice as
        /// `BudgetsViewModelBuilder.FormatPercent` (InvariantCulture round, comma swapped in by
        /// hand — never trust CurrentCulture for French decimals on this runtime).</summary>
        private static string FormatPercent(double value)
        {
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
            var text = rounded.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return $"{text.Replace('.', ',')} %";
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
