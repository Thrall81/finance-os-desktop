using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the Budget screen's display-ready data from BudgetService/CategoryService alone —
    /// same narrower-dependency choice as AccountsViewModelBuilder. Never creates a budget row as
    /// a side effect of being built (uses BudgetService.FindByYearMonth, not GetOrCreate).
    /// See docs/07-Interface.md §3/§5.
    /// </summary>
    public static class BudgetsViewModelBuilder
    {
        public static BudgetsViewModel Build(BudgetService budgets, CategoryService categories, int year, int month)
        {
            var monthLabel = DateFormat.MonthYear(new DateTime(year, month, 1));
            var categoryList = categories.ListActive().OrderBy(c => c.Name).ToList();
            var categoryNames = categoryList.ToDictionary(c => c.Id, c => c.Name);

            var budget = budgets.FindByYearMonth(year, month);
            if (budget is null)
            {
                return new BudgetsViewModel(
                    year, month, monthLabel, false, null, false, null,
                    Array.Empty<BudgetAllocationRowViewModel>(),
                    categoryList.Select(c => new DropdownOption(c.Id, c.Name)).ToList());
            }

            var summary = budgets.GetSummary(budget.Id);
            var overview = budgets.GetOverview(budget.Id);

            var rows = summary
                .Select(s => new BudgetAllocationRowViewModel(
                    s.AllocationId,
                    s.CategoryId,
                    categoryNames.TryGetValue(s.CategoryId, out var name) ? name : "—",
                    MoneyFormat.Format(s.PlannedAmountMinor),
                    MoneyFormat.Format(s.ActualAmountMinor),
                    MoneyFormat.Format(s.CommittedAmountMinor),
                    MoneyFormat.Format(s.RemainingAmountMinor, forceSign: true),
                    s.RemainingAmountMinor < 0))
                .OrderBy(r => r.CategoryName)
                .ToList();

            var allocatedCategoryIds = summary.Select(s => s.CategoryId).ToHashSet();
            var unallocated = categoryList
                .Where(c => !allocatedCategoryIds.Contains(c.Id))
                .Select(c => new DropdownOption(c.Id, c.Name))
                .ToList();

            var overviewViewModel = new BudgetOverviewViewModel(
                MoneyFormat.Format(overview.RemainingToLiveMinor, forceSign: true),
                MoneyFormat.Format(overview.IncomeMinor),
                MoneyFormat.Format(overview.SavingsMinor),
                FormatPercent(overview.SavingsRatePercent));

            return new BudgetsViewModel(
                year, month, monthLabel, true, budget.Id, budget.Status == BudgetStatus.Closed,
                overviewViewModel, rows, unallocated);
        }

        /// <summary>"12,3 %" — rounds via InvariantCulture (safe, no ICU data involved) then
        /// swaps the decimal point for a comma by hand, same reasoning as MoneyFormat: don't
        /// trust CurrentCulture to format French decimals correctly on this runtime.</summary>
        private static string FormatPercent(double value)
        {
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
            var text = rounded.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return $"{text.Replace('.', ',')} %";
        }
    }
}
