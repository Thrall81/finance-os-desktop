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
}
