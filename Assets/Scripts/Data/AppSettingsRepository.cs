using System.Collections.Generic;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class AppSettingRow
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Loads and saves the single typed <see cref="AppSettings"/> object as key/value rows in
    /// `app_setting`. Unlike the other repositories there is no id and no list — one settings
    /// object for the whole application. See docs/03-Modele_de_donnees.md §10.
    /// </summary>
    public sealed class AppSettingsRepository
    {
        private const string DefaultCurrencyKey = "default_currency";
        private const string ForecastHorizonDaysKey = "forecast_horizon_days";
        private const string LowBalanceThresholdMinorKey = "low_balance_threshold_minor";
        private const string DefaultCurrentAccountIdKey = "default_current_account_id";
        private const string MissedThresholdDaysKey = "missed_threshold_days";
        private const string ThemeKey = "theme";

        private readonly SQLiteConnection _connection;

        public AppSettingsRepository(SQLiteConnection connection) => _connection = connection;

        public AppSettings Load()
        {
            var rows = _connection.Query<AppSettingRow>("SELECT key AS Key, value AS Value FROM app_setting");
            var values = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                values[row.Key] = row.Value;
            }

            var settings = new AppSettings(
                defaultCurrency: values.TryGetValue(DefaultCurrencyKey, out var currency) ? currency : AppSettings.DefaultCurrencyCode,
                forecastHorizonDays: values.TryGetValue(ForecastHorizonDaysKey, out var horizon) && int.TryParse(horizon, out var horizonDays)
                    ? horizonDays
                    : AppSettings.DefaultForecastHorizonDays,
                lowBalanceThresholdMinor: values.TryGetValue(LowBalanceThresholdMinorKey, out var threshold) && long.TryParse(threshold, out var thresholdMinor)
                    ? thresholdMinor
                    : AppSettings.DefaultLowBalanceThresholdMinor,
                defaultCurrentAccountId: values.TryGetValue(DefaultCurrentAccountIdKey, out var accountId) && int.TryParse(accountId, out var parsedAccountId)
                    ? parsedAccountId
                    : null,
                missedThresholdDays: values.TryGetValue(MissedThresholdDaysKey, out var missedThreshold) && int.TryParse(missedThreshold, out var missedThresholdDays)
                    ? missedThresholdDays
                    : AppSettings.DefaultMissedThresholdDays,
                theme: values.TryGetValue(ThemeKey, out var theme) ? StorageFormat.ParseAppTheme(theme) : AppTheme.Light);

            return settings;
        }

        public void Save(AppSettings settings)
        {
            _connection.RunInTransaction(() =>
            {
                Upsert(DefaultCurrencyKey, settings.DefaultCurrency);
                Upsert(ForecastHorizonDaysKey, settings.ForecastHorizonDays.ToString());
                Upsert(LowBalanceThresholdMinorKey, settings.LowBalanceThresholdMinor.ToString());
                Upsert(MissedThresholdDaysKey, settings.MissedThresholdDays.ToString());
                Upsert(ThemeKey, settings.Theme.ToStorageString());

                if (settings.DefaultCurrentAccountId is { } accountId)
                {
                    Upsert(DefaultCurrentAccountIdKey, accountId.ToString());
                }
                else
                {
                    _connection.Execute("DELETE FROM app_setting WHERE key = ?", DefaultCurrentAccountIdKey);
                }
            });
        }

        private void Upsert(string key, string value) => _connection.Execute(
            "INSERT INTO app_setting (key, value) VALUES (?, ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value",
            key, value);
    }
}
