using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// Typed application preferences, persisted by FinanceOS.Data as key/value rows
    /// (app_setting table). See docs/01-Perimetre.md §2.11 and docs/03-Modele_de_donnees.md §10.
    /// </summary>
    public sealed class AppSettings
    {
        public const string DefaultCurrencyCode = "EUR";
        public const int DefaultForecastHorizonDays = 90;
        public const long DefaultLowBalanceThresholdMinor = 20000; // 200,00 EUR

        public string DefaultCurrency { get; private set; }
        public int ForecastHorizonDays { get; private set; }
        public long LowBalanceThresholdMinor { get; private set; }
        public int? DefaultCurrentAccountId { get; private set; }

        public AppSettings(
            string defaultCurrency = DefaultCurrencyCode,
            int forecastHorizonDays = DefaultForecastHorizonDays,
            long lowBalanceThresholdMinor = DefaultLowBalanceThresholdMinor,
            int? defaultCurrentAccountId = null)
        {
            if (string.IsNullOrWhiteSpace(defaultCurrency))
            {
                throw new ArgumentException("Default currency is required.", nameof(defaultCurrency));
            }

            if (forecastHorizonDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(forecastHorizonDays), forecastHorizonDays, "Forecast horizon must be positive.");
            }

            DefaultCurrency = defaultCurrency;
            ForecastHorizonDays = forecastHorizonDays;
            LowBalanceThresholdMinor = lowBalanceThresholdMinor;
            DefaultCurrentAccountId = defaultCurrentAccountId;
        }

        public void UpdateForecastHorizon(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days), days, "Forecast horizon must be positive.");
            }

            ForecastHorizonDays = days;
        }

        public void UpdateLowBalanceThreshold(long thresholdMinor) => LowBalanceThresholdMinor = thresholdMinor;

        public void SetDefaultCurrentAccount(int? accountId) => DefaultCurrentAccountId = accountId;
    }
}
