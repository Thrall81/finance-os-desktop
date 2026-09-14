using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Settings.uxml: forecast horizon, low-balance threshold, missed-occurrence threshold
    /// and the dashboard's default account (one form, one save) plus the data file's location
    /// (read-only, copy-to-clipboard) and a manual backup action. See docs/07-Interface.md §3.
    /// </summary>
    public sealed class SettingsController
    {
        private static readonly (AppTheme Theme, string Text)[] ThemeOptions =
        {
            (AppTheme.Light, "Clair"),
            (AppTheme.Dark, "Sombre"),
        };

        private readonly AppSettingsService _settings;
        private readonly AccountService _accounts;
        private readonly BackupService _backup;
        private readonly string _databasePath;
        private readonly Action<AppTheme>? _onThemeChanged;

        private readonly Label _currencyValueLabel;
        private readonly TextField _horizonField;
        private readonly TextField _lowBalanceField;
        private readonly TextField _missedThresholdField;
        private readonly DropdownField _defaultAccountField;
        private readonly DropdownField _themeField;
        private readonly Label _errorLabel;
        private readonly Label _savedLabel;
        private readonly Button _saveButton;

        private readonly Label _databasePathLabel;
        private readonly Button _copyPathButton;
        private readonly Button _backupButton;
        private readonly Label _backupResultLabel;
        private readonly Label _versionLabel;

        private IReadOnlyList<DropdownOption> _accountOptions = Array.Empty<DropdownOption>();

        public SettingsController(
            VisualElement root, AppSettingsService settings, AccountService accounts, BackupService backup, string databasePath,
            Action<AppTheme>? onThemeChanged = null)
        {
            _settings = settings;
            _accounts = accounts;
            _backup = backup;
            _databasePath = databasePath;
            _onThemeChanged = onThemeChanged;

            _currencyValueLabel = root.Q<Label>("currency-value");
            _horizonField = root.Q<TextField>("horizon-field");
            _lowBalanceField = root.Q<TextField>("low-balance-field");
            _missedThresholdField = root.Q<TextField>("missed-threshold-field");
            _defaultAccountField = root.Q<DropdownField>("default-account-field");
            _themeField = root.Q<DropdownField>("theme-field");
            _errorLabel = root.Q<Label>("settings-error");
            _savedLabel = root.Q<Label>("settings-saved-label");
            _saveButton = root.Q<Button>("save-button");

            _databasePathLabel = root.Q<Label>("database-path-value");
            _copyPathButton = root.Q<Button>("copy-path-button");
            _backupButton = root.Q<Button>("backup-button");
            _backupResultLabel = root.Q<Label>("backup-result-label");
            _versionLabel = root.Q<Label>("version-value");
            // Read once — Application.version (PlayerSettings.bundleVersion) never changes
            // during a session, unlike everything else this screen shows via Refresh().
            _versionLabel.text = Application.version;

            _themeField.choices = ThemeOptions.Select(o => o.Text).ToList();

            _saveButton.clicked += Save;
            _copyPathButton.clicked += CopyPathToClipboard;
            _backupButton.clicked += CreateBackup;

            _horizonField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _lowBalanceField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _missedThresholdField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _defaultAccountField.RegisterValueChangedCallback(_ => HideSavedMessage());
            // Applied and persisted immediately on selection, unlike the rest of this form's
            // fields — unlike a horizon/threshold number, a theme choice has instant visual
            // feedback, so batching it behind "Enregistrer" would let a user pick dark, close the
            // app without saving, and have it silently revert to light on the next launch.
            _themeField.RegisterValueChangedCallback(_ => OnThemeFieldChanged());

            Refresh();
        }

        public void Refresh()
        {
            var viewModel = SettingsViewModelBuilder.Build(_settings, _accounts, _databasePath);

            _currencyValueLabel.text = viewModel.Currency;
            _horizonField.SetValueWithoutNotify(viewModel.ForecastHorizonDays.ToString());
            _lowBalanceField.SetValueWithoutNotify(PlainAmountText(viewModel.LowBalanceThresholdMinor));
            _missedThresholdField.SetValueWithoutNotify(viewModel.MissedThresholdDays.ToString());

            _accountOptions = viewModel.Accounts;
            var choices = new List<string> { "Aucun (automatique)" };
            choices.AddRange(_accountOptions.Select(o => o.Name));
            _defaultAccountField.choices = choices;

            var selectedIndex = viewModel.DefaultCurrentAccountId is int accountId
                ? _accountOptions.ToList().FindIndex(o => o.Id == accountId) + 1
                : 0;
            _defaultAccountField.SetValueWithoutNotify(choices[Math.Max(selectedIndex, 0)]);

            _databasePathLabel.text = viewModel.DatabasePath;

            _themeField.SetValueWithoutNotify(ThemeOptions.First(o => o.Theme == viewModel.Theme).Text);

            HideError();
        }

        // Public only so UISmokeTest.cs can trigger it directly after a SetValueWithoutNotify,
        // same "public purely for testability" reasoning as TransactionsController.OnFilterChanged
        // — a VisualTreeAsset instantiated without a panel never dispatches the ChangeEvent a real
        // .value assignment would send, so RegisterValueChangedCallback never fires here either.
        public void OnThemeFieldChanged()
        {
            var index = ThemeOptions.ToList().FindIndex(o => o.Text == _themeField.value);
            var theme = ThemeOptions[index < 0 ? 0 : index].Theme;

            _settings.UpdateTheme(theme);
            _onThemeChanged?.Invoke(theme);
        }

        private void Save()
        {
            if (!int.TryParse(_horizonField.value, out var horizonDays) || horizonDays <= 0)
            {
                ShowError("L'horizon de prévision doit être un nombre de jours positif.");
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_lowBalanceField.value, out var lowBalanceMinor) || lowBalanceMinor < 0)
            {
                ShowError("Le seuil de solde faible doit être un montant positif ou nul, ex. 200,00.");
                return;
            }

            if (!int.TryParse(_missedThresholdField.value, out var missedThresholdDays) || missedThresholdDays <= 0)
            {
                ShowError("Le seuil avant « Manquée » doit être un nombre de jours positif.");
                return;
            }

            var accountIndex = _defaultAccountField.index;
            int? defaultAccountId = accountIndex <= 0 ? null : _accountOptions[accountIndex - 1].Id;

            try
            {
                _settings.UpdateForecastHorizon(horizonDays);
                _settings.UpdateLowBalanceThreshold(lowBalanceMinor);
                _settings.UpdateMissedThreshold(missedThresholdDays);
                _settings.SetDefaultCurrentAccount(defaultAccountId);
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
                return;
            }

            Refresh();
            ShowSavedMessage();
        }

        private void CopyPathToClipboard() => GUIUtility.systemCopyBuffer = _databasePath;

        private void CreateBackup()
        {
            try
            {
                var backupPath = _backup.CreateBackup();
                _backupResultLabel.text = $"Sauvegarde créée : {backupPath}";
            }
            catch (IOException)
            {
                _backupResultLabel.text = "Échec de la sauvegarde — une sauvegarde existe peut-être déjà pour cette même seconde, réessayez.";
            }

            _backupResultLabel.style.display = DisplayStyle.Flex;
        }

        private static string PlainAmountText(long amountMinor) => $"{amountMinor / 100},{amountMinor % 100:D2}";

        private void ShowError(string message)
        {
            _errorLabel.text = message;
            _errorLabel.style.display = DisplayStyle.Flex;
            HideSavedMessage();
        }

        private void HideError()
        {
            _errorLabel.text = string.Empty;
            _errorLabel.style.display = DisplayStyle.None;
        }

        private void ShowSavedMessage() => _savedLabel.style.display = DisplayStyle.Flex;

        private void HideSavedMessage() => _savedLabel.style.display = DisplayStyle.None;
    }
}
