using System;

namespace FinanceOS.Forecast
{
    /// <summary>How sure the engine is that an event will actually happen as described.
    /// Simplified from the old project's four-tier scale — see docs/06-Moteur_de_prevision.md §4.</summary>
    public enum ForecastEventCertainty
    {
        /// <summary>A real, already-booked transaction.</summary>
        Confirmed,

        /// <summary>An occurrence generated from an active recurring operation.</summary>
        Expected,

        /// <summary>A standalone, one-off planned occurrence.</summary>
        Estimated,
    }

    /// <summary>
    /// A single dated, signed movement on one account — the calculator's unit of work, built
    /// from either a real <see cref="FinanceOS.Domain.Transaction"/> or an unresolved
    /// <see cref="FinanceOS.Domain.ForecastOccurrence"/>. See docs/06-Moteur_de_prevision.md §4.
    /// </summary>
    public sealed record ForecastEvent(
        int AccountId,
        int? SourceTransactionId,
        int? SourceOccurrenceId,
        DateTime Date,
        long AmountMinor,
        string Label,
        ForecastEventCertainty Certainty,
        bool IsTransfer);
}
