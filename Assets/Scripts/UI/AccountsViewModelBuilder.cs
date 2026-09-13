using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the accounts screen's display-ready data from AccountService alone — narrower than
    /// DashboardViewModelBuilder's full AppContainer dependency since this screen only ever reads
    /// accounts. Testable without instantiating a VisualElement. See docs/07-Interface.md §5.
    /// </summary>
    public static class AccountsViewModelBuilder
    {
        public static AccountsViewModel Build(AccountService accounts)
        {
            var rows = accounts.ListAll()
                .OrderBy(a => a.IsArchived)
                .ThenBy(a => a.Name)
                .Select(a => new AccountRowViewModel(
                    a.Id,
                    a.Name,
                    TypeText(a.Type),
                    MoneyFormat.Format(a.OfficialBalanceMinor, a.Currency),
                    LiquidityPolicyText(a.LiquidityPolicy),
                    a.IsArchived))
                .ToList();

            return new AccountsViewModel(rows);
        }

        public static string TypeText(AccountType type) => type switch
        {
            AccountType.Current => "Courant",
            AccountType.Savings => "Épargne",
            AccountType.Cash => "Espèces",
            AccountType.Investment => "Investissement",
            AccountType.Debt => "Dette",
            _ => type.ToString(),
        };

        public static string LiquidityPolicyText(LiquidityPolicy policy) => policy switch
        {
            LiquidityPolicy.Immediate => "Immédiat",
            LiquidityPolicy.Reserve => "Réservé",
            LiquidityPolicy.Excluded => "Exclu",
            _ => policy.ToString(),
        };
    }
}
