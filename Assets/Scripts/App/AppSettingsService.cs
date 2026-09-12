using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>Thin pass-through to <see cref="AppSettingsRepository"/> — exists so UI never
    /// calls a repository directly, per docs/02-Architecture.md §3.4.</summary>
    public sealed class AppSettingsService
    {
        private readonly AppSettingsRepository _settings;

        public AppSettingsService(AppSettingsRepository settings) => _settings = settings;

        public AppSettings Get() => _settings.Load();

        public void UpdateForecastHorizon(int days)
        {
            var settings = _settings.Load();
            settings.UpdateForecastHorizon(days);
            _settings.Save(settings);
        }

        public void UpdateLowBalanceThreshold(long thresholdMinor)
        {
            var settings = _settings.Load();
            settings.UpdateLowBalanceThreshold(thresholdMinor);
            _settings.Save(settings);
        }

        public void SetDefaultCurrentAccount(int? accountId)
        {
            var settings = _settings.Load();
            settings.SetDefaultCurrentAccount(accountId);
            _settings.Save(settings);
        }
    }
}
