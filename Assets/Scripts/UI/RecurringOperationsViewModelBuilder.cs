using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the recurring operations screen's display-ready data from exactly the services it
    /// needs, same narrower-dependency choice as AccountsViewModelBuilder/TransactionsViewModelBuilder.
    /// See docs/07-Interface.md §5.
    /// </summary>
    public static class RecurringOperationsViewModelBuilder
    {
        public static RecurringOperationsViewModel Build(
            AccountService accounts,
            CategoryService categories,
            CounterpartyService counterparties,
            RecurringOperationService operations)
        {
            var accountList = accounts.ListAll();
            var accountNames = accountList.ToDictionary(a => a.Id, a => a.Name);

            var categoryList = categories.ListActive().OrderBy(c => c.Name).ToList();
            var categoryNames = categoryList.ToDictionary(c => c.Id, c => c.Name);

            var counterpartyNames = counterparties.ListAll().ToDictionary(c => c.Id, c => c.Name);

            var rows = operations.ListAll()
                .OrderBy(o => !o.IsActive)
                .ThenBy(o => o.Name)
                .Select(o => new RecurringOperationRowViewModel(
                    o.Id,
                    o.Name,
                    TypeText(o.Type),
                    AccountText(o, accountNames),
                    o.SourceAccountId,
                    o.DestinationAccountId,
                    FrequencyText(o.Frequency),
                    o.ExpectedDayOfMonth,
                    MoneyFormat.Format(o.ExpectedAmountMinor, "EUR", forceSign: true),
                    o.CategoryId,
                    o.CategoryId is int categoryId && categoryNames.TryGetValue(categoryId, out var categoryName) ? categoryName : "—",
                    o.CounterpartyId is int counterpartyId && counterpartyNames.TryGetValue(counterpartyId, out var counterpartyName) ? counterpartyName : "—",
                    o.StartDate,
                    o.IsActive))
                .ToList();

            return new RecurringOperationsViewModel(
                accountList.Where(a => !a.IsArchived).Select(a => new DropdownOption(a.Id, a.Name)).ToList(),
                categoryList.Select(c => new DropdownOption(c.Id, c.Name)).ToList(),
                rows);
        }

        private static string AccountText(RecurringOperation operation, Dictionary<int, string> accountNames)
        {
            string NameOf(int? accountId) =>
                accountId is int id && accountNames.TryGetValue(id, out var name) ? name : "—";

            return operation.Type switch
            {
                RecurringOperationType.Income => NameOf(operation.DestinationAccountId),
                RecurringOperationType.Expense => NameOf(operation.SourceAccountId),
                _ => $"{NameOf(operation.SourceAccountId)} → {NameOf(operation.DestinationAccountId)}",
            };
        }

        public static string TypeText(RecurringOperationType type) => type switch
        {
            RecurringOperationType.Income => "Revenu",
            RecurringOperationType.Expense => "Dépense",
            RecurringOperationType.SavingsTransfer => "Virement épargne",
            RecurringOperationType.InternalTransfer => "Virement interne",
            _ => type.ToString(),
        };

        public static string FrequencyText(RecurringFrequency frequency) => frequency switch
        {
            RecurringFrequency.Weekly => "Hebdomadaire",
            RecurringFrequency.Monthly => "Mensuelle",
            RecurringFrequency.Bimonthly => "Bimestrielle",
            RecurringFrequency.Quarterly => "Trimestrielle",
            RecurringFrequency.Semiannual => "Semestrielle",
            RecurringFrequency.Yearly => "Annuelle",
            _ => frequency.ToString(),
        };
    }
}
