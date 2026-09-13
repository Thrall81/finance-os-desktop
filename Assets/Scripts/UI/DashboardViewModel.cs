using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the "file de vérification" — see docs/07-Interface.md §6.</summary>
    public sealed record DashboardVerificationItem(int OccurrenceId, string Label, string DateText, string AmountText);

    /// <summary>One row in "Prochaines opérations" — still-future occurrences, unlike the
    /// verification queue's arrived-or-overdue ones. CertaintyText is "Attendue" (tied to a
    /// recurring operation) or "Estimée" (a one-off occurrence) — the same distinction
    /// `FinanceOS.Forecast.ForecastEventCertainty` makes internally, just not otherwise surfaced
    /// as UI text yet. See docs/07-Interface.md §5.</summary>
    public sealed record DashboardUpcomingItem(string Label, string DateText, string AmountText, string CertaintyText);

    /// <summary>One allocated category's compact progress bar for "Synthèse budgétaire" — same
    /// underlying figures as the Budget screen's allocations table (`BudgetCategorySummary`), just
    /// a bar instead of four columns. NoteText is already the full phrase ("135,20 € restants" or
    /// "Dépassement de 12,50 €") rather than a raw signed amount, so the controller only assigns
    /// text/classes, no sentence composition. SpentRatio is réel-only over prévu, clamped to
    /// [0, 1] — the bar's fill width, computed here since it's derived from the same raw amounts
    /// as the rest of the row, not a display concern for the controller.
    /// See docs/07-Interface.md §5.</summary>
    public sealed record DashboardBudgetRowViewModel(
        string CategoryName, string ActualText, string PlannedText, string NoteText, bool IsOverBudget, float SpentRatio);

    /// <summary>One category's slice of this month's expenses — raw amount for the donut's angle
    /// math plus already-formatted text for the legend row next to it. See docs/07-Interface.md §8.</summary>
    public sealed record ExpenseCategorySliceViewModel(
        string CategoryName, long AmountMinor, string AmountText, string PercentText);

    /// <summary>Everything the dashboard screen displays, already formatted — the controller
    /// only assigns these strings to VisualElements, no formatting logic in the controller
    /// itself. See docs/07-Interface.md §5.</summary>
    public sealed record DashboardViewModel(
        string AccountName,
        string AvailableBalanceText,
        string EndOfMonthBalanceText,
        string LowestBalanceText,
        string LowestBalanceDateText,
        string RemainingToLiveText,
        IReadOnlyList<ChartPointViewModel> ChartSeries,
        IReadOnlyList<ExpenseCategorySliceViewModel> ExpenseBreakdown,
        IReadOnlyList<DashboardVerificationItem> VerificationQueue,
        IReadOnlyList<DashboardUpcomingItem> UpcomingOperations,
        IReadOnlyList<DashboardBudgetRowViewModel> BudgetSummary);
}
