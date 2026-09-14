using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the accounts list — already formatted, see docs/07-Interface.md §5.</summary>
    public sealed record AccountRowViewModel(
        int Id,
        string Name,
        string TypeText,
        string BalanceText,
        string LiquidityPolicyText,
        bool IsArchived);

    public sealed record AccountsViewModel(IReadOnlyList<AccountRowViewModel> Accounts);

    /// <summary>One row in an account's balance history — see docs/01-Perimetre.md §2.2.</summary>
    public sealed record BalanceHistoryRowViewModel(string DateText, string AmountText);
}
