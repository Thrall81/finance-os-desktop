using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One row in the recurring operations table, already formatted.
    /// See docs/07-Interface.md §5/§10.</summary>
    public sealed record RecurringOperationRowViewModel(
        int Id,
        string Name,
        string TypeText,
        string AccountText,
        int? SourceAccountId,
        int? DestinationAccountId,
        string FrequencyText,
        string AmountText,
        string CategoryText,
        string CounterpartyText,
        string StartDateText,
        bool IsActive);

    public sealed record RecurringOperationsViewModel(
        IReadOnlyList<DropdownOption> Accounts,
        IReadOnlyList<DropdownOption> Categories,
        IReadOnlyList<RecurringOperationRowViewModel> Operations);
}
