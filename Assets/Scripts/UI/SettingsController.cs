using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FinanceOS.App;
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
        private readonly AppSettingsService _settings;
        private readonly AccountService _accounts;
        private readonly BackupService _backup;
        private readonly string _databasePath;

        private readonly Label _currencyValueLabel;
        private readonly TextField _horizonField;
        private readonly TextField _lowBalanceField;
        private readonly TextField _missedThresholdField;
        private readonly DropdownField _defaultAccountField;
        private readonly Label _errorLabel;
        private readonly Label _savedLabel;
        private readonly Button _saveButton;

        private readonly Label _databasePathLabel;
        private readonly Button _copyPathButton;
        private readonly Button _backupButton;
        private readonly Label _backupResultLabel;

        private IReadOnlyList<DropdownOption> _accountOptions = Array.Empty<DropdownOption>();

        public SettingsController(
            VisualElement root, AppSettingsService settings, AccountService accounts, BackupService backup, string databasePath)
        {
            _settings = settings;
            _accounts = accounts;
            _backup = backup;
            _databasePath = databasePath;

            _currencyValueLabel = root.Q<Label>("currency-value");
            _horizonField = root.Q<TextField>("horizon-field");
            _lowBalanceField = root.Q<TextField>("low-balance-field");
            _missedThresholdField = root.Q<TextField>("missed-threshold-field");
            _defaultAccountField = root.Q<DropdownField>("default-account-field");
            _errorLabel = root.Q<Label>("settings-error");
            _savedLabel = root.Q<Label>("settings-saved-label");
            _saveButton = root.Q<Button>("save-button");

            _databasePathLabel = root.Q<Label>("database-path-value");
            _copyPathButton = root.Q<Button>("copy-path-button");
            _backupButton = root.Q<Button>("backup-button");
            _backupResultLabel = root.Q<Label>("backup-result-label");

            _saveButton.clicked += Save;
            _copyPathButton.clicked += CopyPathToClipboard;
            _backupButton.clicked += CreateBackup;

            _horizonField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _lowBalanceField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _missedThresholdField.RegisterValueChangedCallback(_ => HideSavedMessage());
            _defaultAccountField.RegisterValueChangedCallback(_ => HideSavedMessage());

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

            HideError();
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
