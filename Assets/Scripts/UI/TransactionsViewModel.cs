using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the transactions table, already formatted. See docs/07-Interface.md §5/§10.</summary>
    public sealed record TransactionRowViewModel(
        int Id,
        int AccountId,
        int? CategoryId,
        int? CounterpartyId,
        string DateText,
        string AccountName,
        string Label,
        string CategoryText,
        string CounterpartyText,
        string AmountText,
        string Notes,
        bool IsInternalTransfer,
        bool IsExcludedFromBudget);

    public sealed record TransactionsViewModel(
        IReadOnlyList<DropdownOption> AccountFilterOptions,
        IReadOnlyList<DropdownOption> CreatableAccounts,
        IReadOnlyList<DropdownOption> Categories,
        IReadOnlyList<TransactionRowViewModel> Transactions);
}
