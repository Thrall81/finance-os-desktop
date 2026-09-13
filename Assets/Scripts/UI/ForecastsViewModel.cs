using System;
using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>The "Synthèse" card's values, already formatted. See docs/07-Interface.md §3.</summary>
    public sealed record ForecastSynthesisViewModel(
        string CurrentBalanceText,
        string ProjectedBalanceText,
        string LowestBalanceText,
        string LowestBalanceDateText,
        string ExpectedIncomeText,
        string ExpectedExpensesText,
        IReadOnlyList<string> Warnings);

    /// <summary>One day with at least one movement — the textual alternative next to the
    /// cash-flow chart, per docs/07-Interface.md §8.4's "toujours une alternative textuelle à
    /// côté du graphique". Deliberately only event days (unlike <see cref="ChartPointViewModel"/>,
    /// which needs every day to draw a continuous line).</summary>
    public sealed record ForecastTimelineRowViewModel(string DateText, string BalanceText, string EventsText);

    /// <summary>One day of the cash-flow chart's line — every day in the horizon, not just
    /// event days, and carries the raw balance/actual-vs-forecast flag `LineChartElement` needs
    /// to draw with, not display text. See docs/07-Interface.md §8.</summary>
    public sealed record ChartPointViewModel(DateTime Date, long ClosingBalanceMinor, bool IsActual);

    /// <summary>One row in "Occurrences prévues" — planned or missed, read-only on this screen.</summary>
    public sealed record ForecastOccurrenceRowViewModel(
        int Id, string DateText, string Label, string AmountText, string StatusText, bool IsMissed);

    /// <summary>One row in "Opérations à vérifier" — carries the raw expected date/amount
    /// alongside the display text, since "C'est arrivé" needs them to prefill the confirm form
    /// and to preserve the original sign when the user edits the magnitude.
    /// See docs/07-Interface.md §6.</summary>
    public sealed record VerificationRowViewModel(
        int OccurrenceId,
        string Label,
        string DateText,
        string AmountText,
        DateTime ExpectedDate,
        long ExpectedAmountMinor,
        bool IsMissed);

    /// <summary>Everything the Prévisions screen displays for the selected account. Null
    /// <see cref="Synthesis"/> means no account exists yet — same convention as
    /// DashboardViewModel. See docs/07-Interface.md §5.</summary>
    public sealed record ForecastsViewModel(
        IReadOnlyList<DropdownOption> Accounts,
        int? SelectedAccountId,
        ForecastSynthesisViewModel? Synthesis,
        IReadOnlyList<ChartPointViewModel> ChartSeries,
        IReadOnlyList<ForecastTimelineRowViewModel> Timeline,
        IReadOnlyList<ForecastOccurrenceRowViewModel> Occurrences,
        IReadOnlyList<VerificationRowViewModel> VerificationQueue,
        IReadOnlyList<DropdownOption> Categories);
}
