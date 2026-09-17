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
        public static OnboardingViewModel Build(AccountService accounts, RecurringOperationService operations, CategoryService categories)
        {
            // ListActive, not ListAll: an account removed mid-onboarding (ADR-142) is archived,
            // not hard-deleted (no delete path exists for accounts anywhere in this app — a
            // recurring operation added after going back a step could already reference it, so a
            // real delete would risk a dangling reference). Filtering to active accounts here is
            // what makes "removed" actually look removed throughout the whole onboarding flow.
            var activeAccounts = accounts.ListActive();

            var accountRows = activeAccounts
                .Select(a => new OnboardingAccountRowViewModel(
                    a.Id,
                    a.Name,
                    AccountsViewModelBuilder.TypeText(a.Type),
                    MoneyFormat.Format(a.OfficialBalanceMinor, a.Currency)))
                .ToList();

            var operationRows = operations.ListAll()
                .Select(o => new OnboardingOperationRowViewModel(
                    o.Id,
                    o.Name,
                    o.Type == RecurringOperationType.Income ? "Revenu" : "Dépense",
                    MoneyFormat.Format(o.ExpectedAmountMinor, forceSign: true)))
                .ToList();

            var accountOptions = activeAccounts
                .Select(a => new DropdownOption(a.Id, a.Name))
                .ToList();

            var categoryOptions = categories.ListActive()
                .OrderBy(c => c.Name)
                .Select(c => new DropdownOption(c.Id, c.Name))
                .ToList();

            return new OnboardingViewModel(accountRows, operationRows, accountOptions, categoryOptions);
        }
    }
}
