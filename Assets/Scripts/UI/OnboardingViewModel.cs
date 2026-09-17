using System.Collections.Generic;

namespace FinanceOS.UI
{
    /// <summary>One account already added during this onboarding session — see
    /// docs/07-Interface.md §4 (étape 2).</summary>
    public sealed record OnboardingAccountRowViewModel(int Id, string Name, string TypeText, string BalanceText);

    /// <summary>One recurring operation already added during this onboarding session — see
    /// docs/07-Interface.md §4 (étape 3).</summary>
    public sealed record OnboardingOperationRowViewModel(int Id, string Name, string TypeText, string AmountText);

    /// <summary>Everything the onboarding flow displays, already formatted. Rebuilt after every
    /// account/operation added, same "self-contained, re-renders itself" pattern as every other
    /// screen. See docs/07-Interface.md §4.</summary>
    public sealed record OnboardingViewModel(
        IReadOnlyList<OnboardingAccountRowViewModel> Accounts,
        IReadOnlyList<OnboardingOperationRowViewModel> Operations,
        IReadOnlyList<DropdownOption> AccountOptions,
        IReadOnlyList<DropdownOption> CategoryOptions);
}
