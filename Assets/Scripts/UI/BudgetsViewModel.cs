using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One category's row in the allocations table, already formatted.
    /// See docs/07-Interface.md §3.</summary>
    public sealed record BudgetAllocationRowViewModel(
        int AllocationId,
        int CategoryId,
        string CategoryName,
        string PlannedText,
        string ActualText,
        string CommittedText,
        string RemainingText,
        bool IsOverBudget);

    /// <summary>"Reste à vivre" and the savings rate, already formatted — see
    /// <see cref="FinanceOS.App.BudgetOverview"/> for what each figure means.</summary>
    public sealed record BudgetOverviewViewModel(
        string RemainingToLiveText, string IncomeText, string SavingsText, string SavingsRateText);

    /// <summary>Everything the Budget screen displays for one calendar month. Null
    /// <see cref="Overview"/>/empty <see cref="Allocations"/> with <see cref="BudgetExists"/>
    /// false means no budget row exists yet for this month — the screen shows a create prompt
    /// instead, per docs/01-Perimetre.md §2.9's "création du budget mensuel" step.</summary>
    public sealed record BudgetsViewModel(
        int Year,
        int Month,
        string MonthLabel,
        bool BudgetExists,
        int? BudgetId,
        bool IsClosed,
        BudgetOverviewViewModel? Overview,
        IReadOnlyList<BudgetAllocationRowViewModel> Allocations,
        IReadOnlyList<DropdownOption> UnallocatedCategories);
}
