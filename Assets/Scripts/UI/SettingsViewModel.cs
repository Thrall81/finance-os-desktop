using System.Collections.Generic;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>Everything the Paramètres screen displays — mostly raw values rather than
    /// pre-formatted display text, since this screen is a form (fields the user edits directly),
    /// not a report. See docs/01-Perimetre.md §2.11 and docs/07-Interface.md §3.</summary>
    public sealed record SettingsViewModel(
        string Currency,
        int ForecastHorizonDays,
        long LowBalanceThresholdMinor,
        int MissedThresholdDays,
        IReadOnlyList<DropdownOption> Accounts,
        int? DefaultCurrentAccountId,
        string DatabasePath,
        AppTheme Theme);
}
