using System;
using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>Every filter dimension from docs/01-Perimetre.md §2.6 ("recherche, filtres
    /// (compte, période, catégorie, montant, texte)"), all optional — a field left null applies
    /// no constraint. Amount bounds compare against the transaction's magnitude
    /// (<c>Math.Abs(AmountMinor)</c>), not its signed value: "between 50 and 100 €" should match
    /// both an expense and an income of that size, not require knowing the sign up front. Text
    /// matches <c>OriginalLabel</c> case-insensitively (ordinal — safe and culture-independent,
    /// unlike the money/date formatting this project deliberately keeps away from CultureInfo).
    /// See docs/07-Interface.md §5/§10.</summary>
    public sealed record TransactionFilter(
        int? AccountId = null,
        int? CategoryId = null,
        DateTime? DateFrom = null,
        DateTime? DateTo = null,
        long? AmountMinMinor = null,
        long? AmountMaxMinor = null,
        string? Text = null);

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
