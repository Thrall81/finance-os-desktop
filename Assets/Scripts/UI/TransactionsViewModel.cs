using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>An entry in an account/category dropdown — id kept alongside the display name so
    /// selection is resolved by index, never by matching the name text back (names are not
    /// guaranteed unique). See docs/07-Interface.md §9.</summary>
    public sealed record TransactionDropdownOption(int Id, string Name);

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
        IReadOnlyList<TransactionDropdownOption> AccountFilterOptions,
        IReadOnlyList<TransactionDropdownOption> CreatableAccounts,
        IReadOnlyList<TransactionDropdownOption> Categories,
        IReadOnlyList<TransactionRowViewModel> Transactions);
}
