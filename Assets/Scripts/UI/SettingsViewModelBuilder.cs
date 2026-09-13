using System.Linq;
using FinanceOS.App;

namespace FinanceOS.UI
{
    /// <summary>Builds the Paramètres screen's data from AppSettingsService/AccountService alone.
    /// See docs/07-Interface.md §5.</summary>
    public static class SettingsViewModelBuilder
    {
        public static SettingsViewModel Build(AppSettingsService settings, AccountService accounts, string databasePath)
        {
            var current = settings.Get();
            var accountOptions = accounts.ListActive().Select(a => new DropdownOption(a.Id, a.Name)).ToList();

            return new SettingsViewModel(
                current.DefaultCurrency,
                current.ForecastHorizonDays,
                current.LowBalanceThresholdMinor,
                current.MissedThresholdDays,
                accountOptions,
                current.DefaultCurrentAccountId,
                databasePath);
        }
    }
}
