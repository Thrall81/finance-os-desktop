using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the "file de vérification" — see docs/07-Interface.md §6.</summary>
    public sealed record DashboardVerificationItem(int OccurrenceId, string Label, string DateText, string AmountText);

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
        IReadOnlyList<DashboardVerificationItem> VerificationQueue);
}
