using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the onboarding flow's display-ready data from AccountService/RecurringOperationService
    /// alone — narrower dependency choice, same reasoning as AccountsViewModelBuilder.
    /// See docs/07-Interface.md §4.
    /// </summary>
    public static class OnboardingViewModelBuilder
    {
        public static OnboardingViewModel Build(AccountService accounts, RecurringOperationService operations)
        {
            var accountRows = accounts.ListAll()
                .Select(a => new OnboardingAccountRowViewModel(
                    a.Name,
                    AccountsViewModelBuilder.TypeText(a.Type),
                    MoneyFormat.Format(a.OfficialBalanceMinor, a.Currency)))
                .ToList();

            var operationRows = operations.ListAll()
                .Select(o => new OnboardingOperationRowViewModel(
                    o.Name,
                    o.Type == RecurringOperationType.Income ? "Revenu" : "Dépense",
                    MoneyFormat.Format(o.ExpectedAmountMinor, forceSign: true)))
                .ToList();

            var accountOptions = accounts.ListAll()
                .Select(a => new DropdownOption(a.Id, a.Name))
                .ToList();

            return new OnboardingViewModel(accountRows, operationRows, accountOptions);
        }
    }
}
