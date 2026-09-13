using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>One category's row in a budget's summary — magnitudes throughout, since
    /// <see cref="BudgetAllocation.PlannedAmountMinor"/> is itself always a positive envelope
    /// regardless of the category's type. A negative <see cref="RemainingAmountMinor"/> means
    /// over budget. See docs/01-Perimetre.md §2.9.</summary>
    public sealed record BudgetCategorySummary(
        int AllocationId,
        int CategoryId,
        long PlannedAmountMinor,
        long ActualAmountMinor,
        long CommittedAmountMinor,
        long RemainingAmountMinor);

    /// <summary>The month's two headline figures beyond the per-category breakdown — "reste à
    /// vivre" (what's left across every Expense-category envelope, réel+engagé already
    /// subtracted) and the savings rate (money moved into Savings-type categories over income),
    /// both magnitudes/ratios derived only from data already in the summary/period, no new
    /// concept invented beyond docs/01-Perimetre.md §2.9's own wording.
    /// <see cref="SavingsRatePercent"/> is 0 when there is no income to divide by.</summary>
    public sealed record BudgetOverview(
        long RemainingToLiveMinor,
        long IncomeMinor,
        long SavingsMinor,
        double SavingsRatePercent);

    /// <summary>Orchestrates the monthly budget: allocations, and the prévu/réel/engagé/restant
    /// figures computed from transactions and occurrences. See docs/01-Perimetre.md §2.9.</summary>
    public sealed class BudgetService
    {
        private readonly BudgetRepository _budgets;
        private readonly BudgetAllocationRepository _allocations;
        private readonly TransactionRepository _transactions;
        private readonly ForecastOccurrenceRepository _occurrences;
        private readonly CategoryRepository _categories;

        public BudgetService(
            BudgetRepository budgets,
            BudgetAllocationRepository allocations,
            TransactionRepository transactions,
            ForecastOccurrenceRepository occurrences,
            CategoryRepository categories)
        {
            _budgets = budgets;
            _allocations = allocations;
            _transactions = transactions;
            _occurrences = occurrences;
            _categories = categories;
        }

        /// <summary>Read-only lookup — unlike <see cref="GetOrCreate"/>, never inserts a row.
        /// The Budget screen uses this every refresh so simply viewing a month never creates a
        /// budget for it.</summary>
        public Budget? FindByYearMonth(int year, int month) => _budgets.FindByYearMonth(year, month);

        public Budget GetOrCreate(int year, int month, bool copyFromPreviousMonth = false)
        {
            var existing = _budgets.FindByYearMonth(year, month);
            if (existing is not null)
            {
                return existing;
            }

            var budget = _budgets.Insert(new Budget(year, month));

            if (copyFromPreviousMonth)
            {
                var (previousYear, previousMonth) = month == 1 ? (year - 1, 12) : (year, month - 1);
                var previousBudget = _budgets.FindByYearMonth(previousYear, previousMonth);
                if (previousBudget is not null)
                {
                    foreach (var allocation in _allocations.ListForBudget(previousBudget.Id))
                    {
                        _allocations.Insert(new BudgetAllocation(budget.Id, allocation.CategoryId, allocation.PlannedAmountMinor));
                    }
                }
            }

            return budget;
        }

        /// <summary>Creates the allocation if none exists for this category yet, otherwise
        /// updates its planned amount — the upsert semantics docs/08-API_REST.md (old project)
        /// used a PUT for. See docs/01-Perimetre.md §2.9.</summary>
        public BudgetAllocation UpsertAllocation(int budgetId, int categoryId, long plannedAmountMinor)
        {
            var existing = _allocations.FindByBudgetAndCategory(budgetId, categoryId);
            if (existing is not null)
            {
                existing.UpdatePlannedAmount(plannedAmountMinor);
                _allocations.Update(existing);
                return existing;
            }

            return _allocations.Insert(new BudgetAllocation(budgetId, categoryId, plannedAmountMinor));
        }

        public void RemoveAllocation(int allocationId) => _allocations.Delete(allocationId);

        public void Activate(int budgetId) => UpdateStatus(budgetId, b => b.Activate());

        public void Close(int budgetId) => UpdateStatus(budgetId, b => b.Close());

        public void Reopen(int budgetId) => UpdateStatus(budgetId, b => b.Reopen());

        /// <summary>Prévu/réel/engagé/restant per category for one budget month. Réel and engagé
        /// are magnitudes (internal transfers excluded from réel) so they compare directly
        /// against the always-positive planned envelope. See docs/06-Moteur_de_prevision.md §24.</summary>
        public IReadOnlyList<BudgetCategorySummary> GetSummary(int budgetId)
        {
            var budget = RequireBudget(budgetId);
            var (monthStart, monthEnd) = MonthRange(budget);

            var actualByCategory = _transactions.ListForPeriod(monthStart, monthEnd)
                .Where(t => !t.IsInternalTransfer && t.CategoryId.HasValue)
                .GroupBy(t => t.CategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.AmountMinor)));

            var committedByCategory = _occurrences.ListForPeriod(monthStart, monthEnd)
                .Where(o => o.CategoryId.HasValue)
                .GroupBy(o => o.CategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(o => Math.Abs(o.ExpectedAmountMinor)));

            return _allocations.ListForBudget(budgetId)
                .Select(allocation =>
                {
                    var actual = actualByCategory.GetValueOrDefault(allocation.CategoryId, 0L);
                    var committed = committedByCategory.GetValueOrDefault(allocation.CategoryId, 0L);
                    return new BudgetCategorySummary(
                        allocation.Id, allocation.CategoryId, allocation.PlannedAmountMinor, actual, committed,
                        allocation.PlannedAmountMinor - actual - committed);
                })
                .ToList();
        }

        /// <summary>"Reste à vivre" and the savings rate for the month — see
        /// <see cref="BudgetOverview"/> for what each figure means and why.</summary>
        public BudgetOverview GetOverview(int budgetId)
        {
            var budget = RequireBudget(budgetId);
            var (monthStart, monthEnd) = MonthRange(budget);
            var categoryTypes = _categories.ListAll().ToDictionary(c => c.Id, c => c.Type);

            var remainingToLive = GetSummary(budgetId)
                .Where(s => categoryTypes.TryGetValue(s.CategoryId, out var type) && type == CategoryType.Expense)
                .Sum(s => s.RemainingAmountMinor);

            var income = SumForCategoryType(monthStart, monthEnd, categoryTypes, CategoryType.Income);
            var savings = SumForCategoryType(monthStart, monthEnd, categoryTypes, CategoryType.Savings);
            var savingsRate = income == 0 ? 0d : (double)savings / income * 100d;

            return new BudgetOverview(remainingToLive, income, savings, savingsRate);
        }

        /// <summary>Réel (transactions) + engagé (occurrences) for every category of one type,
        /// within the month — the same "already happened or still expected" magnitude logic
        /// <see cref="GetSummary"/> uses per category, just grouped by category type instead.</summary>
        private long SumForCategoryType(
            DateTime monthStart, DateTime monthEnd, Dictionary<int, CategoryType> categoryTypes, CategoryType type)
        {
            bool MatchesType(int? categoryId) =>
                categoryId is int id && categoryTypes.TryGetValue(id, out var categoryType) && categoryType == type;

            var actual = _transactions.ListForPeriod(monthStart, monthEnd)
                .Where(t => !t.IsInternalTransfer && MatchesType(t.CategoryId))
                .Sum(t => Math.Abs(t.AmountMinor));

            var committed = _occurrences.ListForPeriod(monthStart, monthEnd)
                .Where(o => MatchesType(o.CategoryId))
                .Sum(o => Math.Abs(o.ExpectedAmountMinor));

            return actual + committed;
        }

        private Budget RequireBudget(int budgetId) =>
            _budgets.FindById(budgetId) ?? throw new InvalidOperationException($"Budget #{budgetId} not found.");

        private static (DateTime Start, DateTime End) MonthRange(Budget budget)
        {
            var start = new DateTime(budget.Year, budget.Month, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        private void UpdateStatus(int budgetId, Action<Budget> mutate)
        {
            var budget = RequireBudget(budgetId);
            mutate(budget);
            _budgets.Update(budget);
        }
    }
}
