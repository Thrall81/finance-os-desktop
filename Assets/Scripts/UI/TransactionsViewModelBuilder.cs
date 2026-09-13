using System.Linq;
using FinanceOS.App;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the transactions screen's display-ready data from exactly the services it needs
    /// (not the full AppContainer), same narrower-dependency choice as AccountsViewModelBuilder.
    /// See docs/07-Interface.md §5.
    /// </summary>
    public static class TransactionsViewModelBuilder
    {
        public static TransactionsViewModel Build(
            AccountService accounts,
            CategoryService categories,
            CounterpartyService counterparties,
            TransactionService transactions,
            int? accountFilter)
        {
            var accountList = accounts.ListAll();
            var accountNames = accountList.ToDictionary(a => a.Id, a => a.Name);

            var categoryList = categories.ListActive().OrderBy(c => c.Name).ToList();
            var categoryNames = categoryList.ToDictionary(c => c.Id, c => c.Name);

            var counterpartyNames = counterparties.ListAll().ToDictionary(c => c.Id, c => c.Name);

            var source = accountFilter is int accountId
                ? transactions.ListForAccount(accountId)
                : transactions.ListAll();

            var rows = source
                .Select(t => new TransactionRowViewModel(
                    t.Id,
                    t.AccountId,
                    t.CategoryId,
                    t.CounterpartyId,
                    DateFormat.Short(t.OperationDate),
                    accountNames.TryGetValue(t.AccountId, out var accountName) ? accountName : "—",
                    t.OriginalLabel,
                    t.IsInternalTransfer
                        ? "Virement interne"
                        : t.CategoryId is int categoryId && categoryNames.TryGetValue(categoryId, out var categoryName)
                            ? categoryName
                            : "—",
                    t.CounterpartyId is int counterpartyId && counterpartyNames.TryGetValue(counterpartyId, out var counterpartyName)
                        ? counterpartyName
                        : "—",
                    MoneyFormat.Format(t.AmountMinor, t.Currency, forceSign: true),
                    t.Notes ?? string.Empty,
                    t.IsInternalTransfer,
                    t.IsExcludedFromBudget))
                .ToList();

            return new TransactionsViewModel(
                accountList.Select(a => new TransactionDropdownOption(a.Id, a.Name)).ToList(),
                accountList.Where(a => !a.IsArchived).Select(a => new TransactionDropdownOption(a.Id, a.Name)).ToList(),
                categoryList.Select(c => new TransactionDropdownOption(c.Id, c.Name)).ToList(),
                rows);
        }
    }
}
