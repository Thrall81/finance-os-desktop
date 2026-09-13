using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the "file de vérification" — see docs/07-Interface.md §6.</summary>
    public sealed record DashboardVerificationItem(int OccurrenceId, string Label, string DateText, string AmountText);

    /// <summary>Everything the dashboard screen displays, already formatted — the controller
    /// only assigns these strings to VisualElements, no formatting logic in the controller
    /// itself. See docs/07-Interface.md §5.</summary>
    public sealed record DashboardViewModel(
        string AccountName,
        string AvailableBalanceText,
        string EndOfMonthBalanceText,
        string LowestBalanceText,
        string LowestBalanceDateText,
        IReadOnlyList<ChartPointViewModel> ChartSeries,
        IReadOnlyList<DashboardVerificationItem> VerificationQueue);
}
